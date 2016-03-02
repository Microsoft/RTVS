﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Languages.Editor.Shell;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.Languages.Editor.Completion {
    public class CompletionCommittedEventArgs : EventArgs {
        public ICompletionSession Session { get; set; }
    }

    /// <summary>
    /// Base completion controller. Not language specific.
    /// </summary>
    public abstract class CompletionController : IIntellisenseController {
        public const string AutoShownCompletion = "AutoShownCompletion";

        public IList<ITextBuffer> SubjectBuffers { get; private set; }
        public ITextView TextView { get; private set; }

        protected ISignatureHelpBroker SignatureBroker { get; set; }
        protected IQuickInfoBroker QuickInfoBroker { get; set; }
        public ICompletionSession CompletionSession { get; protected set; }
        protected ICompletionBroker CompletionBroker { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected CompletionController(
            ITextView textView,
            IList<ITextBuffer> subjectBuffers,
            ICompletionBroker completionBroker,
            IQuickInfoBroker quickInfoBroker,
            ISignatureHelpBroker signatureBroker) {
            TextView = textView;

            SubjectBuffers = subjectBuffers;
            CompletionBroker = completionBroker;
            QuickInfoBroker = quickInfoBroker;
            SignatureBroker = signatureBroker;
        }

        public abstract void ConnectSubjectBuffer(ITextBuffer subjectBuffer);

        public abstract void DisconnectSubjectBuffer(ITextBuffer subjectBuffer);

        public virtual void Detach(ITextView textView) {
            if (textView == TextView) {
                TextView = null;
                SubjectBuffers = null;
            }
        }

        public event EventHandler<CompletionCommittedEventArgs> CompletionCommitted;
        public event EventHandler<EventArgs> CompletionDismissed;

        /// <summary>
        /// Should this key press commit a completion session?
        /// </summary>
        public virtual bool IsCommitChar(char typedCharacter) {
            return false;
        }

        /// <summary>
        /// Should this key press start a completion session?
        /// </summary>
        public virtual bool IsTriggerChar(char typedCharacter) {
            return false;
        }

        /// <summary>
        /// In some languages completion needs to know if a character is a 'closing' character. 
        /// Closing character typically tells if user finished typing certain fragment. 
        /// This might be semicolon in C# or JScript, closing angle bracked in HTML etc.
        /// Upon closing character language may choose to autoformat code, dismiss intellisense
        /// or change intellisense presenter.
        /// </summary>
        public virtual bool IsClosingChar(char typedCharacter) {
            return false;
        }

        /// <summary>
        /// Invoked if user typed a closing character. <seealso cref="IsClosingChar"/>
        /// </summary>
        protected virtual void OnPostTypeClosingChar(char typedCharacter) {
        }

        protected virtual void OnPreTypeCompletionChar(char typedCharacter) {
        }

        protected virtual void OnPostTypeCompletionChar(char typedCharacter) {
        }

        /// <summary>
        /// If completion is already showing, should this keypress start a new session?
        /// </summary>
        protected virtual bool IsRetriggerChar(ICompletionSession session, char typedCharacter) {
            return false;
        }

        /// <summary>
        /// True if character should be 'eaten' when session completes rather
        /// than passed to the core editor controller. For example, '=' symbol
        /// is ignored in HTML completion since on = HTML editor inserts =""
        /// which already contains equals.
        /// </summary>
        public virtual bool IsMuteCharacter(char typedCharacter) {
            return typedCharacter == '\t' || typedCharacter == '\n';
        }

        /// <summary>
        /// Enabled by default, derived classes can override it and check the app setting
        /// </summary>
        protected virtual bool AutoCompletionEnabled {
            get { return true; }
        }

        /// <summary>
        /// Enabled by default, derived classes can override it and check the app setting
        /// </summary>
        protected virtual bool AutoSignatureHelpEnabled {
            get { return true; }
        }

        /// <summary>
        /// Called when the user executes a command that should show the completion list
        /// </summary>
        public void OnShowMemberList(bool filterList) {
            DismissCompletionSession();
            ShowSignatureAndCompletion(autoShownSignature: true, autoShownCompletion: false);

            if (filterList) {
                FilterCompletionSession();
            }
        }

        /// <summary>
        /// Called when the user executes a command that should show the completion list,
        /// and also automatically completes the current word if possible
        /// </summary>
        public virtual void OnCompleteWord() {
            OnShowMemberList(filterList: true);
            CommitUniqueCompletionSession();
        }

        /// <summary>
        /// Called when the user executes a command that should show the signature help tooltip
        /// </summary>
        public virtual void OnShowSignatureHelp() {
            DismissSignatureSession(TextView);
            ShowSignature(autoShown: false);
        }

        /// <summary>
        /// Called when the user executes a command that should show the signature help tooltip
        /// </summary>
        public virtual void OnShowQuickInfo() {
            if (!QuickInfoBroker.IsQuickInfoActive(TextView)) {
                QuickInfoBroker.TriggerQuickInfo(TextView);
            }
        }

        /// <summary>
        /// Called when the user types a character, but before it shows up in the text view.
        /// </summary>
        /// <returns>true if the character should be ignored by the editor</returns>
        public virtual bool OnPreTypeChar(char typedCharacter) {
            bool handled = false;

            OnPreTypeCompletionChar(typedCharacter);

            if (IsCommitChar(typedCharacter)) {
                if (HasActiveCompletionSession) {
                    // IsMuteCharacter must be called first which completion session is still 
                    // active since mute character depends on the completion session type.
                    handled = IsMuteCharacter(typedCharacter);
                    CommitCompletionSession(typedCharacter);
                }

                if (HasActiveSignatureSession(TextView) && CanDismissSignatureOnCommit()) {
                    DismissSignatureSession(TextView);
                }
            }

            return handled;
        }

        protected virtual bool CanDismissSignatureOnCommit() {
            return true;
        }

        /// <summary>
        /// Called when the user types a character, after it shows up in the text view
        /// </summary>
        public virtual void OnPostTypeChar(char typedCharacter) {
            bool triggerCompletion = false;

            if (TextView == null || TextView.IsClosed) {
                return;
            }

            if (!HasActiveCompletionSession && IsTriggerChar(typedCharacter)) {
                triggerCompletion = true;
            } else if (HasActiveCompletionSession && IsRetriggerChar(CompletionSession, typedCharacter)) {
                DismissAllSessions();

                triggerCompletion = true;
            }

            if (triggerCompletion) {
                ShowSignatureAndCompletion(autoShownSignature: true, autoShownCompletion: true);
            }

            if (IsClosingChar(typedCharacter)) {
                OnPostTypeClosingChar(typedCharacter);
            }

            OnPostTypeCompletionChar(typedCharacter);
        }

        /// <summary>
        /// Is there an active completion session? (is the dropdown showing?)
        /// </summary>
        public virtual bool HasActiveCompletionSession {
            get { return CompletionSession != null && !CompletionSession.IsDismissed; }
        }

        /// <summary>
        /// Is there an active signature help session? (is the tooltip showing?)
        /// </summary>
        public static bool HasActiveSignatureSession(ITextView textView) {
            ISignatureHelpBroker broker = EditorShell.Current.ExportProvider.GetExportedValue<ISignatureHelpBroker>();
            return broker.IsSignatureHelpActive(textView);
        }

        /// <summary>
        /// Close any sessions that I am showing
        /// </summary>
        public virtual void DismissAllSessions() {
            DismissCompletionSession();
            DismissSignatureSession(TextView);
            DismissQuickInfoSession(TextView);
        }

        public static void DismissQuickInfoSession(ITextView textView) {
            IQuickInfoBroker broker = EditorShell.Current.ExportProvider.GetExportedValue<IQuickInfoBroker>();
            var sessions = broker.GetSessions(textView);
            foreach (var s in sessions) {
                s.Dismiss();
            }
        }

        /// <summary>
        /// Close the completion dropdown, don't make any changes
        /// </summary>
        public virtual void DismissCompletionSession() {
            if (HasActiveCompletionSession) {
                CompletionSession.Dismiss();
            }
        }

        /// <summary>
        /// Close the completion dropdown, commit the selected item to the text view.
        /// Used when typing a char, use the one without a typed char for CompleteWord command
        /// </summary>
        /// <returns>true if text was committed</returns>
        public virtual bool CommitCompletionSession(char typedCharacter) {
            bool committed = false;
            if (CanCommitCompletionSession(typedCharacter)) {
                UpdateInsertionText();
                CompletionSession.Commit();

                committed = true;
            } else if (HasActiveCompletionSession) {
                DismissCompletionSession();
            }

            return committed;
        }

        protected virtual bool CanCommitCompletionSession(char typedCharacter) {
            if (!HasActiveCompletionSession) {
                return false;
            }

            CompletionSet completionSet = CompletionSession.SelectedCompletionSet;
            CompletionSelectionStatus status = completionSet.SelectionStatus;

            if (status.Completion == null) {
                return false;
            }

            // Let the tab character commit any entry, whether it's really selected or not
            if (typedCharacter == '\t') {
                return true;
            }

            if (status.IsSelected) {
                return true;
            }

            return false;
        }

        /// <summary>
        ///  Close the completion dropdown, commit the selected item to the text view.
        /// </summary>
        /// <returns>true if text was committed</returns>
        public virtual bool CommitCompletionSession() {
            return CommitCompletionSession('\0');
        }

        /// <summary>
        /// Gives derived controllers a chance to update 
        /// CompletionSession.SelectedCompletionSet.SelectionStatus.Completion.InsertionText
        /// </summary>
        protected virtual void UpdateInsertionText() {
        }

        /// <summary>
        /// Allows custom completion presenters to intercept commands
        /// </summary>
        public virtual bool HandleCommand(Guid group, int id, object inputArg) {
            return false;
        }

        /// <summary>
        /// Restricts the set of completions to those that match the applicability
        /// text of the completion set, and then determines the best match.
        /// R is case-sensitive so 't' is different from 'T' (the latter is 
        /// a shortcut for 'TRUE').
        /// </summary>
        public virtual void FilterCompletionSession() {
            if (HasActiveCompletionSession) {
                CompletionSession.Filter();
            }
        }

        /// <summary>
        /// Only commit the completion selection if there is a unique choice in the list
        /// </summary>
        protected void CommitUniqueCompletionSession() {
            if (HasActiveCompletionSession) {
                CompletionSelectionStatus status = CompletionSession.SelectedCompletionSet.SelectionStatus;

                if (status.IsSelected && status.IsUnique) {
                    CommitCompletionSession();
                }
            }
        }

        /// <summary>
        /// Show the completion dropdown if it isn't showing already
        /// </summary>
        public ICompletionSession ShowCompletion(bool autoShownCompletion) {
            if ((!autoShownCompletion || AutoCompletionEnabled) &&
                !HasActiveCompletionSession &&
                TextView != null &&
                TextView.Selection.Mode != TextSelectionMode.Box &&
                CompletionBroker != null) {
                try {
                    TextView.Properties.RemoveProperty(AutoShownCompletion);
                    TextView.Properties.AddProperty(AutoShownCompletion, autoShownCompletion);

                    CompletionSession = TriggerCompletion();

                    if (CompletionSession != null) {
                        CompletionSession.Dismissed += OnCompletionSessionDismissed;
                        CompletionSession.Committed += OnCompletionSessionCommitted;
                    }
                } catch (Exception) {
                    TextView.Properties.RemoveProperty(AutoShownCompletion);
                    //throw;
                }
            }

            return CompletionSession;
        }

        public virtual ICompletionSession TriggerCompletion() {
            return CompletionBroker.TriggerCompletion(TextView);
        }

        /// <summary>
        /// Returns a topic name for showing help to the user, if there is a currently active completion session.
        /// Returns null or string.Empty when there is no known topic name.
        /// </summary>
        public string HelpTopicName {
            get {
                if (HasActiveCompletionSession) {
                    //ICustomCompletionPresenter customPresenter = CompletionSession.Presenter as ICustomCompletionPresenter;

                    //if (customPresenter != null)
                    //{
                    //    return customPresenter.HelpTopicName;
                    //}
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Shows the signature help tooltip if it isn't showing already
        /// </summary>
        public void ShowSignature(bool autoShown) {
            if ((!autoShown || AutoSignatureHelpEnabled) && !HasActiveSignatureSession(TextView) &&
                TextView.Selection.Mode != TextSelectionMode.Box && SignatureBroker != null) {
                TriggerSignatureHelp();
            }
        }

        public virtual void TriggerSignatureHelp() {
            SignatureBroker.DismissAllSessions(TextView);
            SignatureBroker.TriggerSignatureHelp(TextView);
        }

        /// <summary>
        /// Determines if current completion session is automatically invoked
        /// such as when user types a trigger character. Oppisite is
        /// when user explicitly invokes Edit > Intellisense > Show Members
        /// or similar command such as Ctrl+J or Ctrl+Space.
        /// </summary>
        protected bool IsAutoShownCompletion() {
            bool value = false;
            TextView.Properties.TryGetProperty(CompletionController.AutoShownCompletion, out value);
            return value;
        }

        /// <summary>
        /// The signature and completion MUST be shown in a specific order,
        /// so use this function to get the order correct.
        /// </summary>
        protected void ShowSignatureAndCompletion(bool autoShownSignature, bool autoShownCompletion) {
            ShowSignature(autoShownSignature);
            ShowCompletion(autoShownCompletion);
        }

        public static void DismissSignatureSession(ITextView textView) {
            if (HasActiveSignatureSession(textView)) {
                ISignatureHelpBroker broker = EditorShell.Current.ExportProvider.GetExportedValue<ISignatureHelpBroker>();
                broker.DismissAllSessions(textView);
            }
        }

        private void ClearCompletionSession() {
            if (CompletionSession != null) {
                CompletionSession.Dismissed -= OnCompletionSessionDismissed;
                CompletionSession.Committed -= OnCompletionSessionCommitted;
                CompletionSession = null;
            }
        }

        protected virtual void OnCompletionSessionDismissed(object sender, EventArgs eventArgs) {
            if (CompletionDismissed != null) {
                CompletionDismissed(this, new EventArgs());
            }

            ClearCompletionSession();
        }

        protected virtual void OnCompletionSessionCommitted(object sender, EventArgs eventArgs) {
            if (CompletionCommitted != null) {
                CompletionCommittedEventArgs committedEventArgs = new CompletionCommittedEventArgs();
                committedEventArgs.Session = CompletionSession;

                CompletionCommitted(this, committedEventArgs);
            }

            ClearCompletionSession();
        }
    }
}