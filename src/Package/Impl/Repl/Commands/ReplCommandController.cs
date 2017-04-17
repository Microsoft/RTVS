﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.Common.Core;
using Microsoft.Common.Core.Services;
using Microsoft.Common.Core.UI.Commands;
using Microsoft.Languages.Editor.Controller;
using Microsoft.R.Components.Controller;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Editor;
using Microsoft.R.Editor.Commands;
using Microsoft.R.Editor.Completions;
using Microsoft.R.Editor.Document;
using Microsoft.R.Editor.Formatting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Expansions;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.R.Package.Repl.Commands {
    /// <summary>
    /// Main interactive window command controller
    /// </summary>
    public class ReplCommandController : ViewController {
        private readonly ExpansionsController _snippetController;
        private readonly IServiceContainer _services;

        public ReplCommandController(ITextView textView, ITextBuffer textBuffer, IServiceContainer services)
            : base(textView, textBuffer, VsAppShell.Current) {
            _services = services;
            Languages.Editor.Services.ServiceManager.AddService(this, textView, VsAppShell.Current);

            var textManager = _services.GetService<IVsTextManager2>(typeof(SVsTextManager));
            IVsExpansionManager expansionManager;
            textManager.GetExpansionManager(out expansionManager);

            // TODO: make this extensible via MEF like commands and controllers in the editor
            _snippetController = new ExpansionsController(textView, textBuffer, expansionManager, ExpansionsCache.Current);
        }

        public static ReplCommandController Attach(ITextView textView, ITextBuffer textBuffer, IServiceContainer serviceContainer) {
            var controller = FromTextView(textView);
            return controller ?? new ReplCommandController(textView, textBuffer, serviceContainer);
        }

        public static new ReplCommandController FromTextView(ITextView textView)
            => Languages.Editor.Services.ServiceManager.GetService<ReplCommandController>(textView);

        public override void BuildCommandSet() {
            var factory = new ReplCommandFactory();
            var commands = factory.GetCommands(TextView, TextBuffer);
            AddCommandSet(commands);
        }

        public override CommandStatus Status(Guid group, int id) {
            if ((NonRoutedStatus(group, id, null) & CommandStatus.SupportedAndEnabled) == CommandStatus.SupportedAndEnabled
                && !IsCompletionCommand(group, id)) {
                return CommandStatus.SupportedAndEnabled;
            }

            var status = _snippetController.Status(group, id);
            if (status != CommandStatus.NotSupported) {
                return status;
            }

            return base.Status(group, id);
        }

        public override CommandResult Invoke(Guid group, int id, object inputArg, ref object outputArg) {
            if (group == VSConstants.VSStd2K) {
                RCompletionController controller = RCompletionController.FromTextView(TextView);
                if (controller != null) {
                    if (id == (int)VSConstants.VSStd2KCmdID.RETURN) {
                        return HandleEnter(controller);
                    } else if (id == (int)VSConstants.VSStd2KCmdID.CANCEL) {
                        HandleCancel(controller);
                        // Allow VS to continue processing cancel
                    }
                }
            } else if (group == VSConstants.GUID_VSStandardCommandSet97) {
                if (id == (int)VSConstants.VSStd97CmdID.F1Help) {
                    RCompletionController controller = RCompletionController.FromTextView(TextView);
                    if (controller != null) {
                        // Translate to R help
                        HandleF1Help(controller);
                        return CommandResult.Executed;
                    }
                }
            }

            var status = _snippetController.Status(group, id);
            if (status != CommandStatus.NotSupported) {
                var result = _snippetController.Invoke(group, id, inputArg, ref outputArg);
                if (result.Status != CommandStatus.NotSupported) {
                    return result;
                }
            }

            return base.Invoke(group, id, inputArg, ref outputArg);
        }

        private CommandResult HandleEnter(RCompletionController controller) {
            // If completion is up, commit it
            if (controller.HasActiveCompletionSession) {
                // Check for exact match. If applicable span is 'x' and completion is 'x'
                // then we don't complete and rather execute. If span is 'x' while
                // current completion entry is 'X11' then we complete depending on
                // the 'complete on enter' setting.
                try {
                    ICompletionSession session = controller.CompletionSession;
                    CompletionSet set = session.SelectedCompletionSet;
                    ITrackingSpan span = set.ApplicableTo;
                    ITextSnapshot snapshot = span.TextBuffer.CurrentSnapshot;
                    string spanText = snapshot.GetText(span.GetSpan(snapshot));
                    if (spanText != set.SelectionStatus.Completion.InsertionText) {
                        // If selection is does not match typed text,
                        // control completion depending on the editor setting.
                        if (set.SelectionStatus.IsSelected && _services.GetService<IREditorSettings>().CommitOnEnter) {
                            controller.CommitCompletionSession();
                            controller.DismissAllSessions();
                            return CommandResult.Executed;
                        }
                    }
                } catch (Exception) { }
            }

            controller.DismissAllSessions();
            var broker = _services.GetService<ICompletionBroker>();
            broker.DismissAllSessions(TextView);

            var interactiveWorkflowProvider = _services.GetService<IRInteractiveWorkflowProvider>();
            interactiveWorkflowProvider.GetOrCreate().Operations.ExecuteCurrentExpression(TextView, FormatReplDocument);
            return CommandResult.Executed;
        }

        private static void FormatReplDocument(ITextView textView, ITextBuffer textBuffer, int position) {
            var document = REditorDocument.TryFromTextBuffer(textBuffer);
            if (document != null) {
                var tree = document.EditorTree;
                tree.EnsureTreeReady();
                FormatOperations.FormatCurrentStatement(textView, textBuffer, VsAppShell.Current);
            }
        }

        private void HandleCancel(RCompletionController controller) {
            if (!controller.HasActiveCompletionSession && !controller.HasActiveSignatureSession(TextView)) {
                Workflow.Operations.CancelAsync().DoNotWait();
                // Post interrupt command which knows if it can interrupt R or not
                Workflow.Shell.PostCommand(RGuidList.RCmdSetGuid, RPackageCommandId.icmdInterruptR);
            }
        }

        private void HandleF1Help(RCompletionController controller) {
            Workflow.Shell.PostCommand(RGuidList.RCmdSetGuid, RPackageCommandId.icmdHelpOnCurrent);
        }

        /// <summary>
        /// Determines if command is one of the completion commands
        /// </summary>
        private bool IsCompletionCommand(Guid group, int id) {
            ICommand cmd = Find(group, id);
            return cmd is RCompletionCommandHandler;
        }

        /// <summary>
        /// Disposes main controller and removes it from service manager.
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (TextView != null) {
                Languages.Editor.Services.ServiceManager.RemoveService<ReplCommandController>(TextView);
            }
            base.Dispose(disposing);
        }

        private IRInteractiveWorkflow Workflow {
            get {
                var interactiveWorkflowProvider = _services.GetService<IRInteractiveWorkflowProvider>();
                return interactiveWorkflowProvider.GetOrCreate();
            }
        }
    }
}