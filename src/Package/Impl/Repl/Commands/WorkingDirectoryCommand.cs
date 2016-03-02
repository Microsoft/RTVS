﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Common.Core;
using Microsoft.Languages.Editor;
using Microsoft.Languages.Editor.Controller.Command;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Session;
using Microsoft.R.Support.Settings;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudioTools;

namespace Microsoft.VisualStudio.R.Package.Repl.Commands {
    public sealed class WorkingDirectoryCommand : Command, IDisposable {
        private IRSession _session;

        public Task InitializationTask { get; }

        public WorkingDirectoryCommand() :
            base(new[] {
                new CommandId(RGuidList.RCmdSetGuid, RPackageCommandId.icmdSelectWorkingDirectory),
                new CommandId(RGuidList.RCmdSetGuid, RPackageCommandId.icmdGetDirectoryList),
                new CommandId(RGuidList.RCmdSetGuid, RPackageCommandId.icmdSetWorkingDirectory)
            }, false) {

            _session = VsAppShell.Current.ExportProvider.GetExportedValue<IRSessionProvider>().GetInteractiveWindowRSession();
            _session.Connected += OnSessionConnected;
            _session.DirectoryChanged += OnCurrentDirectoryChanged;

            if (InitializationTask == null && _session.IsHostRunning) {
                InitializationTask = UpdateRUserDirectoryAsync();
            }
        }

        internal string UserDirectory { get; private set; }

        public void Dispose() {
            if (_session != null) {
                _session.Connected -= OnSessionConnected;
                _session.DirectoryChanged -= OnCurrentDirectoryChanged;
            }
            _session = null;
        }

        private void OnCurrentDirectoryChanged(object sender, EventArgs e) {
            FetchRWorkingDirectoryAsync().DoNotWait();
        }

        private void OnSessionConnected(object sender, EventArgs e) {
            FetchRWorkingDirectoryAsync().DoNotWait();
        }

        private async Task FetchRWorkingDirectoryAsync() {
            if (UserDirectory == null) {
                await UpdateRUserDirectoryAsync();
            }

            string directory = await GetRWorkingDirectoryAsync();
            if (!string.IsNullOrEmpty(directory)) {
                RToolsSettings.Current.WorkingDirectory = directory;
            }
        }

        public override CommandStatus Status(Guid group, int id) {
            if (ReplWindow.ReplWindowExists) {
                return CommandStatus.SupportedAndEnabled;
            }
            return CommandStatus.Invisible;
        }

        public override CommandResult Invoke(Guid group, int id, object inputArg, ref object outputArg) {
            switch (id) {
                case RPackageCommandId.icmdSelectWorkingDirectory:
                    SelectDirectory();
                    break;

                case RPackageCommandId.icmdGetDirectoryList:
                    // Return complete list
                    outputArg = GetFriendlyDirectoryNames();
                    break;

                case RPackageCommandId.icmdSetWorkingDirectory:
                    if (inputArg == null) {
                        // Return currently selected item
                        if (!string.IsNullOrEmpty(RToolsSettings.Current.WorkingDirectory)) {
                            outputArg = GetFriendlyDirectoryName(RToolsSettings.Current.WorkingDirectory);
                        }
                    } else {
                        SetDirectory(inputArg as string);
                    }
                    break;
            }

            return CommandResult.Executed;
        }

        internal Task SetDirectory(string friendlyName) {
            string currentDirectory = GetFullPathName(RToolsSettings.Current.WorkingDirectory);
            string newDirectory = GetFullPathName(friendlyName);

            if (newDirectory != null && currentDirectory != newDirectory) {
                RToolsSettings.Current.WorkingDirectory = GetFriendlyDirectoryName(newDirectory);
                _session.ScheduleEvaluation(async e => {
                    await e.SetWorkingDirectory(newDirectory);
                });
            }

            return Task.CompletedTask;
        }

        internal void SelectDirectory() {
            IVsUIShell uiShell = VsAppShell.Current.GetGlobalService<IVsUIShell>(typeof(SVsUIShell));
            IntPtr dialogOwner;
            uiShell.GetDialogOwnerHwnd(out dialogOwner);

            string currentDirectory = RToolsSettings.Current.WorkingDirectory;
            string newDirectory = Dialogs.BrowseForDirectory(dialogOwner, currentDirectory, Resources.ChooseDirectory);
            SetDirectory(newDirectory);
        }

        internal string[] GetFriendlyDirectoryNames() {
            return RToolsSettings.Current.WorkingDirectoryList
                .Select(GetFriendlyDirectoryName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        internal string GetFriendlyDirectoryName(string directory) {
            if (!string.IsNullOrEmpty(UserDirectory)) {
                if (directory.StartsWithIgnoreCase(UserDirectory)) {
                    var relativePath = PathHelper.MakeRelative(UserDirectory, directory);
                    if (relativePath.Length > 0) {
                        return "~/" + relativePath.Replace('\\', '/');
                    }
                    return "~";
                }
                return directory.Replace('\\', '/');
            }
            return directory;
        }

        internal string GetFullPathName(string friendlyName) {
            string folder = friendlyName;
            if (friendlyName == null) {
                return folder;
            }

            if (!friendlyName.StartsWithIgnoreCase("~")) {
                return folder;
            }

            if (friendlyName.EqualsIgnoreCase("~")) {
                return UserDirectory;
            }

            return PathHelper.MakeRooted(PathHelper.EnsureTrailingSlash(UserDirectory), friendlyName.Substring(2));
        }

        internal async Task<string> GetRWorkingDirectoryAsync() {
            await TaskUtilities.SwitchToBackgroundThread();
            try {
                using (var evaluation = await _session.BeginEvaluationAsync(false)) {
                    return await evaluation.GetWorkingDirectory();
                }
            } catch (TaskCanceledException) { }
            return null;
        }

        private async Task UpdateRUserDirectoryAsync() {
            await TaskUtilities.SwitchToBackgroundThread();
            try {
                using (var evaluation = await _session.BeginEvaluationAsync(false)) {
                    UserDirectory = await evaluation.GetRUserDirectory();
                }
            } catch (TaskCanceledException) {
            }
        }
    }
}
