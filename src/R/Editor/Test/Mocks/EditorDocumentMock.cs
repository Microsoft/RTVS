﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Languages.Editor.EditorFactory;
using Microsoft.Languages.Editor.Services;
using Microsoft.Languages.Editor.Workspace;
using Microsoft.R.Editor.Document.Definitions;
using Microsoft.R.Editor.Tree.Definitions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.R.Editor.Test.Mocks
{
    [ExcludeFromCodeCoverage]
    public sealed class EditorDocumentMock : IREditorDocument
    {
        public EditorDocumentMock(IEditorTree tree)
        {
            EditorTree = tree;
            ServiceManager.AddService<IREditorDocument>(this, tree.TextBuffer);
        }

        public IEditorTree EditorTree { get; private set; }

        public void Close() { }

        public bool IsTransient
        {
            get { return false; }
        }

        public bool IsClosed { get; private set; }

        public bool IsMassiveChangeInProgress
        {
            get { return false; }
        }

        public ITextBuffer TextBuffer
        {
            get { return EditorTree.TextBuffer; }
        }

        public IWorkspace Workspace
        {
            get { return null; }
        }

        public IWorkspaceItem WorkspaceItem
        {
            get { return null; }
        }

#pragma warning disable 67
        private readonly object _syncObj = new object();

        public event EventHandler<EventArgs> Activated;
        public event EventHandler<EventArgs> Deactivated;

        public EventHandler<EventArgs> DocumentClosing { get; private set; }
        event EventHandler<EventArgs> IEditorDocument.DocumentClosing {
            add {
                lock (_syncObj) {
                    DocumentClosing = (EventHandler<EventArgs>)Delegate.Combine(DocumentClosing, value);
                }
            }
            remove {
                lock (_syncObj) {
                    DocumentClosing = (EventHandler<EventArgs>)Delegate.Remove(DocumentClosing, value);
                }
            }
        }

        public event EventHandler<EventArgs> MassiveChangeBegun;
        public event EventHandler<EventArgs> MassiveChangeEnded;

        public void BeginMassiveChange()
        {
        }

        public void Dispose()
        {
        }

        public bool EndMassiveChange()
        {
            return true;
        }
    }
}
