﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.Languages.Editor.Controller.Command;
using Microsoft.R.Components.Controller;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.R.Package.Repl.Commands {
    public sealed class SendToReplCommand : ViewCommand {
        private readonly IRInteractiveWorkflow _interactiveWorkflow;
        private string _leadingArgument;
        private string _closingArgument;

        public SendToReplCommand(ITextView textView, IRInteractiveWorkflow interactiveWorkflow, string leadingArgument = "", string closingArgument = "") :
            base(textView, new CommandId(RGuidList.RCmdSetGuid, (int)RPackageCommandId.icmdSendToRepl), false) { 
            _interactiveWorkflow = interactiveWorkflow;
            _leadingArgument = leadingArgument;
            _closingArgument = closingArgument;

        }

        public override CommandStatus Status(Guid group, int id) {
            return (TextView.Selection.Mode == TextSelectionMode.Stream) ? CommandStatus.SupportedAndEnabled : CommandStatus.Supported;
        }

        public override CommandResult Invoke(Guid group, int id, object inputArg, ref object outputArg) {
            ITextSelection selection = TextView.Selection;
            ITextSnapshot snapshot = TextView.TextBuffer.CurrentSnapshot;
            int position = selection.Start.Position;
            ITextSnapshotLine line = snapshot.GetLineFromPosition(position);

            var window = _interactiveWorkflow.ActiveWindow;
            if (window == null) {
                return CommandResult.Disabled;
            }

            string text;
            if (selection.StreamSelectionSpan.Length == 0) {
                text = line.GetText();
            } else {
                text = TextView.Selection.StreamSelectionSpan.GetText();
                line = TextView.Selection.End.Position.GetContainingLine();
            }

            window.Container.Show(false);
            if (text.Trim().Length > 0)
            {
                _interactiveWorkflow.Operations.EnqueueExpression(_leadingArgument + text + _closingArgument, true);
            } else {
                _interactiveWorkflow.Operations.EnqueueExpression(text, true);
            }


            var targetLine = line;
            while (targetLine.LineNumber < snapshot.LineCount - 1) {
                targetLine = snapshot.GetLineFromLineNumber(targetLine.LineNumber + 1);
                // skip over blank lines, unless it's the last line, in which case we want to land on it no matter what
                if (!string.IsNullOrWhiteSpace(targetLine.GetText()) || targetLine.LineNumber == snapshot.LineCount - 1) {
                    TextView.Caret.MoveTo(new SnapshotPoint(snapshot, targetLine.Start));
                    TextView.Caret.EnsureVisible();
                    break;
                }
            }

            // Take focus back if REPL window has stolen it
            if (!TextView.HasAggregateFocus) {
                IVsEditorAdaptersFactoryService adapterService = VsAppShell.Current.ExportProvider.GetExportedValue<IVsEditorAdaptersFactoryService>();
                IVsTextView tv = adapterService.GetViewAdapter(TextView);
                tv.SendExplicitFocus();
            }
            
            return CommandResult.Executed;
        }
    }
}
