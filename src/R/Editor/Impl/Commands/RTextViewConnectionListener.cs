﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Languages.Editor.Controller;
using Microsoft.Languages.Editor.EditorFactory;
using Microsoft.Languages.Editor.Services;
using Microsoft.R.Editor.Document;
using Microsoft.R.Editor.Document.Definitions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.R.Editor.Commands
{
    // In HTML case document creation and controller connection happens either in
    // application-specific listener or in text buffer / editor factory.
    public class RTextViewConnectionListener : TextViewConnectionListener
    {
        protected override void OnTextViewConnected(ITextView textView, ITextBuffer textBuffer)
        {
            RMainController.Attach(textView, textBuffer);

            base.OnTextViewConnected(textView, textBuffer);
        }

        protected override void OnTextBufferDisposing(ITextBuffer textBuffer)
        {
            IEditorInstance editorInstance = ServiceManager.GetService<IEditorInstance>(textBuffer);

            if (editorInstance != null)
            {
                editorInstance.Dispose();
            }
            else
            {
                IREditorDocument doc = REditorDocument.TryFromTextBuffer(textBuffer);
                if (doc != null)
                {
                    doc.Dispose();
                }
            }

            base.OnTextBufferDisposing(textBuffer);
        }
    }
}