﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.R.Host.Broker.Startup;

namespace Microsoft.R.Host.Broker.Lifetime {
    public class LifetimeManager {
        private readonly LifetimeOptions _options;
        private readonly ILogger _logger;

        private CancellationTokenSource _cts;

        public LifetimeManager(IOptions<LifetimeOptions> options, ILogger<LifetimeManager> logger) {
            _options = options.Value;
            _logger = logger;
        }

        public void Initialize() {
            if (_options.ParentProcessID != null) {
                int pid = _options.ParentProcessID.Value;
                Process process;
                try {
                    process = Process.GetProcessById(pid);
                    process.EnableRaisingEvents = true;
                } catch (ArgumentException) {
                    _logger.LogCritical(Resources.Critical_ParentProcessNotFound, pid);
                    CommonStartup.Exit();
                    return;
                }

                _logger.LogInformation(Resources.Info_MonitoringParentProcess, pid);
                process.Exited += delegate {
                    _logger.LogInformation(Resources.Info_ParentProcessExited, pid);
                    CommonStartup.Exit();
                };
            }

            Ping();
        }

        public void Ping() {
            if (_options.PingTimeout == null) {
                return;
            }

            var cts = new CancellationTokenSource(_options.PingTimeout.Value);
            cts.Token.Register(() => {
                if (_cts == cts) {
                    _logger.LogCritical(Resources.Critical_PingTimeOut);
                    CommonStartup.Exit();
                }
            });
            _cts = cts; 
        }
    }
}
