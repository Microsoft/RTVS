﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.Common.Core;
using Microsoft.Common.Core.Services;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.Languages.Editor.Text {
    public sealed class EditorView : IEditorView {
        private readonly Lazy<PropertyDictionary> _properties = Lazy.Create(() => new PropertyDictionary());
        private readonly Lazy<ServiceManager> _services = Lazy.Create(() => new ServiceManager());
        private readonly ITextView _textView;

        public EditorView(ITextView textView) {
            _textView = textView;
            Selection = new EditorSelection(textView);
        }

        public IViewCaret Caret => new ViewCaret(_textView.Caret);

        public IEditorBuffer EditorBuffer => _textView.TextBuffer.ToEditorBuffer();
        public PropertyDictionary Properties => _properties.Value;
        public IServiceManager Services => _services.Value;
        public IEditorSelection Selection { get; }
        public T As<T>() where T : class => _textView as T;

        public ISnapshotPoint GetCaretPosition(IEditorBuffer editorBuffer) {
            var point = _textView.GetCaretPosition(editorBuffer);
            return point.HasValue ? new EditorSnapshotPoint(point.Value, editorBuffer) : null;
        }
    }
}