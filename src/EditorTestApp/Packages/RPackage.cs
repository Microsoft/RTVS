﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Common.Core.Shell;
using Microsoft.Languages.Editor.Composition;
using Microsoft.Languages.Editor.EditorFactory;
using Microsoft.Languages.Editor.Services;
using Microsoft.Languages.Editor.Settings;
using Microsoft.R.Components.ContentTypes;
using Microsoft.R.Editor.Commands;
using Microsoft.R.Editor.Document;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Languages.Editor.Application.Packages {
    [ExcludeFromCodeCoverage]
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(RContentTypeDefinition.ContentType)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [Name("R Text View Connection Listener")]
    [Order(Before = "Default")]
    internal sealed class TestRTextViewConnectionListener : RTextViewConnectionListener {
        private readonly ICoreShell _shell;

        [ImportingConstructor]
        public TestRTextViewConnectionListener(ICoreShell shell) {
            _shell = shell;
        }

        protected override void OnTextBufferCreated(ITextView textView, ITextBuffer textBuffer) {
            InitEditorInstance(textBuffer);
            base.OnTextBufferCreated(textView, textBuffer);
        }

        private void InitEditorInstance(ITextBuffer textBuffer) {
            if (ServiceManager.GetService<IEditorInstance>(textBuffer) == null) {
                var cs = _shell.GetService<ICompositionService>();
                var importComposer = new ContentTypeImportComposer<IEditorFactory>(cs);
                var factory = importComposer.GetImport(textBuffer.ContentType.TypeName);
                var editorInstance = factory.CreateEditorInstance(textBuffer, new RDocumentFactory(_shell));
            }
        }
    }

    [ExcludeFromCodeCoverage]
    [Export(typeof(IWritableEditorSettingsStorage))]
    [ContentType(RContentTypeDefinition.ContentType)]
    [Name("R Test settings")]
    [Order(Before = "Default")]
    internal sealed class RSettingsStorage : SettingsStorage { }
}
