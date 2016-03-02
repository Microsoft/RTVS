﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Microsoft.Languages.Editor.Services;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.R.Editor.QuickInfo
{
    internal sealed class QuickInfoController : IIntellisenseController
    {
        private ITextView _textView;
        private IList<ITextBuffer> _subjectBuffers;
        private IQuickInfoBroker _quickInfoBroker;

        public QuickInfoController(ITextView textView, IList<ITextBuffer> subjectBuffers, IQuickInfoBroker quickInfoBroker)
        {
            _quickInfoBroker = quickInfoBroker;
            _textView = textView;
            _subjectBuffers = subjectBuffers;

            _textView.MouseHover += OnViewMouseHover;
            _textView.TextBuffer.Changing += OnTextBufferChanging;

            ServiceManager.AddService<QuickInfoController>(this, textView);
        }

        private void OnTextBufferChanging(object sender, TextContentChangingEventArgs e)
        {
            if (_quickInfoBroker.IsQuickInfoActive(_textView))
            {
                var sessions = _quickInfoBroker.GetSessions(_textView);

                foreach (var session in sessions)
                    session.Dismiss();
            }

        }

        void OnViewMouseHover(object sender, MouseHoverEventArgs e)
        {
            //find the mouse position by mapping down to the subject buffer
            SnapshotPoint? point = _textView.BufferGraph.MapDownToFirstMatch
                 (new SnapshotPoint(_textView.TextSnapshot, e.Position),
                PointTrackingMode.Positive,
                snapshot => _subjectBuffers.Contains(snapshot.TextBuffer),
                PositionAffinity.Predecessor);

            if (point != null)
            {
                ITrackingPoint triggerPoint = point.Value.Snapshot.CreateTrackingPoint(point.Value.Position,
                PointTrackingMode.Positive);

                if (!_quickInfoBroker.IsQuickInfoActive(_textView))
                    _quickInfoBroker.TriggerQuickInfo(_textView, triggerPoint, true);
            }
        }

        #region IIntellisenseController

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer) { }
        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer) { }

        public void Detach(ITextView textView)
        {
            if (textView == _textView)
            {
                _textView.TextBuffer.Changing -= OnTextBufferChanging;
                _textView.MouseHover -= OnViewMouseHover;
                _textView = null;
            }
        }
        #endregion
    }
}
