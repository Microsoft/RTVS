﻿using System;
using Microsoft.Languages.Editor.BraceMatch.Definitions;
using Microsoft.Languages.Editor.Composition;
using Microsoft.Languages.Editor.Controller.Command;
using Microsoft.Languages.Editor.Controller.Constants;
using Microsoft.Languages.Editor.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.Languages.Editor.BraceMatch {

    public class GotoBraceCommand : ViewCommand {
        private IBraceMatcherProvider _braceMatcherProvider;
        protected ITextBuffer TextBuffer { get; set; }

        private static CommandId[] _commands = new CommandId[]
        {
            new CommandId(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.GOTOBRACE),
            new CommandId(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.GOTOBRACE_EXT),
        };

        public GotoBraceCommand(ITextView textView, ITextBuffer textBuffer) :
            base(textView, _commands, false) {
            var importComposer = new ContentTypeImportComposer<IBraceMatcherProvider>(EditorShell.Current.CompositionService);
            _braceMatcherProvider = importComposer.GetImport(textBuffer.ContentType.TypeName);

            TextBuffer = textBuffer;
        }

        #region ICommmand

        public override CommandStatus Status(Guid group, int id) {
            return CommandStatus.SupportedAndEnabled;
        }

        public override CommandResult Invoke(Guid group, int id, object inputArg, ref object outputArg) {
            if (group == VSConstants.VSStd2K) {
                if (id == (int)VSConstants.VSStd2KCmdID.GOTOBRACE) {
                    return GotoBrace(false);
                } else if (id == (int)VSConstants.VSStd2KCmdID.GOTOBRACE_EXT) {
                    return GotoBrace(true);
                }
            }

            return CommandResult.NotSupported;
        }

        #endregion

        private CommandResult GotoBrace(bool extendSelection) {
            if (_braceMatcherProvider == null)
                return CommandResult.NotSupported;

            var secondaryBufferPoint = TextView.Caret.Position.Point.GetPoint(TextBuffer, TextView.Caret.Position.Affinity);
            var viewOriginPoint = TextView.Caret.Position.BufferPosition;

            if (secondaryBufferPoint.HasValue) {
                var snapshot = TextBuffer.CurrentSnapshot;

                var currentPoint = secondaryBufferPoint.Value;
                int currentPosition = currentPoint.Position;
                int start;
                int end;

                var braceMatcher = _braceMatcherProvider.CreateBraceMatcher(TextView, TextBuffer);
                if (braceMatcher != null && braceMatcher.GetBracesFromPosition(snapshot, currentPosition, extendSelection, out start, out end)) {
                    int moveTo;

                    if (currentPosition == end || currentPosition == end + 1) {
                        moveTo = start;
                    } else {
                        moveTo = end + 1;
                    }

                    var bufferPosition = new SnapshotPoint(snapshot, moveTo);

                    var viewMatchPoint = TextView.BufferGraph.MapUpToBuffer(bufferPosition, PointTrackingMode.Positive, PositionAffinity.Successor, TextView.TextBuffer);

                    if (viewMatchPoint.HasValue) {
                        TextView.Caret.MoveTo(viewMatchPoint.Value);

                        if (extendSelection) {
                            var newPosition = viewMatchPoint.Value;

                            SnapshotSpan span;
                            if (viewOriginPoint > newPosition) {
                                span = new SnapshotSpan(newPosition, viewOriginPoint);
                            } else {
                                span = new SnapshotSpan(viewOriginPoint, newPosition);
                            }

                            TextView.Selection.Select(span, false);
                        }
                    }

                    TextView.Caret.EnsureVisible();

                    return CommandResult.Executed;
                }
            }

            return CommandResult.NotSupported;
        }
    }
}
