﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Common.Core;
using Microsoft.Common.Core.Enums;
using Microsoft.Common.Core.IO;
using Microsoft.Common.Core.Shell;
using Microsoft.R.Actions.Logging;
using Microsoft.R.Components.History;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Session;
using Microsoft.R.Support.Settings.Definitions;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.FileSystemMirroring.IO;
using Microsoft.VisualStudio.ProjectSystem.FileSystemMirroring.Project;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.R.Package.Interop;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.R.Package.SurveyNews;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Shell.Interop;
#if VS14
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using IThreadHandling = Microsoft.VisualStudio.ProjectSystem.IThreadHandling;
#endif
#if VS15
using IThreadHandling = Microsoft.VisualStudio.ProjectSystem.IProjectThreadingService;
#endif


namespace Microsoft.VisualStudio.R.Package.ProjectSystem {
    internal sealed class RProjectLoadHooks {
        private const string DefaultRDataName = ".RData";
        private const string DefaultRHistoryName = ".RHistory";

        [Export(typeof(IFileSystemMirroringProjectTemporaryItems))]
        private FileSystemMirroringProject Project { get; }

        private readonly MsBuildFileSystemWatcher _fileWatcher;
        private readonly string _projectDirectory;
        private readonly IRToolsSettings _toolsSettings;
        private readonly IFileSystem _fileSystem;
        private readonly IThreadHandling _threadHandling;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IEnumerable<Lazy<IVsProject>> _cpsIVsProjects;
        private readonly IRInteractiveWorkflowProvider _workflowProvider;
        private readonly IInteractiveWindowComponentContainerFactory _componentContainerFactory;

        private IRInteractiveWorkflow _workflow;
        private IRSession _session;
        private IRHistory _history;
        private ISurveyNewsService _surveyNews;

        /// <summary>
        /// Backing field for the similarly named property.
        /// </summary>
        [ImportingConstructor]
        public RProjectLoadHooks(UnconfiguredProject unconfiguredProject
            , [ImportMany("Microsoft.VisualStudio.ProjectSystem.Microsoft.VisualStudio.Shell.Interop.IVsProject")] IEnumerable<Lazy<IVsProject>> cpsIVsProjects
            , IProjectLockService projectLockService
            , IRInteractiveWorkflowProvider workflowProvider
            , IInteractiveWindowComponentContainerFactory componentContainerFactory
            , IRToolsSettings toolsSettings
            , IFileSystem fileSystem
            , IThreadHandling threadHandling
            , ISurveyNewsService surveyNews) {

            _unconfiguredProject = unconfiguredProject;
            _cpsIVsProjects = cpsIVsProjects;
            _workflowProvider = workflowProvider;
            _componentContainerFactory = componentContainerFactory;

            _toolsSettings = toolsSettings;
            _fileSystem = fileSystem;
            _threadHandling = threadHandling;
            _surveyNews = surveyNews;
            _projectDirectory = unconfiguredProject.GetProjectDirectory();

            unconfiguredProject.ProjectUnloading += ProjectUnloading;
            _fileWatcher = new MsBuildFileSystemWatcher(_projectDirectory, "*", 25, 1000, fileSystem, new RMsBuildFileSystemFilter());
            _fileWatcher.Error += FileWatcherError;
            Project = new FileSystemMirroringProject(unconfiguredProject, projectLockService, _fileWatcher);
        }

        [AppliesTo("RTools")]
#if VS14
        [UnconfiguredProjectAutoLoad2(completeBy: UnconfiguredProjectLoadCheckpoint.CapabilitiesEstablished)]
#else
        [ProjectAutoLoad(
            startAfter: ProjectLoadCheckpoint.AfterLoadInitialConfiguration,
            completeBy: ProjectLoadCheckpoint.ProjectInitialCapabilitiesEstablished)]
#endif
        public async Task InitializeProjectFromDiskAsync() {
            await Project.CreateInMemoryImport();
            _fileWatcher.Start();

            await _threadHandling.SwitchToUIThread();
            // Make sure R package is loaded
            VsAppShell.EnsurePackageLoaded(RGuidList.RPackageGuid);

            // Verify project is not on a network share and give warning if it is
            CheckRemoteDrive(_projectDirectory);

            _workflow = _workflowProvider.GetOrCreate();
            _session = _workflow.RSession;
            _history = _workflow.History;

            if (_workflow.ActiveWindow == null) {
                var window = await _workflow.GetOrCreateVisualComponent(_componentContainerFactory);
                window.Container.Show(true);
            }

            try {
                await _session.HostStarted;
            } catch (Exception) {
                return;
            }

            var rdataPath = Path.Combine(_projectDirectory, DefaultRDataName);
            bool loadDefaultWorkspace = _fileSystem.FileExists(rdataPath) && await GetLoadDefaultWorkspace(rdataPath);
            using (var evaluation = await _session.BeginEvaluationAsync()) {
                if (loadDefaultWorkspace) {
                    await evaluation.LoadWorkspace(rdataPath);
                }

                await evaluation.SetWorkingDirectory(_projectDirectory);
            }

            _toolsSettings.WorkingDirectory = _projectDirectory;
            _history.TryLoadFromFile(Path.Combine(_projectDirectory, DefaultRHistoryName));

            CheckSurveyNews();
        }

