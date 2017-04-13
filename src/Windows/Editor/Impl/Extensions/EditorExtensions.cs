﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq;
using Microsoft.Languages.Editor.Controllers;
using Microsoft.Languages.Editor.Controllers.Views;
using Microsoft.Languages.Editor.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.Languages.Editor.Document {
    public static class EditorExtensions {
        /// <summary>
        /// Given text view buffer and the content type, locates document 
        /// in the underlying  text buffer graph.
        /// </summary>
        public static T FindInProjectedBuffers<T>(ITextBuffer viewBuffer, string contentType) where T : class, IEditorDocument {
            if (viewBuffer.ContentType.IsOfType(contentType)) {
                return viewBuffer.GetService<T>();
            }

            T document = null;
            ITextBuffer rBuffer = null;
            var pb = viewBuffer as IProjectionBuffer;
            if (pb != null) {
                rBuffer = pb.SourceBuffers.FirstOrDefault((ITextBuffer tb) => {
                    if (tb.ContentType.IsOfType(contentType)) {
                        document = tb.GetService<T>();
                        if (document != null) {
                            return true;
                        }
                    }
                    return false;
                });
            }
            return document;
        }

        public static T TryFromTextBuffer<T>(ITextBuffer textBuffer, string contentType) where T : class, IEditorDocument {
            var document = textBuffer.GetService<T>();
            if (document == null) {
                document = FindInProjectedBuffers<T>(textBuffer, contentType);
                if (document == null) {
                    TextViewData viewData = TextViewConnectionListener.GetTextViewDataForBuffer(textBuffer);
                    if (viewData != null && viewData.LastActiveView != null) {
                        var controller = ViewController.FromTextView(viewData.LastActiveView);
                        if (controller != null && controller.TextBuffer != null) {
                            document = controller.TextBuffer.GetService<T>();
                        }
                    }
                }
            }
            return document;
        }

        public static ITextView GetFirstView(this ITextBuffer textBuffer) => TextViewConnectionListener.GetFirstViewForBuffer(textBuffer);
    }
}
