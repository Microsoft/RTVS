﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Languages.Editor.Services;
using Microsoft.Languages.Editor.Utility;
using Microsoft.R.Core.AST;
using Microsoft.R.Editor.Document;
using Microsoft.R.Editor.Settings;
using Microsoft.R.Support.Help.Definitions;
using Microsoft.R.Support.Help.Functions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.R.Editor.Signatures {
    sealed class SignatureHelpSource : ISignatureHelpSource
    {
        ITextBuffer _textBuffer;

        public SignatureHelpSource(ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
            ServiceManager.AddService<SignatureHelpSource>(this, textBuffer);
        }

        #region ISignatureHelpSource
        public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
        {
            if (!REditorSettings.SignatureHelpEnabled || session.IsDismissed)
                return;

            var document = REditorDocument.TryFromTextBuffer(_textBuffer);
            if (document != null)
            {
                document.EditorTree.EnsureTreeReady();
                AugmentSignatureHelpSession(session, signatures, document.EditorTree.AstRoot, TriggerSignatureHelp);
            }
        }

        public bool AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures, AstRoot ast, Action<object> triggerSession)
        {
            ITextSnapshot snapshot = _textBuffer.CurrentSnapshot;
            int position = session.GetTriggerPoint(_textBuffer).GetPosition(snapshot);

            // Retrieve parameter positions from the current text buffer snapshot
            ParameterInfo parametersInfo = SignatureHelp.GetParametersInfoFromBuffer(ast, snapshot, position);
            if (parametersInfo != null)
            {
                position = Math.Min(parametersInfo.SignatureEnd, position);
                int start = Math.Min(position, snapshot.Length);
                int end = Math.Min(parametersInfo.SignatureEnd, snapshot.Length);

                ITrackingSpan applicableToSpan = snapshot.CreateTrackingSpan(Span.FromBounds(start, end), SpanTrackingMode.EdgeInclusive);

                // Get collection of function signatures from documentation (parsed RD file)
                IFunctionInfo functionInfo = FunctionIndex.GetFunctionInfo(parametersInfo.FunctionName, triggerSession, session.TextView);
                if (functionInfo != null && functionInfo.Signatures != null)
                {
                    foreach (ISignatureInfo signatureInfo in functionInfo.Signatures)
                    {
                        ISignature signature = CreateSignature(session, functionInfo, signatureInfo, applicableToSpan, ast, position);
                        signatures.Add(signature);
                    }

                    session.Properties["functionInfo"] = functionInfo;
                    return true;
                }
            }

            return false;
        }

        private void TriggerSignatureHelp(object o)
        {
            SignatureHelp.TriggerSignatureHelp(o as ITextView);
        }

        public ISignature GetBestMatch(ISignatureHelpSession session)
        {
            if (session.Signatures.Count > 0)
            {
                ITrackingSpan applicableToSpan = session.Signatures[0].ApplicableToSpan;
                string text = applicableToSpan.GetText(applicableToSpan.TextBuffer.CurrentSnapshot);

                var typedText = text.Trim();
                foreach (var sig in session.Signatures)
                {
                    var jsSig = sig as SignatureHelp;

                    if (jsSig != null && jsSig.FunctionName.StartsWith(text, StringComparison.Ordinal))
                    {
                        return sig;
                    }
                }
            }

            return null;
        }
        #endregion

        private ISignature CreateSignature(ISignatureHelpSession session,
                                       IFunctionInfo functionInfo, ISignatureInfo signatureInfo,
                                       ITrackingSpan span, AstRoot ast, int position)
        {
            SignatureHelp sig = new SignatureHelp(session, _textBuffer, functionInfo.Name, string.Empty, signatureInfo);
            List<IParameter> paramList = new List<IParameter>();

            // Locus points in the pretty printed signature (the one displayed in the tooltip)
            var locusPoints = new List<int>();
            string signatureString = signatureInfo.GetSignatureString(locusPoints);
            sig.Content = signatureString;
            sig.ApplicableToSpan = span;

            sig.Documentation = functionInfo.Description.Wrap(Math.Min(SignatureInfo.MaxSignatureLength, sig.Content.Length));

            Debug.Assert(locusPoints.Count == signatureInfo.Arguments.Count + 1);
            for (int i = 0; i < signatureInfo.Arguments.Count; i++)
            {
                IArgumentInfo p = signatureInfo.Arguments[i];
                if (p != null)
                {
                    int locusStart = locusPoints[i];
                    int locusLength = locusPoints[i + 1] - locusStart;

                    Debug.Assert(locusLength >= 0);
                    Span locus = new Span(locusStart, locusLength);

                    /// VS may end showing very long tooltip so we need to keep 
                    /// description reasonably short: typically about
                    /// same length as the function signature.
                    paramList.Add(
                        new SignatureParameter(
                            p.Description.Wrap(
                                Math.Min(SignatureInfo.MaxSignatureLength, sig.Content.Length)),
                                locus, locus, p.Name, sig));
                }
            }

            sig.Parameters = new ReadOnlyCollection<IParameter>(paramList);
            sig.ComputeCurrentParameter(ast, position);
            
            return sig;
        }

        #region IDisposable
        public void Dispose()
        {
            if (_textBuffer != null) {
                ServiceManager.RemoveService<SignatureHelpSource>(_textBuffer);
                _textBuffer = null;
            }
        }
        #endregion
    }
}
