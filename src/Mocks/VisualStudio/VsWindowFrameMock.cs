﻿using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Shell.Mocks {
    public sealed class VsWindowFrameMock : IVsWindowFrame, IVsWindowFrame2 {
        #region IVsWindowFrame
        public int CloseFrame(uint grfSaveOptions) {
            return VSConstants.S_OK;
        }

        public int GetFramePos(VSSETFRAMEPOS[] pdwSFP, out Guid pguidRelativeTo, out int px, out int py, out int pcx, out int pcy) {
            pguidRelativeTo = Guid.Empty;
            px = py = 0;
            pcx = pcy = 100;
            return VSConstants.S_OK;
        }

        public int GetGuidProperty(int propid, out Guid pguid) {
            pguid = Guid.Empty;
            return VSConstants.S_OK;
        }

        public int GetProperty(int propid, out object pvar) {
            if (propid == (int)__VSFPROPID.VSFPROPID_ExtWindowObject) {
                pvar = new VsToolWindowToolbarHostMock();
            } else {
                pvar = null;
            }
            return VSConstants.S_OK;
        }

        public int Hide() {
            return VSConstants.S_OK;
        }

        public int IsOnScreen(out int pfOnScreen) {
            pfOnScreen = 1;
            return VSConstants.S_OK;
        }

        public int IsVisible() {
            return VSConstants.S_OK;
        }

        public int QueryViewInterface(ref Guid riid, out IntPtr ppv) {
            ppv = IntPtr.Zero;
            return VSConstants.E_NOTIMPL;
        }

        public int SetFramePos(VSSETFRAMEPOS dwSFP, ref Guid rguidRelativeTo, int x, int y, int cx, int cy) {
            return VSConstants.S_OK;
        }

        public int SetGuidProperty(int propid, ref Guid rguid) {
            return VSConstants.S_OK;
        }

        public int SetProperty(int propid, object var) {
            return VSConstants.S_OK;
        }

        public int Show() {
            return VSConstants.S_OK;
        }

        public int ShowNoActivate() {
            return VSConstants.S_OK;
        }
        #endregion

        #region IVsWindowFrame2
        public int ActivateOwnerDockedWindow() {
            return VSConstants.S_OK;
        }

        public int Advise(IVsWindowFrameNotify pNotify, out uint pdwCookie) {
            pdwCookie = 1;
            return VSConstants.S_OK;
        }

        public int Unadvise(uint dwCookie) {
            return VSConstants.S_OK;
        }
        #endregion
    }
}
