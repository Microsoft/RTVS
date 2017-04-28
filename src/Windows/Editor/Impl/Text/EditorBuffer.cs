﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using Microsoft.Common.Core;
using Microsoft.Common.Core.Diagnostics;
using Microsoft.Common.Core.Services;
using Microsoft.Languages.Core.Text;
using Microsoft.Languages.Editor.Controllers;
using Microsoft.Languages.Editor.Controllers.Views;
using Microsoft.Languages.Editor.Document;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.Languages.Editor.Text {
    /// <summary>
    /// Implements IEditorBuffer abstraction over Visual Studio text buffer.
    /// </summary>
    public sealed class EditorBuffer : IEditorBuffer {
        private const string Key = "XEditorBuffer";

        private readonly Lazy<PropertyDictionary> _properties = Lazy.Create(() => new PropertyDictionary());
        private readonly Lazy<ServiceManager> _services = Lazy.Create(() => new ServiceManager());
        private readonly ITextBuffer _textBuffer;
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;

        public EditorBuffer(ITextBuffer textBuffer, ITextDocumentFactoryService textDocumentFactoryService = null) {
            Check.ArgumentNull(nameof(textBuffer), textBuffer);
            Check.ArgumentNull(nameof(textDocumentFactoryService), textDocumentFactoryService);

            _textBuffer = textBuffer;
            _textBuffer.ChangedHighPriority += OnTextBufferChangedHighPriority;
            _textBuffer.Changed += OnTextBufferChanged;
            _textBuffer.Properties[Key] = this;

            _textDocumentFactoryService = textDocumentFactoryService;
            if (_textDocumentFactoryService != null) {
                _textDocumentFactoryService.TextDocumentDisposed += OnTextDocumentDisposed;
            }
        }

        public static IEditorBuffer Create(ITextBuffer textBuffer, ITextDocumentFactoryService textDocumentFactoryService)
            => textBuffer.ToEditorBuffer() ?? new EditorBuffer(textBuffer, textDocumentFactoryService);

        public static IEditorBuffer FromTextBuffer(ITextBuffer textBuffer)
            => textBuffer.Properties.TryGetProperty(Key, out IEditorBuffer buffer) ? buffer : null;

        #region IEditorBuffer
        public string ContentType => _textBuffer.ContentType.TypeName;

        public IEditorBufferSnapshot CurrentSnapshot => new EditorBufferSnapshot(this, _textBuffer.CurrentSnapshot);
        public PropertyDictionary Properties => _properties.Value;
        public IServiceManager Services => _services.Value;

        /// <summary>
        /// Path to the file being edited, if any.
        /// </summary>
        public string FilePath => _textBuffer.GetFileName();

        /// <summary>
        /// Returns underlying platform object such as ITextBuffer in Visual Studio.
        /// May return null if there is no underlying implementation.
        /// </summary>
        public T As<T>() where T : class => _textBuffer as T;

        /// <summary>
        /// Attempts to locate associated editor document. Implementation depends on the platform.
        /// </summary>
        /// <typeparam name="T">Type of the document to locate</typeparam>
        public T GetEditorDocument<T>() where T : class, IEditorDocument {
            var document = Services.GetService<T>();
            if (document == null) {
                document = FindInProjectedBuffers<T>(_textBuffer);
                if (document == null) {
                    var viewData = TextViewConnectionListener.GetTextViewDataForBuffer(_textBuffer);
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

        public void Insert(int position, string text) => _textBuffer.Insert(position, text);
        public void Replace(ITextRange range, string text) => _textBuffer.Replace(range.ToSpan(), text);
        public void Delete(ITextRange range) => _textBuffer.Delete(range.ToSpan());

        public event EventHandler<TextChangeEventArgs> ChangedHighPriority;
        public event EventHandler<TextChangeEventArgs> Changed;
        public event EventHandler Closing;
        #endregion

        #region IDisposable
        public void Dispose() {
            _textBuffer.ChangedHighPriority -= OnTextBufferChangedHighPriority;
            _textBuffer.Changed -= OnTextBufferChanged;
            _textBuffer.Properties.RemoveProperty(typeof(IEditorBuffer));

            if (_textDocumentFactoryService != null) {
                _textDocumentFactoryService.TextDocumentDisposed -= OnTextDocumentDisposed;
            }
            Closing?.Invoke(this, EventArgs.Empty);

            if (_services.IsValueCreated) {
                _services.Value.Dispose();
            }
        }
        #endregion

        private T FindInProjectedBuffers<T>(ITextBuffer textBuffer) where T : class, IEditorDocument {
            var pb = textBuffer as IProjectionBuffer;
            return pb?.SourceBuffers.Select((tb) => tb.GetService<T>()).FirstOrDefault(x => x != null);
        }

        private void OnTextBufferChangedHighPriority(object sender, TextContentChangedEventArgs e) {
            var changes = e.ConvertToRelative();
            foreach (var ch in changes) {
                ChangedHighPriority?.Invoke(this, ch);
            }
        }

        private void OnTextDocumentDisposed(object sender, TextDocumentEventArgs e) {
            if (e.TextDocument.TextBuffer == _textBuffer) {
                Dispose();
            }
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e) {
            var changes = e.ConvertToRelative();
            foreach (var ch in changes) {
                Changed?.Invoke(this, ch);
            }
        }
    }
}
