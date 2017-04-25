﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Common.Core.Shell;
using Microsoft.Languages.Core.Text;
using Microsoft.Languages.Editor;
using Microsoft.Languages.Editor.Completions;
using Microsoft.Languages.Editor.Signatures;
using Microsoft.Languages.Editor.Text;
using Microsoft.Languages.Editor.Utility;
using Microsoft.R.Core.AST;
using Microsoft.R.Editor.Document;
using Microsoft.R.Editor.Functions;

namespace Microsoft.R.Editor.Signatures {
    public partial class FunctionSignature : IFunctionSignature {
        // http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.language.intellisense.isignature.aspx

        private IEditorCompletionSession _session;
        private IEditorView _view;
        private IEditorBuffer _editorBuffer;

        private ISignatureParameter _currentParameter;
        private ITrackingTextRange _applicableToRange;
        private int _initialPosition;
        private readonly ISignatureInfo _signatureInfo;
        private readonly ICoreShell _shell;
        private readonly IViewCompletionBroker _completionBroker;

        public string FunctionName { get; private set; }

        public static IFunctionSignature Create(IRCompletionContext context, IFunctionInfo functionInfo, ISignatureInfo signatureInfo, ITrackingTextRange applicableSpan) {
            var sig = new FunctionSignature(context.Session, context.EditorBuffer, functionInfo.Name, string.Empty, signatureInfo, _sh);
            var paramList = new List<ISignatureParameter>();

            // Locus points in the pretty printed signature (the one displayed in the tooltip)
            var locusPoints = new List<int>();
            string signatureString = signatureInfo.GetSignatureString(functionInfo.Name, locusPoints);
            sig.Content = signatureString;
            sig.ApplicableToSpan = applicableSpan;

            sig.Documentation = functionInfo.Description?.Wrap(Math.Min(SignatureInfo.MaxSignatureLength, sig.Content.Length));

            Debug.Assert(locusPoints.Count == signatureInfo.Arguments.Count + 1);
            for (var i = 0; i < signatureInfo.Arguments.Count; i++) {
                var p = signatureInfo.Arguments[i];
                if (p != null) {
                    var locusStart = locusPoints[i];
                    var locusLength = locusPoints[i + 1] - locusStart;

                    Debug.Assert(locusLength >= 0);
                    var locus = new TextRange(locusStart, locusLength);

                    // VS may end showing very long tooltip so we need to keep 
                    // description reasonably short: typically about
                    // same length as the function signature.
                    paramList.Add(
                        new FunctionParameter(
                            p.Description.Wrap(
                                Math.Min(SignatureInfo.MaxSignatureLength, sig.Content.Length)),
                                locus, locus, p.Name, sig));
                }
            }

            sig.Parameters = new ReadOnlyCollection<ISignatureParameter>(paramList);
            sig.ComputeCurrentParameter(context.AstRoot, context.Position);

            return sig;
        }

        private FunctionSignature(IEditorCompletionSession session, IEditorBuffer textBuffer, string functionName, string documentation, ISignatureInfo signatureInfo, ICoreShell shell) {
            FunctionName = functionName;
            _signatureInfo = signatureInfo;
            _shell = shell;

            Documentation = documentation;
            Parameters = null;

            _session = session;
            _session.Dismissed += OnSessionDismissed;

            _view = session.View;
            _view.Caret.PositionChanged += OnCaretPositionChanged;

            _completionBroker = _view.GetService<IViewCompletionBroker>();
            Debug.Assert(_completionBroker != null);

            _editorBuffer = textBuffer;
            _editorBuffer.Changed += OnTextBufferChanged;
        }

        internal int ComputeCurrentParameter(IEditorBufferSnapshot snapshot, AstRoot ast, int position) {
            var parameterInfo = FunctionParameter.FromEditorBuffer(ast, snapshot, position);
            int index = 0;

            if (parameterInfo != null) {
                index = parameterInfo.ParameterIndex;
                if (parameterInfo.NamedParameter) {
                    // A function f <- function(foo, bar) is said to have formal parameters "foo" and "bar", 
                    // and the call f(foo=3, ba=13) is said to have (actual) arguments "foo" and "ba".
                    // R first matches all arguments that have exactly the same name as a formal parameter. 
                    // Two identical argument names cause an error. Then, R matches any argument names that
                    // partially matches a(yet unmatched) formal parameter. But if two argument names partially 
                    // match the same formal parameter, that also causes an error. Also, it only matches 
                    // formal parameters before ... So formal parameters after ... must be specified using 
                    // their full names. Then the unnamed arguments are matched in positional order to 
                    // the remaining formal arguments.

                    int argumentIndexInSignature = _signatureInfo.GetArgumentIndex(parameterInfo.ParameterName, _shell.GetService<IREditorSettings>().PartialArgumentNameMatch);
                    if (argumentIndexInSignature >= 0) {
                        index = argumentIndexInSignature;
                    }
                }
            }
            return index;
        }

        #region IFunctionSignature
        /// <summary>
        /// Content of the signature, including all the characters to be displayed.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Documentation associated with this signature.
        /// </summary>
        public string Documentation { get; set; }

