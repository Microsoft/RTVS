﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Languages.Editor.Shell;
using Microsoft.R.Core.AST;
using Microsoft.R.Editor.Test.Mocks;
using Microsoft.R.Editor.Test.Utility;
using Microsoft.UnitTests.Core.XUnit;
using Microsoft.VisualStudio.Text.Editor;
using Xunit;

namespace Microsoft.R.Editor.Test.Formatting {
    [ExcludeFromCodeCoverage]
    [Category.R.SmartIndent]
    public class SmartIndentTest {
        [CompositeTest]
        [InlineData("x <- function(a) {\n", 1, 4)]
        [InlineData("x <- function(a) {\n\n", 2, 4)]
        [InlineData("x <- function(a) {\n\n\n", 3, 4)]
        [InlineData("x <- function(a,\nb) {\n", 2, 4)]
        [InlineData("x <- function(a, b, c,\n              d) {\n}", 2, 0)]
        [InlineData("x <- function(a, b, c,\n              d) {\n\n}", 2, 4)]
        [InlineData("x <- function(a, b, c,\nd) {\n\n}", 2, 4)]
        [InlineData("x <- function(a, b,\n   c,\n", 2, 3)]
        [InlineData("x <- function(a, b, c,\nd) {\n\n}", 2, 4)]
        [InlineData("{\n", 1, 4)]
        [InlineData("{\n    {\n", 2, 8)]
        [InlineData("{\n    {\n    {\n", 3, 8)]
        [InlineData("{\n    {\n        {\n", 3, 12)]
        [InlineData("{\n\n}", 1, 4)]
        [InlineData("{\n    {\n\n    }\n}", 2, 8)]
        [InlineData("{\n\n}\n", 3, 0)]
        [InlineData("{\n    {\n\n    }\n}", 4, 0)]
        [InlineData("{\n    {\n\n    }\n\n}", 4, 4)]
        [InlineData("if(1) {\n", 1, 4)]
        [InlineData("if(1) {\n", 1, 4)]
        [InlineData("library(abind)\n", 1, 0)]
        [InlineData("if (x > 1) {\n\n}", 2, 0)]
        [InlineData("while (TRUE) {\n    if (x > 1) {\n\n    }\n}", 4, 0)]
        [InlineData("if (x > 1) {\r\n\r\n}", 1, 4)]
        [InlineData("x <- function(a,\n", 1, 14)]
        [InlineData("func(a,\n", 1, 5)]
        [InlineData("if (TRUE)\n    x <- 1\n\n", 3, 0)]
        [InlineData("repeat\r\n    if (x > 1)\r\n", 2, 8)]
        [InlineData("{if (x > 1)\r\n    x <- 1\r\nelse\n", 3, 4)]
        [InlineData("if (x > 1)\n", 1, 4)]
        [InlineData("x <- function(a) {\n  if(TRUE)\n\n}", 2, 6)]
        [InlineData("x <- function(a) {}\n", 1, 0)]
        public void Scope(string content, int lineNum, int expectedIndent) {
            int? indent = GetSmartIndent(content, lineNum);
            indent.Should().HaveValue().And.Be(expectedIndent);
        }

        private int? GetSmartIndent(string content, int lineNumber) {
            AstRoot ast;
            ITextView textView = TextViewTest.MakeTextView(content, 0, out ast);
            var document = new EditorDocumentMock(new EditorTreeMock(textView.TextBuffer, ast));

            ISmartIndentProvider provider = EditorShell.Current.ExportProvider.GetExport<ISmartIndentProvider>().Value;
            ISmartIndent indenter = provider.CreateSmartIndent(textView);

            return indenter.GetDesiredIndentation(textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber));
        }
    }
}
