﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Interpreters;

namespace Microsoft.R.Host.Client.Test.Script {
    [ExcludeFromCodeCoverage]
    public class RHostScript : IDisposable {
        private IRSessionCallback _clientApp;
        private bool _disposed;

        public IRSessionProvider SessionProvider { get; private set; }
        public IRSession Session { get; private set; }

        public static Version RVersion => new RInstallation().GetCompatibleEngines().First().Version;

        public RHostScript(IRSessionProvider sessionProvider, IRSessionCallback clientApp = null) {
            SessionProvider = sessionProvider;
            _clientApp = clientApp;
            InitializeAsync().Wait();
        }

        public RHostScript(IRSessionProvider sessionProvider, bool async, IRSessionCallback clientApp) {
            SessionProvider = sessionProvider;
            _clientApp = clientApp;
        }

        public async Task InitializeAsync(IRSessionCallback clientApp = null) {
            _clientApp = clientApp ?? _clientApp;

            Session = SessionProvider.GetOrCreate(SessionNames.InteractiveWindow);
            if (Session.IsHostRunning) {
                await Session.StopHostAsync();
            }

            await Session.StartHostAsync(new RHostStartupInfo(), _clientApp ?? new RHostClientTestApp(), 50000);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (_disposed) {
                return;
            }

            if (disposing) {
                if (Session != null) {
                    Session.StopHostAsync().Wait(15000);
                    if (Session.IsHostRunning) {
                        Debugger.Launch();
                    }
                    Debug.Assert(!Session.IsHostRunning);
                }

                if (SessionProvider != null) {
                    SessionProvider = null;
                }
            }

            _disposed = true;
        }
    }
}