        private async void CheckSurveyNews() {
            // Don't return a task, the caller doesn't want to await on this
            // and hold up loading of the project.
            // We do it this way instead of calling DoNotWait extension in order
            // to handle any non critical exceptions.
            try {
                await _surveyNews.CheckSurveyNewsAsync(false);
            } catch (Exception ex) when (!ex.IsCriticalException()) {
                GeneralLog.Write(ex);
            }
        }

        private void FileWatcherError(object sender, EventArgs args) {
            _fileWatcher.Error -= FileWatcherError;
            VsAppShell.Current.DispatchOnUIThread(() => {
                foreach (var iVsProjectLazy in _cpsIVsProjects) {
                    IVsProject iVsProject;
                    try {
                        iVsProject = iVsProjectLazy.Value;
                    } catch (Exception) {
                        continue;
                    }

                    if (iVsProject.AsUnconfiguredProject() != _unconfiguredProject) {
                        continue;
                    }

                    var solution = VsAppShell.Current.GetGlobalService<IVsSolution>(typeof(SVsSolution));
                    solution.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_UnloadProject, (IVsHierarchy)iVsProject, 0);
                    return;
                }
            });
        }

        private async Task ProjectUnloading(object sender, EventArgs args) {
            VsAppShell.Current.AssertIsOnMainThread();

            _unconfiguredProject.ProjectUnloading -= ProjectUnloading;
            _fileWatcher.Dispose();

            if (_fileSystem.DirectoryExists(_projectDirectory)) {
                if (_toolsSettings.AlwaysSaveHistory) {
                    _history.TrySaveToFile(Path.Combine(_projectDirectory, DefaultRHistoryName));
                }

                var rdataPath = Path.Combine(_projectDirectory, DefaultRDataName);
                var saveDefaultWorkspace = await GetSaveDefaultWorkspace(rdataPath);
                if (_session.IsHostRunning) {
                    Task.Run(async () => {
                        using (var evaluation = await _session.BeginEvaluationAsync()) {
                            if (saveDefaultWorkspace) {
                                await evaluation.SaveWorkspace(rdataPath);
                            }
                            await evaluation.SetDefaultWorkingDirectory();
                        }
                    }).DoNotWait();
                }
            }
        }

        private async Task<bool> GetLoadDefaultWorkspace(string rdataPath) {
            switch (_toolsSettings.LoadRDataOnProjectLoad) {
                case YesNoAsk.Yes:
                    return true;
                case YesNoAsk.Ask:
                    await _threadHandling.SwitchToUIThread();
                    return VsAppShell.Current.ShowMessage(
                        string.Format(CultureInfo.CurrentCulture, Resources.LoadWorkspaceIntoGlobalEnvironment, rdataPath),
                        MessageButtons.YesNo) == MessageButtons.Yes;
                default:
                    return false;
            }
        }

        private async Task<bool> GetSaveDefaultWorkspace(string rdataPath) {
            switch (_toolsSettings.SaveRDataOnProjectUnload) {
                case YesNoAsk.Yes:
                    return true;
                case YesNoAsk.Ask:
                    await _threadHandling.SwitchToUIThread();
                    return VsAppShell.Current.ShowMessage(
                        string.Format(CultureInfo.CurrentCulture, Resources.SaveWorkspaceOnProjectUnload, rdataPath),
                        MessageButtons.YesNo) == MessageButtons.Yes;
                default:
                    return false;
            }
        }

        private void CheckRemoteDrive(string path) {
            bool remoteDrive = NativeMethods.PathIsUNC(path);
            if (!remoteDrive) {
                var pathRoot = Path.GetPathRoot(path);
                var driveType = (NativeMethods.DriveType)NativeMethods.GetDriveType(pathRoot);
                remoteDrive = driveType == NativeMethods.DriveType.Remote;
            }
            if (remoteDrive) {
                VsAppShell.Current.ShowMessage(Resources.Warning_UncPath, MessageButtons.OK);
            }
        }
    }
}