﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Common.Core;
using Microsoft.Common.Core.Shell;
using Microsoft.Common.Core.Threading;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Host;
using Microsoft.R.Support.Settings;
using static System.FormattableString;

namespace Microsoft.R.Support.Help {
    [Export(typeof(IIntellisenseRSession))]
    public sealed class IntelliSenseRSession : IIntellisenseRSession {
        private readonly ICoreShell _coreShell;
        private readonly IRSessionProvider _sessionProvider;
        private readonly IRInteractiveWorkflow _workflow;
        private readonly BinaryAsyncLock _lock = new BinaryAsyncLock();
        private IEnumerable<string> _loadedPackages = null;

        [ImportingConstructor]
        public IntelliSenseRSession(ICoreShell coreShell, IRInteractiveWorkflowProvider workflowProvider) {
            _coreShell = coreShell;
            _workflow = workflowProvider.GetOrCreate();
            _sessionProvider = _workflow.RSessions;
        }

        /// <summary>
        /// Timeout to allow R-Host to start. Typically only needs
        /// different value in tests or code coverage runs.
        /// </summary>
        public static int HostStartTimeout { get; set; } = 3000;

        public IRSession Session { get; private set; }

        public void Dispose() {
            if (_workflow?.RSession != null) {
                _workflow.RSession.Mutated -= OnInteractiveSessionMutated;
            }
            Session?.Dispose();
            Session = null;
        }

        public async Task<string> GetFunctionPackageNameAsync(string functionName) {
            IRSession session = InteractiveSession;
            string packageName = null;

            if (session != null && session.IsHostRunning) {
                try {
                    packageName = await session.EvaluateAsync<string>(
                        Invariant(
                            $"as.list(find('{functionName}', mode='function')[1])[[1]]"
                        ), REvaluationKind.Normal);
                    if (packageName != null && packageName.StartsWithOrdinal("package:")) {
                        packageName = packageName.Substring(8);
                    }
                } catch (Exception) { }
            }

            return packageName;
        }

        public async Task CreateSessionAsync() {
            var token = await _lock.ResetAsync();
            try {
                if (!_sessionProvider.HasBroker) {
                    throw new RHostDisconnectedException();
                }

                if (Session == null) {
                    Session = _sessionProvider.GetOrCreate(SessionNames.Intellisense);
                }

                if (!Session.IsHostRunning) {
                    int timeout = _coreShell.IsUnitTestEnvironment ? 10000 : 3000;
                    await Session.EnsureHostStartedAsync(new RHostStartupInfo(RToolsSettings.Current.CranMirror, codePage: RToolsSettings.Current.RCodePage), null, timeout);
                }
            } finally {
                token.Set();
            }
        }

        public IEnumerable<string> LoadedPackageNames {
            get {
                if (_loadedPackages == null && _workflow.RSession != null) {
                    _workflow.RSession.Mutated += OnInteractiveSessionMutated;
                    UpdateListOfLoadedPackagesAsync().Wait(2000);
                    _loadedPackages = _loadedPackages ?? Enumerable.Empty<string>();
                }
                return _loadedPackages;
            }
        }

        private void OnInteractiveSessionMutated(object sender, EventArgs e)
             => UpdateListOfLoadedPackagesAsync().DoNotWait();

        private async Task UpdateListOfLoadedPackagesAsync() {
            string result;
            try {
                result = await InteractiveSession.EvaluateAsync<string>("paste0(.packages(), collapse = ' ')", REvaluationKind.Normal);
            } catch (RHostDisconnectedException) {
                return;
            } catch (RException) {
                return;
            }
            ParseSearchResponse(result);
        }

        private void ParseSearchResponse(string response) {
            var loadedPackages = response.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Interlocked.Exchange(ref _loadedPackages, loadedPackages);
        }

        private IRSession InteractiveSession {
            get {
                IRSession session = null;
                if (_workflow.RSession.IsHostRunning) {
                    session = _workflow.RSession;
                } else if (_coreShell.IsUnitTestEnvironment) {
                    session = Session;
                }
                return session;
            }
        }
    }
}