        /// <summary>
        /// Span of text in the buffer to which this signature help is applicable.
        /// </summary>
        public ITrackingTextRange ApplicableToSpan {
            get { return _applicableToRange; }
            set {
                if (_editorBuffer != null) {
                    _initialPosition = value.GetStartPoint(_editorBuffer.CurrentSnapshot);
                }
                _applicableToRange = value;
            }
        }

        /// <summary>
        /// List of parameters that this signature knows about.
        /// </summary>
        public ReadOnlyCollection<ISignatureParameter> Parameters { get; private set; }

        /// <summary>
        /// Content of the signature, pretty-printed into a form suitable for display on-screen.
        /// </summary>
        public string PrettyPrintedContent { get; set; }

        /// <summary>
        /// Occurs when the currently-selected parameter changes.
        /// </summary>
        public event EventHandler<SignatureParameterChangedEventArgs> CurrentParameterChanged;

        /// <summary>
        /// Current parameter for this signature.
        /// </summary>
        public ISignatureParameter CurrentParameter {
            get { return _currentParameter; }
            internal set {
                if (_currentParameter != value) {
                    var prevCurrentParameter = _currentParameter;
                    _currentParameter = value;
                    CurrentParameterChanged?.Invoke(this, new SignatureParameterChangedEventArgs(prevCurrentParameter, _currentParameter));
                }
            }
        }
        #endregion

        #region Event handlers
        protected virtual void OnTextBufferChanged(object sender, TextChangeEventArgs e) {
            if (_session != null) {
                int position = e.Start + e.NewLength;
                if (position < _initialPosition) {
                    _completionBroker.DismissSignatureSession();
                } else {
                    UpdateCurrentParameter();
                }
            }
        }

        private void OnCaretPositionChanged(object sender, ViewCaretPositionChangedEventArgs e) {
            if (_view != null) {
                if (IsSameSignatureContext()) {
                    UpdateCurrentParameter();
                } else {
                    _completionBroker.DismissSignatureSession();
                    _completionBroker.TriggerSignatureSession();
                }
            }
            else {
                e.View.Caret.PositionChanged -= OnCaretPositionChanged;
            }
        }
        #endregion

        private void UpdateCurrentParameter() {
            if (_editorBuffer != null && _view != null) {
                var document = _editorBuffer.GetEditorDocument<IREditorDocument>();
                if (document != null) {
                    var p = _view.GetCaretPosition(_editorBuffer);
                     if (p != null) {
                        document.EditorTree.InvokeWhenReady((o) => {
                            if (_view != null) {
                                // Session is still active
                                p = _view.GetCaretPosition(_editorBuffer);
                                if (p != null) {
                                    ComputeCurrentParameter(document.EditorTree.AstRoot, p.Position);
                                }
                            }
                        }, null, GetType());
                    } else {
                        _completionBroker.DismissSignatureSession();
                    }
                }
            }
        }

        public virtual void ComputeCurrentParameter(AstRoot ast, int position) {
            if (Parameters == null || Parameters.Count == 0 || _editorBuffer == null) {
                CurrentParameter = null;
                return;
            }

            var parameterIndex = ComputeCurrentParameter(_editorBuffer.CurrentSnapshot, ast, position);
            if (parameterIndex < Parameters.Count) {
                CurrentParameter = Parameters[parameterIndex];
            } else {
                //too many commas, so use the last parameter as the current one.
                CurrentParameter = Parameters[Parameters.Count - 1];
            }
        }

        protected virtual void OnSessionDismissed(object sender, EventArgs e) {
            if (_session != null) {
                _session.Dismissed -= OnSessionDismissed;
                _session = null;
            }

            if (_editorBuffer != null) {
                _editorBuffer.Changed -= OnTextBufferChanged;
                _editorBuffer = null;
            }

            if (_view != null) {
                _view.Caret.PositionChanged -= OnCaretPositionChanged;
                _view = null;
            }
        }

        /// <summary>
        /// Determines if current caret position is in the same function
        /// argument list as before or is it a different one and signature 
        /// help session should be dismissed and re-triggered. This is helpful
        /// when user types nested function calls such as 'a(b(c(...), d(...)))'
        /// </summary>
        private bool IsSameSignatureContext() {
            var sessions = _completionBroker.GetSessions(textView);
            Debug.Assert(sessions.Count < 2);
            if (sessions.Count == 1) {
                IFunctionInfo sessionFunctionInfo = null;
                sessions[0].Properties.TryGetProperty("functionInfo", out sessionFunctionInfo);

                if (sessionFunctionInfo != null) {
                    try {
                        var document = _editorBuffer.GetEditorDocument<IREditorDocument>();
                        document.EditorTree.EnsureTreeReady();

                        var parametersInfo = FunctionParameter.FromEditorBuffer(
                            document.EditorTree.AstRoot, _editorBuffer.CurrentSnapshot, _view.Caret.Position.Position);

                        return parametersInfo != null && parametersInfo.FunctionName == sessionFunctionInfo.Name;
                    } catch (Exception) { }
                }
            }

            return false;
        }
    }
}
