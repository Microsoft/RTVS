﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.R.Core.Tokens;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.R.Editor.Completion {
    using Completion = VisualStudio.Language.Intellisense.Completion;
    using CompletionList = Microsoft.Languages.Editor.Completion.CompletionList;

    internal sealed class RCompletionSet : CompletionSet {
        private ITextBuffer _textBuffer;
        private CompletionList _completions;
        private FilteredObservableCollection<Completion> _filteredCompletions;

        public RCompletionSet(ITextBuffer textBuffer, ITrackingSpan trackingSpan, List<RCompletion> completions) :
            base("R Completion", "R Completion", trackingSpan, Enumerable.Empty<RCompletion>(), Enumerable.Empty<RCompletion>()) {
            _textBuffer = textBuffer;
            _completions = OrderList(completions);
            _filteredCompletions = new FilteredObservableCollection<Completion>(_completions);
        }

        public override IList<Completion> Completions {
            get { return _filteredCompletions; }
        }

        public override void Filter() {
            UpdateVisibility();
            _filteredCompletions.Filter(x => ((RCompletion)x).IsVisible);
        }

        private void UpdateVisibility() {
            Dictionary<int, List<Completion>> matches = new Dictionary<int, List<Completion>>();
            int maxKey = 0;

            string typedText = GetTypedText();
            if (typedText.Length == 0) {
                return;
            }

            foreach (RCompletion c in _completions) {
                int key = Match(typedText, c.DisplayText);
                if (key > 0) {
                    List<Completion> list;
                    if (!matches.TryGetValue(key, out list)) {
                        list = new List<Completion>();
                        matches[key] = list;
                        maxKey = Math.Max(maxKey, key);
                    }
                    list.Add(c);
                }
            }

            if (maxKey > 0) {
                _completions.ForEach(x => ((RCompletion)x).IsVisible = false);
                matches[maxKey].ForEach(x => ((RCompletion)x).IsVisible = true);
            }
        }

        private int Match(string typedText, string compText) {
            // Match at least something
            int i = 0;
            for (i = 0; i < Math.Min(typedText.Length, compText.Length); i++) {
                if (typedText[i] != compText[i]) {
                    return i;
                }
            }

            return i;
        }

        private string GetTypedText() {
            ITextSnapshot snapshot = ApplicableTo.TextBuffer.CurrentSnapshot;
            return ApplicableTo.GetText(snapshot);
        }

        private static CompletionList OrderList(IReadOnlyCollection<Completion> completions) {
            // Place 'name =' at the top prioritizing argument names
            // Place items starting with non-alpha characters like .Call and &&
            // at the end of the list.
            var argumentNames = completions.Where(x => RTokenizer.IsIdentifierCharacter(x.DisplayText[0]) && x.DisplayText.EndsWith("=", StringComparison.Ordinal));

            var rtvsNames = completions.Where(x => x.DisplayText.IndexOf(".rtvs") >= 0);
            var specialNames = completions.Where(x => !char.IsLetter(x.DisplayText[0]));
            specialNames = specialNames.Except(rtvsNames);

            var generalEntries = completions.Except(argumentNames);
            generalEntries = generalEntries.Except(rtvsNames);
            generalEntries = generalEntries.Except(specialNames);

            List<Completion> orderedCompletions = new List<Completion>();
            orderedCompletions.AddRange(argumentNames);
            orderedCompletions.AddRange(generalEntries);
            orderedCompletions.AddRange(specialNames);

            return new CompletionList(orderedCompletions);
        }
    }
}
