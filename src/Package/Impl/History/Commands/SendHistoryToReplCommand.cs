﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.Languages.Editor;
using Microsoft.Languages.Editor.Controller.Command;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Repl;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.R.Package.History.Commands {
    internal class SendHistoryToReplCommand : ViewCommand {
        private readonly IRHistory _history;

        public SendHistoryToReplCommand(ITextView textView, IRHistoryProvider historyProvider)
            : base(textView, new [] {
                new CommandId(RGuidList.RCmdSetGuid, RPackageCommandId.icmdSendHistoryToRepl),
                new CommandId(VSConstants.VsStd11, (int)VSConstants.VSStd11CmdID.ExecuteLineInInteractive)
            }, false) {
            _history = historyProvider.GetAssociatedRHistory(textView);
        }

        public override CommandStatus Status(Guid guid, int id) {
            return ReplWindow.ReplWindowExists && (_history.HasSelectedEntries || !TextView.Selection.IsEmpty)
                ? CommandStatus.SupportedAndEnabled
                : CommandStatus.Supported;
        }

        public override CommandResult Invoke(Guid group, int id, object inputArg, ref object outputArg) {
            _history.SendSelectedToRepl();
            return CommandResult.Executed;
        }
    }
}