﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Languages.Editor.Completion;
using Microsoft.Languages.Editor.Services;
using Microsoft.R.Core.AST;
using Microsoft.R.Core.AST.Operators;
using Microsoft.R.Editor.Completion.Definitions;
using Microsoft.R.Editor.Document;
using Microsoft.R.Editor.Document.Definitions;
using Microsoft.R.Editor.Settings;
using Microsoft.R.Editor.Signatures;
using Microsoft.R.Support.Help.Definitions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.R.Editor.Completion {
    using Completion = Microsoft.VisualStudio.Language.Intellisense.Completion;

    /// <summary>
    /// R-specific completion controller. Initiates, commits or dismisses
    /// completion, signature and parameter help sessions depending 
    /// on what was typed and the current editor context.
    /// </summary>
    public sealed class RCompletionController : CompletionController {
        private ITextBuffer _textBuffer;
        private char _commitChar = '\0';

        private RCompletionController(
            ITextView textView,
            IList<ITextBuffer> subjectBuffers,
            ICompletionBroker completionBroker,
            IQuickInfoBroker quickInfoBroker,
            ISignatureHelpBroker signatureBroker)
            : base(textView, subjectBuffers, completionBroker, quickInfoBroker, signatureBroker) {
            _textBuffer = subjectBuffers[0];

            ServiceManager.AdviseServiceAdded<REditorDocument>(_textBuffer, OnDocumentReady);
        }

        /// <summary>
        /// Called when text buffer becomes visible in the text view.
        /// The buffer may not be a top-level buffer in the graph and
        /// may be projected into view.
        /// </summary>
        public override void ConnectSubjectBuffer(ITextBuffer subjectBuffer) {
            if (_textBuffer == null) {
                _textBuffer = subjectBuffer;
            }

            if (_textBuffer == subjectBuffer) {
                ServiceManager.AdviseServiceAdded<REditorDocument>(_textBuffer, OnDocumentReady);
            }
        }

        /// <summary>
        /// Called when text buffer becomes invisible in the text view.
        /// The buffer may not be a top-level buffer in the graph and
        /// may be projected into view. Typically called when document
        /// is closed or buffer is removed from the view buffer graph.
        /// </summary>
        public override void DisconnectSubjectBuffer(ITextBuffer subjectBuffer) {
            if (_textBuffer == subjectBuffer) {
                RCompletionController existingController = ServiceManager.GetService<RCompletionController>(TextView);

                // This can get called multiple times without a ConnectSubjectBuffer call between
                if (existingController != null) {
                    Debug.Assert(existingController == this);
                    if (existingController == this) {
                        ServiceManager.RemoveService<RCompletionController>(TextView);
                    }
                }

                _textBuffer = null;
            }
        }

        private void OnDocumentReady(REditorDocument document) {
            // This object isn't released on content type changes, 
            // instead using the (Dis)ConnectSubjectBuffer
            // methods to control it's lifetime.
            ServiceManager.AddService<RCompletionController>(this, TextView);
        }

        public static RCompletionController Create(
            ITextView textView,
            IList<ITextBuffer> subjectBuffers,
            ICompletionBroker completionBroker,
            IQuickInfoBroker quickInfoBroker,
            ISignatureHelpBroker signatureBroker) {
            RCompletionController completionController = null;

            completionController = ServiceManager.GetService<RCompletionController>(textView);
            if (completionController == null) {
                completionController = new RCompletionController(textView, subjectBuffers, completionBroker, quickInfoBroker, signatureBroker);
            }

            return completionController;
        }

        public static RCompletionController FromTextView(ITextView textView) {
            return ServiceManager.GetService<RCompletionController>(textView);
        }

        protected override bool AutoCompletionEnabled {
            get { return REditorSettings.CompletionEnabled; }
        }

        protected override bool AutoSignatureHelpEnabled {
            get { return REditorSettings.SignatureHelpEnabled; }
        }

        /// <summary>
        /// Should this key commit a completion session?
        /// </summary>
        public override bool IsCommitChar(char typedChar) {
            if (HasActiveCompletionSession && typedChar != 0) {
                // only ( completes keywords
                CompletionSet completionSet = CompletionSession.SelectedCompletionSet;
                string completionText = completionSet.SelectionStatus.Completion.InsertionText;

                if (completionText == "else" || completionText == "repeat") {
                    // { after 'else' or 'repeat' completes keyword
                    if (typedChar == '{')
                        return true;

                    // Space completes if selection is unique
                    if (char.IsWhiteSpace(typedChar) && completionSet.SelectionStatus.IsUnique)
                        return true;

                    return false;
                }

                // ';' completes after next or break keyword
                if (completionText == "break" || completionText == "next") {
                    if (typedChar == ';')
                        return true;

                    // Space completes if selection is unique
                    if (char.IsWhiteSpace(typedChar) && completionSet.SelectionStatus.IsUnique)
                        return true;
                }

                // Handle ( after keyword that is usually followed by expression in braces
                // such as for(), if(), library(), ...
                if (completionText == "if" || completionText == "for" || completionText == "while" ||
                    completionText == "return" || completionText == "library" || completionText == "require") {
                    if (typedChar == '(')
                        return true;

                    if (char.IsWhiteSpace(typedChar) && completionSet.SelectionStatus.IsUnique)
                        return true;

                    return false;
                }

                switch (typedChar) {
                    case '<':
                    case '>':
                    case '+':
                    case '-':
                    case '*':
                    case '^':
                    case '=':
                    case '%':
                    case '|':
                    case '&':
                    case '!':
                    case ':':
                    case '@':
                    case '$':
                    case '(':
                    case '[':
                    case '{':
                    case ')':
                    case ']':
                    case '}':
                    case ';':
                        return completionSet.SelectionStatus.IsUnique;
                }

                if (typedChar == ' ' && !REditorSettings.CommitOnSpace)
                    return false;

                if (char.IsWhiteSpace(typedChar)) {
                    IREditorDocument document = REditorDocument.TryFromTextBuffer(TextView.TextBuffer);
                    if (document != null && document.IsTransient) {
                        return typedChar == '\t';
                    }

                    if (typedChar == '\n' || typedChar == '\r') {
                        if (REditorSettings.CommitOnEnter)
                            return true;

                        return !IsAutoShownCompletion();
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Called before character type is passed down to the core editor
        /// along the controll chain. Gives language-specific controller
        /// a chance to initiate different action and potentially 'eat'
        /// the character. For example, in R typing 'abc[TAB] should bring
        /// up intellisense list rather than actually insert the tab character.
        /// </summary>
        /// <returns>
        /// True if character was handled and should not be 
        /// passed down to core editor or false otherwise.
        /// </returns>
        public override bool OnPreTypeChar(char typedCharacter) {
            if (typedCharacter == '\t' && !HasActiveCompletionSession) {
                // if previous character is not whitespace, bring it on
                SnapshotPoint? position = REditorDocument.MapCaretPositionFromView(TextView);
                if (position.HasValue) {
                    int pos = position.Value;
                    if (pos > 0 && pos <= position.Value.Snapshot.Length) {
                        if (!char.IsWhiteSpace(position.Value.Snapshot[pos - 1])) {
                            ShowCompletion(autoShownCompletion: false);
                            return true;
                        }
                    }
                }
            }

            return base.OnPreTypeChar(typedCharacter);
        }

        /// <summary>
        /// Should this key press trigger a completion session?
        /// </summary>
        public override bool IsTriggerChar(char typedCharacter) {
            if (!HasActiveCompletionSession) {
                switch (typedCharacter) {
                    case '$':
                        //case '@':
                        return true;

                    case ':':
                        return RCompletionContext.IsCaretInNamespace(TextView);

                    case '(':
                        return RCompletionContext.IsCaretInLibraryStatement(TextView);

                    default:
                        if (REditorSettings.ShowCompletionOnFirstChar) {
                            return Char.IsLetter(typedCharacter) || typedCharacter == '.';
                        }
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if character is a re-trigger one. Re-trigger
        /// means 'commit and trigger again' such as when user
        /// hits $ that commits current session for the class/object
        /// and trigger it again for object members.
        /// </summary>
        protected override bool IsRetriggerChar(ICompletionSession session, char typedCharacter) {
            switch (typedCharacter) {
                case '@':
                case '$':
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Called after character is typed. Gives language-specific completion
        /// controller has a chance to dismiss or initiate completion and paramenter
        /// help sessions depending on the current context.
        /// </summary>
        public override void OnPostTypeChar(char typedCharacter) {
            if (typedCharacter == '(' || typedCharacter == ',') {
                if (!IsSameSignatureContext()) {
                    DismissAllSessions();
                    SignatureBroker.TriggerSignatureHelp(TextView);
                }
            } else if (HasActiveSignatureSession(TextView) && typedCharacter == ')') {
                DismissAllSessions();

                AstRoot ast = REditorDocument.FromTextBuffer(TextView.TextBuffer).EditorTree.AstRoot;
                FunctionCall f = ast.GetNodeOfTypeFromPosition<FunctionCall>(TextView.Caret.Position.BufferPosition);
                if (f != null) {
                    SignatureBroker.TriggerSignatureHelp(TextView);
                }
            } else if (HasActiveSignatureSession(TextView) && typedCharacter == '\n') {
                DismissAllSessions();
                SignatureBroker.TriggerSignatureHelp(TextView);
            } else if (this.HasActiveCompletionSession) {
                if (typedCharacter == ',') {
                    CompletionSession.Dismiss();
                } else if (typedCharacter == '\'' || typedCharacter == '\"') {
                    base.OnPostTypeChar(typedCharacter);

                    DismissAllSessions();
                    ShowCompletion(autoShownCompletion: true);
                    return;
                }
            }

            base.OnPostTypeChar(typedCharacter);
        }

        /// <summary>
        /// Determines if current caret position is in the same function
        /// argument list as before or is it a different one and signature 
        /// help session should be dismissed and re-triggered. This is helpful
        /// when user types nested function calls such as 'a(b(c(...), d(...)))'
        /// </summary>
        private bool IsSameSignatureContext() {
            var sessions = SignatureBroker.GetSessions(TextView);
            Debug.Assert(sessions.Count < 2);
            if (sessions.Count == 1) {
                IFunctionInfo sessionFunctionInfo = null;
                sessions[0].Properties.TryGetProperty<IFunctionInfo>("functionInfo", out sessionFunctionInfo);

                if (sessionFunctionInfo != null) {
                    try {
                        IREditorDocument document = REditorDocument.FromTextBuffer(TextView.TextBuffer);
                        document.EditorTree.EnsureTreeReady();

                        ParameterInfo parametersInfo = SignatureHelp.GetParametersInfoFromBuffer(
                            document.EditorTree.AstRoot, _textBuffer.CurrentSnapshot,
                            TextView.Caret.Position.BufferPosition);

                        return parametersInfo != null && parametersInfo.FunctionName == sessionFunctionInfo.Name;
                    } catch (Exception) { }
                }
            }

            return false;
        }

        public override bool CommitCompletionSession(char typedCharacter) {
            try {
                _commitChar = typedCharacter;
                return base.CommitCompletionSession(typedCharacter);
            } finally {
                _commitChar = '\0';
            }
        }

        /// <summary>
        /// Updates insertion text so it excludes final commit character 
        /// </summary>
        protected override void UpdateInsertionText() {
            if (CompletionSession != null && !IsMuteCharacter(_commitChar)) {
                Completion curCompletion = CompletionSession.SelectedCompletionSet.SelectionStatus.Completion;
                string insertionText = curCompletion.InsertionText;

                if (insertionText[insertionText.Length - 1] == _commitChar) {
                    curCompletion.InsertionText = insertionText.Substring(0, insertionText.Length - 1);
                }
            }
        }
   }
}