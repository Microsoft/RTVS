﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Languages.Core.Utility;
using Microsoft.Languages.Editor.Completion;
using Microsoft.Languages.Editor.Shell;
using Microsoft.R.Core.AST;
using Microsoft.R.Editor.Completion.Definitions;
using Microsoft.R.Editor.Completion.Engine;
using Microsoft.R.Editor.Document;
using Microsoft.R.Editor.Document.Definitions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.R.Editor.Completion {
    using Core.Tokens;
    using Languages.Editor.Services;
    using Completion = VisualStudio.Language.Intellisense.Completion;

    /// <summary>
    /// Provides actual content for the intellisense dropdown
    /// </summary>
    public sealed class RCompletionSource : ICompletionSource {
        private static readonly string _asyncIntellisenseSession = "Async R Intellisense Session";
        private ITextBuffer _textBuffer;
        private ICompletionSession _asyncSession;

        public RCompletionSource(ITextBuffer textBuffer) {
            _textBuffer = textBuffer;
            _textBuffer.Changed += OnTextBufferChanged;
        }

        /// <summary>
        /// Primary entry point for intellisense. This is where intellisense list is getting created.
        /// </summary>
        /// <param name="session">Completion session</param>
        /// <param name="completionSets">Completion sets to populate</param>
        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets) {
            Debug.Assert(EditorShell.IsUIThread);

            if (_asyncSession != null) {
                return;
            }

            IREditorDocument doc = REditorDocument.TryFromTextBuffer(_textBuffer);
            if (doc == null) {
                return;
            }

            int position = session.GetTriggerPoint(_textBuffer).GetPosition(_textBuffer.CurrentSnapshot);
            if (!doc.EditorTree.IsReady) {
                // Parsing is pending. Make completion async.
                CreateAsyncSession(doc, position, session, completionSets);
            } else {
                PopulateCompletionList(position, session, completionSets, doc.EditorTree.AstRoot);
            }
        }

        private void CreateAsyncSession(IREditorDocument document, int position, ICompletionSession session, IList<CompletionSet> completionSets) {
            _asyncSession = session;
            _asyncSession.Properties.AddProperty(_asyncIntellisenseSession, String.Empty);
            document.EditorTree.ProcessChangesAsync(TreeUpdatedCallback);
        }
        private void TreeUpdatedCallback() {
            if (_asyncSession == null) {
                return;
            }

            RCompletionController controller = ServiceManager.GetService<RCompletionController>(_asyncSession.TextView);
            _asyncSession = null;
            if (controller != null) {
                controller.ShowCompletion(autoShownCompletion: true);
                controller.FilterCompletionSession();
            }
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e) {
            DismissAsyncSession();
        }

        private void DismissAsyncSession() {
           if (_asyncSession != null && _asyncSession.Properties != null && _asyncSession.Properties.ContainsProperty(_asyncIntellisenseSession) && !_asyncSession.IsDismissed) {
                RCompletionController controller = ServiceManager.GetService<RCompletionController>(_asyncSession.TextView);
                if (controller != null) {
                    controller.DismissCompletionSession();
                }
            }
            _asyncSession = null;
        }

        internal void PopulateCompletionList(int position, ICompletionSession session, IList<CompletionSet> completionSets, AstRoot ast) {
            RCompletionContext context = new RCompletionContext(session, _textBuffer, ast, position);

            bool autoShownCompletion = true;
            if (session.TextView.Properties.ContainsProperty(CompletionController.AutoShownCompletion))
                autoShownCompletion = session.TextView.Properties.GetProperty<bool>(CompletionController.AutoShownCompletion);

            IReadOnlyCollection<IRCompletionListProvider> providers =
                RCompletionEngine.GetCompletionForLocation(context, autoShownCompletion);

            Span applicableSpan = GetApplicableSpan(position, session);
            ITrackingSpan trackingSpan = _textBuffer.CurrentSnapshot.CreateTrackingSpan(applicableSpan, SpanTrackingMode.EdgeInclusive);
            List<RCompletion> completions = new List<RCompletion>();
            bool sort = true;

            foreach (IRCompletionListProvider provider in providers) {
                IReadOnlyCollection<RCompletion> entries = provider.GetEntries(context);
                Debug.Assert(entries != null);

                if (entries.Count > 0) {
                    completions.AddRange(entries);
                }
                sort &= provider.AllowSorting;
            }

            if (sort) {
                completions.Sort(RCompletion.Compare);
                completions.RemoveDuplicates();
            }

            CompletionSet completionSet = new RCompletionSet(_textBuffer, trackingSpan, completions);
            completionSets.Add(completionSet);
        }

        /// <summary>
        /// Calculates span in the text buffer that contains data
        /// applicable to the current completion session. A tracking
        /// span will be created over it and editor will grow and shrink
        /// tracking span as user types and filter completion session
        /// based on the data inside the tracking span.
        /// </summary>
        private Span GetApplicableSpan(int position, ICompletionSession session) {
            var selectedSpans = session.TextView.Selection.SelectedSpans;
            if (selectedSpans.Count == 1 && selectedSpans[0].Span.Length > 0) {
                return selectedSpans[0].Span;
            }

            ITextSnapshot snapshot = _textBuffer.CurrentSnapshot;
            ITextSnapshotLine line = snapshot.GetLineFromPosition(position);
            string lineText = snapshot.GetText(line.Start, line.Length);
            int linePosition = position - line.Start;

            int start = 0;
            int end = line.Length;

            for (int i = linePosition - 1; i >= 0; i--) {
                char ch = lineText[i];
                if (!RTokenizer.IsIdentifierCharacter(ch)) {
                    start = i + 1;
                    break;
                }
            }

            for (int i = linePosition; i < lineText.Length; i++) {
                char ch = lineText[i];
                if (!RTokenizer.IsIdentifierCharacter(ch)) {
                    end = i;
                    break;
                }
            }

            if (start < end) {
                return new Span(start + line.Start, end - start);
            }

            return new Span(position, 0);
        }

        #region Dispose
        public void Dispose() {
            if (_textBuffer != null) {
                _textBuffer.Changed -= OnTextBufferChanged;
                _textBuffer = null;
            }
        }
        #endregion
    }
}
