﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.Languages.Core.Text;
using Microsoft.Languages.Editor;
using Microsoft.Languages.Editor.Controller.Command;
using Microsoft.Languages.Editor.Controller.Constants;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.R.Editor.Comments
{
    internal class UncommentCommand : EditingCommand
    {
        internal UncommentCommand(ITextView textView, ITextBuffer textBuffer)
            : base(textView, new CommandId(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK))
        {
        }

        #region ICommand
        public override CommandResult Invoke(Guid group, int id, object inputArg, ref object outputArg)
        {
            SnapshotSpan selectionSpan = TextView.Selection.StreamSelectionSpan.SnapshotSpan;

            RCommenter.UncommentBlock(TextView, TextView.TextBuffer,
                new TextRange(selectionSpan.Start.Position, selectionSpan.Length));

            return CommandResult.Executed;
        }

        public override CommandStatus Status(Guid group, int id)
        {
            return CommandStatus.SupportedAndEnabled;
        }
        #endregion
    }
}
