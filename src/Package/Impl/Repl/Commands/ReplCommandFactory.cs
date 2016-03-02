﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Microsoft.Languages.Editor.BraceMatch;
using Microsoft.Languages.Editor.Controller;
using Microsoft.R.Editor.Commands;
using Microsoft.R.Editor.Formatting;
using Microsoft.R.Editor.Selection;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.R.Package.Repl.Commands {
    internal sealed class ReplCommandFactory : ICommandFactory {
        public IEnumerable<ICommand> GetCommands(ITextView textView, ITextBuffer textBuffer) {
            return new ICommand[] {
                new GotoBraceCommand(textView, textBuffer),
                new WorkingDirectoryCommand(),
                new HistoryNavigationCommand(textView),
                new ReplFormatDocumentCommand(textView, textBuffer),
                new FormatSelectionCommand(textView, textBuffer),
                new FormatOnPasteCommand(textView, textBuffer),
                new SendToReplCommand(textView),
                new RTypingCommandHandler(textView),
                new RCompletionCommandHandler(textView),
                new ExecuteCurrentCodeCommand(textView),
                new PasteCurrentCodeCommand(textView),
                new SelectWordCommand(textView, textBuffer)
            };
        }
    }
}
