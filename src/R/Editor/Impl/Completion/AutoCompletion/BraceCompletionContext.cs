﻿using Microsoft.Languages.Core.Text;
using Microsoft.Languages.Editor.Shell;
using Microsoft.Languages.Editor.Undo;
using Microsoft.R.Editor.Document;
using Microsoft.R.Editor.Document.Definitions;
using Microsoft.R.Editor.Formatting;
using Microsoft.R.Editor.Tree.Definitions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;

namespace Microsoft.R.Editor.Completion.AutoCompletion {

    /// <summary>
    /// Represents a simple context used to extend the default brace 
    /// completion behaviors to include language-specific behaviors
    /// such as parsing and formatting.
    /// </summary>
    internal sealed class BraceCompletionContext : IBraceCompletionContext {
        /// <summary>
        /// Called before the session is added to the stack.
        /// </summary>
        /// <remarks>
        /// If additional formatting is required for the opening or 
        /// closing brace it should be done here.
        /// </remarks>
        /// <param name="session">Default brace completion session</param>
        public void Start(IBraceCompletionSession session) {
            if (session.OpeningBrace == '{') {
                AutoFormat.IgnoreOnce = false;
                IEditorTree tree = GetEditorTree(session);
                if (tree != null) {
                    AutoFormat.FormatLine(session.TextView, session.SubjectBuffer, tree.AstRoot, 0);
                }
            }
        }

        /// <summary>
        /// Called after the session has been removed from the stack.
        /// </summary>
        /// <param name="session">Default brace completion session</param>
        public void Finish(IBraceCompletionSession session) {
            if (session.OpeningBrace == '{') {
                AutoFormat.IgnoreOnce = false;
                IEditorTree tree = GetEditorTree(session);
                if (tree != null) {
                    AutoFormat.FormatCurrentScope(session.TextView, session.SubjectBuffer, tree.AstRoot, indentCaret: false);
                }
            }
        }

        /// <summary>
        /// Called by the editor when return is pressed while both 
        /// braces are on the same line and no typing has occurred 
        /// in the session.
        /// </summary>
        /// <remarks>
        /// Called after the newline has been inserted into the buffer.
        /// Formatting for scenarios where the closing brace needs to be 
        /// moved down an additional line past the caret should be done here.
        /// </remarks>
        /// <param name="session">Default brace completion session</param>
        public void OnReturn(IBraceCompletionSession session) {
            if (session.OpeningBrace == '{') {
                IEditorTree tree = GetEditorTree(session);
                if (tree != null) {
                    AutoFormat.FormatCurrentScope(session.TextView, session.SubjectBuffer, tree.AstRoot, indentCaret: true);
                }
                AutoFormat.IgnoreOnce = true;
            }
        }

        /// <summary>
        /// Called by the editor when the closing brace character has been typed.
        /// </summary>
        /// <remarks>
        /// The closing brace character will not be inserted into the buffer 
        /// until after this returns. Does not occur if there is any non-whitespace 
        /// between the caret and the closing brace. Language-specific decisions 
        /// may be made here to take into account scenarios such as an escaped 
        /// closing char.
        /// </remarks>
        /// <param name="session">Default brace completion session</param>
        /// <returns>Returns true if the context is a valid overtype scenario.</returns>
        public bool AllowOverType(IBraceCompletionSession session) {
            return true;
        }

        private IEditorTree GetEditorTree(IBraceCompletionSession session) {
            IREditorDocument document = REditorDocument.TryFromTextBuffer(session.SubjectBuffer);
            if (document != null) {
                IEditorTree tree = document.EditorTree;
                tree.EnsureTreeReady();
                return tree;
            }

            return null;
        }
    }
}
