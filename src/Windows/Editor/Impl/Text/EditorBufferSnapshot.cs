﻿using Microsoft.Languages.Core.Text;
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.Languages.Editor.Text {
    public sealed class EditorBufferSnapshot: TextProvider, IEditorBufferSnapshot {
        private readonly ITextSnapshot _snapshot;
        public EditorBufferSnapshot(IEditorBuffer editorBuffer, ITextSnapshot snapshot): base(snapshot) {
            _snapshot = snapshot;
        }

        public T As<T>() where T:class => _snapshot as T;
        public IEditorBuffer EditorBuffer => _snapshot.TextBuffer.ToEditorBuffer();
        public int LineCount => _snapshot.LineCount;
        public IEditorLine GetLineFromLineNumber(int lineNumber) => new EditorLine(_snapshot.GetLineFromLineNumber(lineNumber));
        public IEditorLine GetLineFromPosition(int position) => new EditorLine(_snapshot.GetLineFromPosition(position));
        public int GetLineNumberFromPosition(int position) => _snapshot.GetLineNumberFromPosition(position);
        public ITrackingTextRange CreateTrackingRange(ITextRange range) 
            => new TrackingTextRange(_snapshot.CreateTrackingSpan(range.ToSpan(), SpanTrackingMode.EdgeInclusive));
    }
}
