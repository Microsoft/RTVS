﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.Common.Core.UI.Commands;
using Microsoft.Languages.Editor.Controllers.Commands;
using Microsoft.Markdown.Editor.ContentTypes;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Markdown.Editor.Commands {
    [Export(typeof(ICommandFactory))]
    [ContentType(MdContentTypeDefinition.ContentType)]
    internal class MdCommandFactory : ICommandFactory {
        private readonly IRInteractiveWorkflowProvider _workflowProvider;

        [ImportingConstructor]
        public MdCommandFactory(IRInteractiveWorkflowProvider workflowProvider) {
            _workflowProvider = workflowProvider;
        }

        public IEnumerable<ICommand> GetCommands(ITextView textView, ITextBuffer textBuffer) {
            var commands = new List<ICommand>() {
                new RunRChunkCommand(textView, _workflowProvider.GetOrCreate())
            };
            return commands;
        }
    }
}
