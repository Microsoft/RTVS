﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Common.Core.Services;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Editor.Functions;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Host;
using Microsoft.R.LanguageServer.InteractiveWorkflow;

namespace Microsoft.R.LanguageServer.Server {
    /// <summary>
    /// Manages connection to RTVS
    /// </summary>
    internal sealed class RConnection: IDisposable {
        private const int HostStartTimeout = 5000;
        private IRInteractiveWorkflow _workflow;
        private IPackageIndex _packageIndex;

        public async Task ConnectAsync(IServiceContainer services) {
            var provider = services.GetService<IRInteractiveWorkflowProvider>();
            _workflow = provider.GetOrCreate();

            var info = BrokerConnectionInfo.Create(services.Security(), "VSCR", @"C:\Program Files\R\R-3.4.0", string.Empty, false);

            await _workflow.RSessions.TrySwitchBrokerAsync("VSCR", info);
            await _workflow.RSession.StartHostAsync(new RHostStartupInfo(), new RSessionCallback(), HostStartTimeout);
            
            // Start package building
            _packageIndex = services.GetService<IPackageIndex>();
            await _packageIndex.BuildIndexAsync();
        }

        public void Dispose() => _workflow?.Dispose();
    }
}