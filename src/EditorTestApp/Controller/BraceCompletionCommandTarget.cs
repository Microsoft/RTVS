﻿using System;
using Microsoft.Languages.Editor.Completion;
using Microsoft.Languages.Editor.Controller;
using Microsoft.Languages.Editor.Controller.Constants;
using Microsoft.Languages.Editor.Shell;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.Languages.Editor.Application.Controller {
    /// <summary>
    /// Lifted off core IDE editor
    /// </summary>
    internal sealed class BraceCompletionCommandTarget : ICommandTarget {
        private IBraceCompletionManager _manager;
        private ITextView _textView;

        public BraceCompletionCommandTarget(ITextView textView) {
            _textView = textView;
        }

        #region ICommandTarget
        public CommandResult Invoke(Guid group, int id, object inputArg, ref object outputArg) {

            // only run for VSStd2K commands and if brace completion is enabled
            if (group == VSConstants.VSStd2K) {
                if (id == (uint)VSConstants.VSStd2KCmdID.TYPECHAR) {
                    char typedChar = TypingCommandHandler.GetTypedChar(group, id, inputArg);

                    // handle closing braces if there is an active session
                    if ((Manager.HasActiveSessions && Manager.ClosingBraces.IndexOf(typedChar) > -1)
                        || Manager.OpeningBraces.IndexOf(typedChar) > -1) {
                        bool handledCommand = false;
                        Manager.PreTypeChar(typedChar, out handledCommand);
                        if (handledCommand) {
                            return CommandResult.Executed;
                        }
                    }
                }
                // tab, delete, backspace, and return only need to be handled if there is an active session
                // tab and return should be skipped if completion is currently active
                else if (Manager.HasActiveSessions) {
                    switch (id) {
                        case (int)VSConstants.VSStd2KCmdID.RETURN:
                            {
                                if (!IsCompletionActive) {
                                    bool handledCommand = false;
                                    Manager.PreReturn(out handledCommand);
                                    if (handledCommand) {
                                        return CommandResult.Executed;
                                    }
                                }
                                break;
                            }
                        case (int)VSConstants.VSStd2KCmdID.TAB:
                            {
                                if (!IsCompletionActive) {
                                    bool handledCommand = false;

                                    Manager.PreTab(out handledCommand);
                                    if (handledCommand) {
                                        return CommandResult.Executed;
                                    }
                                }
                                break;
                            }
                        case (int)VSConstants.VSStd2KCmdID.BACKSPACE:
                            {
                                bool handledCommand = false;
                                Manager.PreBackspace(out handledCommand);
                                if (handledCommand) {
                                    return CommandResult.Executed;
                                }
                                break;
                            }
                        case (int)VSConstants.VSStd2KCmdID.DELETE:
                            {
                                bool handledCommand = false;
                                Manager.PreDelete(out handledCommand);
                                if (handledCommand) {
                                    return CommandResult.Executed;
                                }
                                break;
                            }
                    }
                }
            }
            return CommandResult.NotSupported;
        }

        public void PostProcessInvoke(CommandResult result, Guid group, int id, object inputArg, ref object outputArg) {

            // only run for VSStd2K commands and if brace completion is enabled
            if (group == VSConstants.VSStd2K) {
                if (id == (int)VSConstants.VSStd2KCmdID.TYPECHAR) {
                    char typedChar = TypingCommandHandler.GetTypedChar(group, id, inputArg);

                    // handle closing braces if there is an active session
                    if ((Manager.HasActiveSessions && Manager.ClosingBraces.IndexOf(typedChar) > -1)
                        || Manager.OpeningBraces.IndexOf(typedChar) > -1) {
                        Manager.PostTypeChar(typedChar);
                    }
                }
                // tab, delete, backspace, and return only need to be handled if there is an active session
                // tab and return should be skipped if completion is currently active
                else if (Manager.HasActiveSessions) {
                    switch (id) {
                        case (int)VSConstants.VSStd2KCmdID.RETURN:
                            if (!IsCompletionActive) {
                                Manager.PostReturn();
                            }
                            break;
                        case (int)VSConstants.VSStd2KCmdID.TAB:
                            if (!IsCompletionActive) {
                                Manager.PostTab();
                            }
                            break;
                        case (int)VSConstants.VSStd2KCmdID.BACKSPACE:
                            Manager.PostBackspace();
                            break;
                        case (int)VSConstants.VSStd2KCmdID.DELETE:
                            Manager.PostDelete();
                            break;
                    }
                }
            }
        }

        public CommandStatus Status(Guid group, int id) {
            return CommandStatus.SupportedAndEnabled;
        }
        #endregion

        #region Private Helpers
        private IBraceCompletionManager Manager {
            get {
                if (_manager == null
                    && !_textView.Properties.TryGetProperty("BraceCompletionManager", out _manager)) {
                    _manager = null;
                }

                return _manager;
            }
        }

        private bool IsCompletionActive {
            get {
                return CompletionBroker.IsCompletionActive(_textView);
            }
        }

        private ICompletionBroker _completionBroker;
        private ICompletionBroker CompletionBroker {
            get {
                if (_completionBroker == null) {
                    _completionBroker = EditorShell.Current.ExportProvider.GetExportedValue<ICompletionBroker>();
                }

                return _completionBroker;
            }
        }
        #endregion
    }
}
