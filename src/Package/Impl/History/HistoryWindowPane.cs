﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.Languages.Editor.Services;
using Microsoft.R.Components.Controller;
using Microsoft.R.Components.History;
using Microsoft.R.Components.History.Implementation;
using Microsoft.R.Support.Settings;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Interop;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.R.Package.History {
    [Guid(WindowGuidString)]
    internal class HistoryWindowPane : VisualComponentToolWindow<IRHistoryWindowVisualComponent>, IOleCommandTarget {
        public const string WindowGuidString = "62ACEA29-91C7-4BFC-B76F-550E7B3DE234";
        public static Guid WindowGuid { get; } = new Guid(WindowGuidString);

        private readonly ITextBuffer _historyTextBuffer;
        private readonly IRHistoryProvider _historyProvider;
        private readonly ITextEditorFactoryService _textEditorFactory;
        private IOleCommandTarget _commandTarget;
        private IRHistory _history;
        private IRHistoryFiltering _historyFiltering;

        public HistoryWindowPane(ITextBuffer historyTextBuffer, IRHistoryProvider historyProvider, ITextEditorFactoryService textEditorFactory) {
            _historyTextBuffer = historyTextBuffer;
            _historyProvider = historyProvider;
            _textEditorFactory = textEditorFactory;

            BitmapImageMoniker = KnownMonikers.History;
            Caption = Resources.HistoryWindowCaption;
            ToolBar = new CommandID(RGuidList.RCmdSetGuid, RPackageCommandId.historyWindowToolBarId);
        }

        protected override void OnCreate() {
            Component = new RHistoryWindowVisualComponent(_historyTextBuffer, _historyProvider, _textEditorFactory, this);

            _history = _historyProvider.GetAssociatedRHistory(Component.TextView);
            _history.HistoryChanged += OnHistoryChanged;
            _historyFiltering = _historyProvider.CreateFiltering(Component);
            _commandTarget = new CommandTargetToOleShim(Component.TextView, ServiceManager.GetService<ICommandTarget>(Component.TextView));

            base.OnCreate();
        }

        public override void OnToolWindowCreated() {
            Guid commandUiGuid = VSConstants.GUID_TextEditorFactory;
            ((IVsWindowFrame)Frame).SetGuidProperty((int)__VSFPROPID.VSFPROPID_InheritKeyBindings, ref commandUiGuid);
            base.OnToolWindowCreated();
        }

        protected override void Dispose(bool disposing) {
            if (disposing && _history != null) {
                _commandTarget = null;
                _history.HistoryChanged -= OnHistoryChanged;
                _history = null;
                Component = null;
            }
            base.Dispose(disposing);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
            return _commandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
            return _commandTarget.Exec(ref pguidCmdGroup, nCmdId, nCmdexecopt, pvaIn, pvaOut);
        }

        public override bool SearchEnabled => true;

        public override void ProvideSearchSettings(IVsUIDataSource pSearchSettings) {
            dynamic settings = pSearchSettings;
            settings.SearchStartType = VSSEARCHSTARTTYPE.SST_INSTANT;
            base.ProvideSearchSettings(pSearchSettings);
        }

        public override IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback) {
            return new HistorySearchTask(dwCookie, _historyFiltering, pSearchQuery, pSearchCallback);
        }

        public override void ClearSearch() {
            VsAppShell.Current.DispatchOnUIThread(() => _historyFiltering.ClearFilter());
            base.ClearSearch();
        }

        private void OnHistoryChanged(object sender, EventArgs e) {
            if (RToolsSettings.Current.ClearFilterOnAddHistory) {
                SearchHost.SearchAsync(null);
            }
        }

        private sealed class HistorySearchTask : VsSearchTask {
            private readonly IRHistoryFiltering _historyFiltering;

            public HistorySearchTask(uint dwCookie, IRHistoryFiltering historyFiltering, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
                : base(dwCookie, pSearchQuery, pSearchCallback) {
                _historyFiltering = historyFiltering;
            }

            protected override void OnStartSearch() {
                base.OnStartSearch();
                VsAppShell.Current.DispatchOnUIThread(() => _historyFiltering.Filter(SearchQuery.SearchString));
            }
        }
    }
}
