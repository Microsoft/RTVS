﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Languages.Core.Formatting;
using Microsoft.Languages.Core.Text;
using Microsoft.Languages.Editor.Shell;
using Microsoft.Languages.Editor.Test.Text;
using Microsoft.Languages.Editor.Text;
using Microsoft.R.Core.AST;
using Microsoft.R.Editor.Formatting;
using Microsoft.R.Editor.SmartIndent;
using Microsoft.R.Editor.Test.Mocks;
using Microsoft.R.Editor.Test.Utility;
using Microsoft.UnitTests.Core.XUnit;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Xunit;

namespace Microsoft.R.Editor.Test.Formatting {
    [ExcludeFromCodeCoverage]
    [Category.R.Formatting]
    public class AutoFormatTest {
        [CompositeTest]
        [InlineData("x<-function(x,y,", 16, "x <- function(x, y,\n")]
        [InlineData("'x<-1'", 5, "'x<-1\n'")]
        [InlineData("x<-1", 4, "x <- 1\n")]
        public void FormatTest(string content, int position, string expected) {
            ITextView textView = TestAutoFormat(position, content);

            string actual = textView.TextBuffer.CurrentSnapshot.GetText();
            actual.Should().Be(expected);
        }

        [Test]
        public void SmartIndentTest05() {
            AstRoot ast;
            ITextView textView = TextViewTest.MakeTextView("  x <- 1\r\n", 0, out ast);
            var document = new EditorDocumentMock(new EditorTreeMock(textView.TextBuffer, ast));

            ISmartIndentProvider provider = EditorShell.Current.ExportProvider.GetExportedValue<ISmartIndentProvider>();
            SmartIndenter indenter = (SmartIndenter)provider.CreateSmartIndent(textView);

            int? indent = indenter.GetDesiredIndentation(textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(1), IndentStyle.Block);

            indent.Should().HaveValue().And.Be(2);
        }

        private ITextView TestAutoFormat(int position, string initialContent = "") {
            AstRoot ast;
            ITextView textView = TextViewTest.MakeTextView(initialContent, position, out ast);

            textView.TextBuffer.Changed += (object sender, TextContentChangedEventArgs e) => {
                List<TextChangeEventArgs> textChanges = TextUtility.ConvertToRelative(e);
                ast.ReflectTextChanges(textChanges);

                if (e.Changes[0].NewText.Length == 1) {
                    char ch = e.Changes[0].NewText[0];
                    if (AutoFormat.IsPreProcessAutoformatTriggerCharacter(ch)) {
                        position = e.Changes[0].OldPosition + 1;
                        FormatOperations.FormatCurrentLine(textView, textView.TextBuffer);
                        textView.Caret.MoveTo(new SnapshotPoint(e.After, position));
                    }
                } else {
                    ITextSnapshotLine line = e.After.GetLineFromPosition(position);
                    textView.Caret.MoveTo(new SnapshotPoint(e.After, Math.Min(e.After.Length, line.Length + 1)));
                }
            };

            Typing.Type(textView.TextBuffer, position, "\n");

            return textView;
        }
    }
}
