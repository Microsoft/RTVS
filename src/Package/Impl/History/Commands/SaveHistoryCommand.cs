﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.Languages.Editor.Controller.Command;
using Microsoft.R.Components.Controller;
using Microsoft.R.Components.History;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Support.Settings;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Text.Editor;
#if VS14
using Microsoft.VisualStudio.ProjectSystem.Utilities;
#endif

namespace Microsoft.VisualStudio.R.Package.History.Commands {
    internal class SaveHistoryCommand : ViewCommand {
        private readonly IRInteractiveWorkflow _interactiveWorkflow;
        private readonly IRHistory _history;

        public SaveHistoryCommand(ITextView textView, IRHistoryProvider historyProvider, IRInteractiveWorkflow interactiveWorkflow)
            : base(textView, RGuidList.RCmdSetGuid, RPackageCommandId.icmdSaveHistory, false) {
            _interactiveWorkflow = interactiveWorkflow;
            _history = historyProvider.GetAssociatedRHistory(textView);
        }

        public override Microsoft.R.Components.Controller.CommandStatus Status(Guid guid, int id) {
            return _interactiveWorkflow.ActiveWindow != null && _history.HasEntries
                ? Microsoft.R.Components.Controller.CommandStatus.SupportedAndEnabled 
                : Microsoft.R.Components.Controller.CommandStatus.Supported;
        }

        public override CommandResult Invoke(Guid group, int id, object inputArg, ref object outputArg) {
            var initialPath = RToolsSettings.Current.WorkingDirectory != null ? PathHelper.EnsureTrailingSlash(RToolsSettings.Current.WorkingDirectory) : null;
            var file = VsAppShell.Current.BrowseForFileSave(IntPtr.Zero, Resources.HistoryFileFilter, initialPath, Resources.SaveHistoryAsTitle);
            if (file != null) {
                _history.TrySaveToFile(file);
            }

            return CommandResult.Executed;
        }
    }
}