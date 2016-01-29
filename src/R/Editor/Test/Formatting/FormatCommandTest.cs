﻿using System.Diagnostics.CodeAnalysis;
using System.Windows;
using FluentAssertions;
using Microsoft.Languages.Editor;
using Microsoft.Languages.Editor.Controller.Constants;
using Microsoft.Languages.Editor.Services;
using Microsoft.R.Core.Parser;
using Microsoft.R.Editor.ContentType;
using Microsoft.R.Editor.Formatting;
using Microsoft.R.Editor.Formatting.Data;
using Microsoft.R.Editor.Test.Mocks;
using Microsoft.UnitTests.Core.XUnit;
using Microsoft.VisualStudio.Editor.Mocks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.R.Editor.Test.Formatting {
    [ExcludeFromCodeCoverage]
    [Category.R.Formatting]
    public class FormatCommandTest {
        [Test]
        public void FormatDocument() {
            string content = "if(x<1){x<-2}";
            ITextBuffer textBuffer = new TextBufferMock(content, RContentTypeDefinition.ContentType);
            ITextView textView = new TextViewMock(textBuffer);

            using (var command = new FormatDocumentCommand(textView, textBuffer)) {
                var status = command.Status(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.FORMATDOCUMENT);
                status.Should().Be(CommandStatus.SupportedAndEnabled);

                object o = new object();
                command.Invoke(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.FORMATDOCUMENT, null, ref o);
            }

            string actual = textBuffer.CurrentSnapshot.GetText();
            actual.Should().Be("if (x < 1) {\r\n    x <- 2\r\n}");
        }

        [Test]
        public void FormatOnPasteStatus() {
            ITextBuffer textBuffer = new TextBufferMock(string.Empty, RContentTypeDefinition.ContentType);
            ITextView textView = new TextViewMock(textBuffer);
            var clipboard = new ClipboardDataProvider();

            using (var command = new FormatOnPasteCommand(textView, textBuffer)) {
                command.ClipboardDataProvider = clipboard;

                var status = command.Status(VSConstants.GUID_VSStandardCommandSet97, (int)VSConstants.VSStd97CmdID.Paste);
                status.Should().Be(CommandStatus.NotSupported);

                clipboard.Format = DataFormats.UnicodeText;
                clipboard.Data = "data";

                status = command.Status(VSConstants.GUID_VSStandardCommandSet97, (int)VSConstants.VSStd97CmdID.Paste);
                status.Should().Be(CommandStatus.SupportedAndEnabled);
            }
        }

        [Test]
        public void FormatOnPaste01() {
            string actual = FormatFromClipboard("if(x<1){x<-2}");
            actual.Should().Be("if (x < 1) {\r\n    x <- 2\r\n}");
        }

        [Test]
        public void FormatOnPaste02() {
            string content = "\"a\r\nb\r\nc\"";
            string actual = FormatFromClipboard(content);
            actual.Should().Be(content);
        }

        private string FormatFromClipboard(string content) {
            ITextBuffer textBuffer = new TextBufferMock(string.Empty, RContentTypeDefinition.ContentType);
            ITextView textView = new TextViewMock(textBuffer);
            var clipboard = new ClipboardDataProvider();

            using (var command = new FormatOnPasteCommand(textView, textBuffer)) {
                command.ClipboardDataProvider = clipboard;

                clipboard.Format = DataFormats.UnicodeText;
                clipboard.Data = content;

                var ast = RParser.Parse(textBuffer.CurrentSnapshot.GetText());
                var document = new EditorDocumentMock(new EditorTreeMock(textBuffer, ast));

                object o = new object();
                command.Invoke(VSConstants.GUID_VSStandardCommandSet97, (int)VSConstants.VSStd97CmdID.Paste, null, ref o);
            }

            return textBuffer.CurrentSnapshot.GetText();
        }

        class ClipboardDataProvider : IClipboardDataProvider {
            public string Format { get; set; }
            public object Data { get; set; }

            public bool ContainsData(string format) {
                return format == Format;
            }

            public object GetData(string format) {
                return format == Format ? Data : null;
            }
        }
    }
}
