﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Common.Core;
using Microsoft.Common.Core.Diagnostics;
using Microsoft.Common.Core.Imaging;
using Microsoft.Common.Core.Shell;
using Microsoft.Languages.Editor.Completions;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Components.Settings;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Session;
using Newtonsoft.Json.Linq;
using static System.FormattableString;

namespace Microsoft.R.Editor.Completions.Providers {
    /// <summary>
    /// Provides list of files and folder in the current directory
    /// </summary>
    internal sealed class FilesCompletionProvider : IRCompletionListProvider {
        private readonly IImageService _imageService;
        private readonly IRInteractiveWorkflow _workflow;
        private readonly IRSettings _settings;

        private Task<string> _userDirectoryFetchingTask;
        private string _directory;
        private string _cachedUserDirectory;
        private bool _forceR; // for tests

        public FilesCompletionProvider(string directoryCandidate, ICoreShell coreShell, bool forceR = false) {
            Check.ArgumentNull(nameof(directoryCandidate), directoryCandidate);

            _workflow = coreShell.GetService<IRInteractiveWorkflowProvider>().GetOrCreate();
            _imageService = coreShell.GetService<IImageService>();
            _settings = coreShell.GetService<IRSettings>();
            _forceR = forceR;

            _directory = ExtractDirectory(directoryCandidate);

            if (_directory.Length == 0 || _directory.StartsWithOrdinal("~\\")) {
                _directory = _directory.Length > 1 ? _directory.Substring(2) : _directory;
                _userDirectoryFetchingTask = _workflow.RSession.GetRUserDirectoryAsync();
            }
        }

        #region IRCompletionListProvider
        public bool AllowSorting { get; } = false;

        public IReadOnlyCollection<ICompletionEntry> GetEntries(IRCompletionContext context) {
            var completions = new List<ICompletionEntry>();
            string directory = null;
            string userDirectory = null;

            if (_userDirectoryFetchingTask != null) {
                userDirectory = _userDirectoryFetchingTask.WaitTimeout(500);
                userDirectory = userDirectory ?? _cachedUserDirectory;
            }

            try {
                if (!string.IsNullOrEmpty(userDirectory)) {
                    _cachedUserDirectory = userDirectory;
                    directory = Path.Combine(userDirectory, _directory);
                } else {
                    directory = _directory;
                }

                if (!string.IsNullOrEmpty(directory)) {
                    IEnumerable<ICompletionEntry> entries = null;

                    if (_forceR || _sessionProvider.RSession.IsRemote) {
                        var t = GetRemoteDirectoryItemsAsync(directory);
                        entries = t.WaitTimeout(_forceR ? 5000 : 1000);
                    } else {
                        entries = GetLocalDirectoryItems(directory);
                    }
                    completions.AddRange(entries);
                }
            } catch (IOException) { } catch (UnauthorizedAccessException) { }

            return completions;
        }
        #endregion

        private Task<List<ICompletionEntry>> GetRemoteDirectoryItemsAsync(string directory) {
            return Task.Run(async () => {
                var session = _workflow.RSession;
                var completions = new List<ICompletionEntry>();

                try {
                    var rPath = directory.ToRPath().ToRStringLiteral();
                    var files = await session.EvaluateAsync<JArray>(Invariant($"as.list(list.files(path = {rPath}, include.dirs = FALSE))"), REvaluationKind.Normal);
                    var dirs = await session.EvaluateAsync<JArray>(Invariant($"as.list(list.dirs(path = {rPath}, full.names = FALSE, recursive = FALSE))"), REvaluationKind.Normal);

                    var folderGlyph = _imageService.GetImage(ImageType.OpenFolder);
                    foreach (var d in dirs) {
                        completions.Add(new EditorCompletionEntry((string)d, (string)d + "/", string.Empty, folderGlyph));
                    }
                    foreach (var f in files) {
                        completions.Add(new EditorCompletionEntry((string)f, (string)f, string.Empty, folderGlyph));
                    }

                } catch (RException) { } catch (OperationCanceledException) { }

                return completions;
            });
        }

        private IEnumerable<ICompletionEntry> GetLocalDirectoryItems(string userDirectory) {
            string directory;

            if (!string.IsNullOrEmpty(userDirectory)) {
                _cachedUserDirectory = userDirectory;
                directory = Path.Combine(userDirectory, _directory);
            } else {
                directory = Path.Combine(_settings.WorkingDirectory, _directory);
            }

            if (Directory.Exists(directory)) {
                var folderGlyph = _imageService.GetImage(ImageType.ClosedFolder);

                foreach (string dir in Directory.GetDirectories(directory)) {
                    DirectoryInfo di = new DirectoryInfo(dir);
                    if (!di.Attributes.HasFlag(FileAttributes.Hidden) && !di.Attributes.HasFlag(FileAttributes.System)) {
                        string dirName = Path.GetFileName(dir);
                        yield return new EditorCompletionEntry(dirName, dirName + "/", string.Empty, folderGlyph);
                    }
                }

                foreach (string file in Directory.GetFiles(directory)) {
                    FileInfo di = new FileInfo(file);
                    if (!di.Attributes.HasFlag(FileAttributes.Hidden) && !di.Attributes.HasFlag(FileAttributes.System)) {
                        var fileGlyph = _imageService.GetFileIcon(file);
                        string fileName = Path.GetFileName(file);
                        yield return new EditorCompletionEntry(fileName, fileName, string.Empty, fileGlyph);
                    }
                }
            }
        }

        private string ExtractDirectory(string directory) {
            if (directory.Length > 0) {
                if (directory[0] == '\"' || directory[0] == '\'') {
                    directory = directory.Substring(1);
                }
                if (directory[directory.Length - 1] == '\"' || directory[directory.Length - 1] == '\'') {
                    directory = directory.Substring(0, directory.Length - 1);
                }
            }
            return directory.Replace('/', '\\');
        }
    }
}
