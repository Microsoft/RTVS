﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Languages.Editor.Shell;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using IVsServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.R.Package.Editors {
    using Shell;
    using Package = Microsoft.VisualStudio.Shell.Package;

    /// <summary>
    /// Base editor factory for all Web editors
    /// </summary>
    public class BaseEditorFactory : IVsEditorFactory, IDisposable {
        [Import]
        protected IVsEditorAdaptersFactoryService _adaptersFactory = null;

        protected Microsoft.VisualStudio.Shell.Package Package { get; private set; }
        protected IVsServiceProvider VsServiceProvider { get; private set; }
        protected List<TextBufferInitializationTracker> InitializationTrackers { get; private set; }
        protected Guid LanguageServiceId { get; private set; }

        private Guid _editorFactoryId;
        private bool _encoding;

        public BaseEditorFactory(Package package, Guid editorFactoryId, Guid languageServiceId, bool encoding = false) {
            VsAppShell.Current.CompositionService.SatisfyImportsOnce(this);
            Package = package;
            InitializationTrackers = new List<TextBufferInitializationTracker>();
            _editorFactoryId = editorFactoryId;
            _encoding = encoding;
            LanguageServiceId = languageServiceId;

        }

        internal IObjectInstanceFactory InstanceFactory { get; set; }

        public void SetEncoding(bool value) {
            this._encoding = value;
        }

        public virtual int CreateEditorInstance(
           uint createEditorFlags,
           string documentMoniker,
           string physicalView,
           IVsHierarchy hierarchy,
           uint itemid,
           IntPtr docDataExisting,
           out IntPtr docView,
           out IntPtr docData,
           out string editorCaption,
           out Guid commandUIGuid,
           out int createDocumentWindowFlags) {

            return CreateEditorInstance(createEditorFlags, documentMoniker, physicalView, hierarchy, itemid, docDataExisting,
                                        LanguageServiceId, out docView, out docData, out editorCaption, out commandUIGuid, out createDocumentWindowFlags);
        }

        protected int CreateEditorInstance(
           uint createEditorFlags,
           string documentMoniker,
           string physicalView,
           IVsHierarchy hierarchy,
           uint itemid,
           IntPtr docDataExisting,
           Guid languageServiceId,
           out IntPtr docView,
           out IntPtr docData,
           out string editorCaption,
           out Guid commandUIGuid,
           out int createDocumentWindowFlags) {
            // Initialize output parameters
            docView = IntPtr.Zero;
            docData = IntPtr.Zero;
            commandUIGuid = _editorFactoryId;
            createDocumentWindowFlags = 0;
            editorCaption = null;

            // Validate inputs
            if ((createEditorFlags & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0) {
                return VSConstants.E_INVALIDARG;
            }

            // Get a text buffer
            IVsTextLines textLines = GetTextBuffer(docDataExisting, languageServiceId);

            if (IsIncompatibleContentType(textLines)) {
                // Unknown docData type then, so we have to force VS to close the other editor.
                return (int)VSConstants.VS_E_INCOMPATIBLEDOCDATA;
            }

            if (this._encoding) {
                // Force to close the editor if it's currently open
                if (docDataExisting != IntPtr.Zero) {
                    docView = IntPtr.Zero;
                    docData = IntPtr.Zero;
                    editorCaption = null;
                    commandUIGuid = Guid.Empty;
                    createDocumentWindowFlags = 0;
                    return (int)VSConstants.VS_E_INCOMPATIBLEDOCDATA;
                }

                IVsUserData userData = textLines as IVsUserData;
                if (userData != null) {
                    int hresult = userData.SetData(
                        VSConstants.VsTextBufferUserDataGuid.VsBufferEncodingPromptOnLoad_guid,
                        (uint)__PROMPTONLOADFLAGS.codepagePrompt);

                    if (ErrorHandler.Failed(hresult)) {
                        return hresult;
                    }
                }
            }

            // Assign docData IntPtr to either existing docData or the new text buffer
            if (docDataExisting != IntPtr.Zero) {
                docData = docDataExisting;
                Marshal.AddRef(docData);
            } else {
                docData = Marshal.GetIUnknownForObject(textLines);
            }

            try {
                docView = CreateDocumentView(
                    physicalView,
                    documentMoniker,
                    hierarchy,
                    (VSConstants.VSITEMID)itemid,
                    textLines,
                    docDataExisting,
                    languageServiceId,
                    out editorCaption,
                    out commandUIGuid);
            } finally {
                if (docView == IntPtr.Zero) {
                    if (docDataExisting != docData && docData != IntPtr.Zero) {
                        // Cleanup the instance of the docData that we have addref'ed
                        Marshal.Release(docData);
                        docData = IntPtr.Zero;
                    }
                }
            }

            return VSConstants.S_OK;
        }

        protected virtual bool IsIncompatibleContentType(IVsTextLines textLines) {
            return false;
        }

        private IVsTextLines GetTextBuffer(IntPtr docDataExisting, Guid languageServiceId) {
            IVsTextLines textLines = null;

            if (docDataExisting == IntPtr.Zero) {
                // Create a new IVsTextLines buffer.
                Type textLinesType = typeof(IVsTextLines);
                Guid clsid = typeof(VsTextBufferClass).GUID;
                textLines = CreateInstance<IVsTextLines>(ref clsid);

                // set the buffer's site
                ((IObjectWithSite)textLines).SetSite(VsServiceProvider);
                textLines.SetLanguageServiceID(ref languageServiceId);
            } else {
                // Use the existing text buffer
                object dataObject = Marshal.GetObjectForIUnknown(docDataExisting);
                textLines = dataObject as IVsTextLines;

                if (textLines == null) {
                    // Try get the text buffer from textbuffer provider
                    IVsTextBufferProvider textBufferProvider = dataObject as IVsTextBufferProvider;

                    if (textBufferProvider != null) {
                        textBufferProvider.GetTextBuffer(out textLines);
                    }
                }
            }

            if (textLines == null) {
                // Unknown docData type then, so we have to force VS to close the other editor.
                ErrorHandler.ThrowOnFailure((int)VSConstants.VS_E_INCOMPATIBLEDOCDATA);
            }

            return textLines;
        }

        private IntPtr CreateDocumentView(
            string physicalView,
            string documentName,
            IVsHierarchy hierarchy,
            VSConstants.VSITEMID itemid,
            IVsTextLines textLines,
            IntPtr docDataExisting,
            Guid languageServiceId,
            out string editorCaption,
            out Guid cmdUI) {
            // Init out params
            editorCaption = string.Empty;
            cmdUI = Guid.Empty;

            if (string.IsNullOrEmpty(physicalView)) {
                // create code window as default physical view
                return CreateTextView(
                    textLines,
                    documentName,
                    hierarchy,
                    itemid,
                    docDataExisting,
                    languageServiceId,
                    ref editorCaption,
                    ref cmdUI);
            }

            // We couldn't create the view
            // Return special error code so VS can try another editor factory.
            ErrorHandler.ThrowOnFailure((int)VSConstants.VS_E_UNSUPPORTEDFORMAT);

            return IntPtr.Zero;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private IntPtr CreateTextView(
            IVsTextLines textLines,
            string documentName,
            IVsHierarchy hierarchy,
            VSConstants.VSITEMID itemid,
            IntPtr docDataExisting,
            Guid languageServiceId,
            ref string editorCaption,
            ref Guid cmdUI) {
            IVsCodeWindow window = _adaptersFactory.CreateVsCodeWindowAdapter(VsServiceProvider);
            ErrorHandler.ThrowOnFailure(window.SetBuffer(textLines));
            ErrorHandler.ThrowOnFailure(window.SetBaseEditorCaption(null));
            ErrorHandler.ThrowOnFailure(window.GetEditorCaption(READONLYSTATUS.ROSTATUS_Unknown, out editorCaption));

            cmdUI = VSConstants.GUID_TextEditorFactory;

            CreateTextBufferInitializationTracker(textLines, documentName, hierarchy, itemid, docDataExisting, languageServiceId);

            return Marshal.GetIUnknownForObject(window);
        }

        protected virtual void CreateTextBufferInitializationTracker(
            IVsTextLines textLines,
            string documentName,
            IVsHierarchy hierarchy,
            VSConstants.VSITEMID itemid,
            IntPtr docDataExisting,
            Guid languageServiceId) {
            // At this point buffer hasn't been initialized with content yet and hence we cannot 
            // get ITextBuffer from the adapter. In order to get text buffer and properly attach 
            // view filters we need to create a proxy class which will get called when document 
            // is fully loaded and text buffer is finally populated with content.

            TextBufferInitializationTracker tracker = new TextBufferInitializationTracker(
                documentName, hierarchy, itemid, textLines, languageServiceId, InitializationTrackers);

            if (docDataExisting != IntPtr.Zero) {
                // When creating a new view for an existing buffer, the tracker object has to clean itself up
                tracker.OnLoadCompleted(0);
            }
        }

        public virtual int SetSite(IVsServiceProvider psp) {
            VsServiceProvider = psp;
            return VSConstants.S_OK;
        }

        public virtual int Close() {
            VsServiceProvider = null;
            Package = null;

            return VSConstants.S_OK;
        }

        public int MapLogicalView(ref Guid logicalView, out string physicalView) {
            // initialize out parameter
            physicalView = null;

            // Determine the physical view
            // {alexgav} LOGVIEWID_Code is needed by JavaScript Language Service
            // See bug 663657 Double clicking on error list error will try to use legacy editor 
            // to open the file (instead of staying in the libra editor)
            if (VSConstants.LOGVIEWID_Primary == logicalView ||
                VSConstants.LOGVIEWID_TextView == logicalView ||
                VSConstants.LOGVIEWID_Code == logicalView ||
                VSConstants.LOGVIEWID_Debugging == logicalView) {
                return VSConstants.S_OK;
            }

            // E_NOTIMPL must be returned for any unrecognized rguidLogicalView values
            return VSConstants.E_NOTIMPL;
        }

        protected T CreateInstance<T>(ref Guid clsid) where T : class {
            Guid riid = typeof(T).GUID;

            if (InstanceFactory != null) {
                return InstanceFactory.CreateInstance(ref clsid, ref riid, typeof(T)) as T;
            }

            return Package.CreateInstance(ref clsid, ref riid, typeof(T)) as T;
        }

        #region IDisposable
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }
        #endregion
    }
}
