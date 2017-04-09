﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using FluentAssertions;
using Microsoft.Common.Core;
using Microsoft.Common.Core.Net;
using Microsoft.Common.Core.OS;
using Microsoft.Common.Core.Services;
using Microsoft.Common.Core.Shell;
using Microsoft.Common.Core.Telemetry;
using Microsoft.Common.Core.Test.Registry;
using Microsoft.Common.Core.UI;
using Microsoft.R.Interpreters;
using Microsoft.UnitTests.Core.XUnit;
using Microsoft.VisualStudio.R.Package.RClient;
using Microsoft.VisualStudio.R.Package.Telemetry;
using NSubstitute;
using Xunit;

namespace Microsoft.VisualStudio.R.Package.Test.RClient {
    [ExcludeFromCodeCoverage]
    [Collection(CollectionNames.NonParallel)]
    [Category.R.Install]
    public class RClientTest {
        [Test]
        public void InstallTelemetry() {
            var telemetryEvents = new List<string>();
            var telemetry = Substitute.For<ITelemetryService>();
            telemetry.When(x => x.ReportEvent(Arg.Any<TelemetryArea>(), Arg.Any<string>(), Arg.Any<object>()))
                     .Do(x => telemetryEvents.Add(x.Args()[1] as string));

            var coreShell = Substitute.For<ICoreShell>();
            coreShell.Telemetry().Returns(telemetry);

            var ps = Substitute.For<IProcessServices>();
            ps.When(x => x.Start(Arg.Any<string>())).Do(c => {
                c.Args()[0].Should().NotBeNull();
            });
            coreShell.Process().Returns(ps);

            coreShell.ShowMessage(Arg.Any<string>(), Arg.Any<MessageButtons>()).Returns(MessageButtons.Yes);

            var downloader = Substitute.For<IFileDownloader>();
            downloader.Download(null, null, CancellationToken.None).ReturnsForAnyArgs((string)null);

            var inst = new MicrosoftRClientInstaller();
            inst.LaunchRClientSetup(coreShell.Services, downloader);

            telemetryEvents.Should().HaveCount(1);
            telemetryEvents[0].Should().Be(RtvsTelemetry.ConfigurationEvents.RClientInstallYes);

            downloader.Download(null, null, CancellationToken.None).ReturnsForAnyArgs("Failed");

            telemetryEvents.Clear();
            inst.LaunchRClientSetup(coreShell.Services, downloader);

            telemetryEvents.Should().HaveCount(2);
            telemetryEvents[0].Should().Be(RtvsTelemetry.ConfigurationEvents.RClientInstallYes);
            telemetryEvents[1].Should().Be(RtvsTelemetry.ConfigurationEvents.RClientDownloadFailed);

            downloader.Download(null, null, CancellationToken.None).ReturnsForAnyArgs((string)null);

            ps = Substitute.For<IProcessServices>();
            ps.When(x => x.Start(Arg.Any<string>())).Do(c => {
                throw new Win32Exception((unchecked((int)0x800704C7)));
            });
            coreShell.Process().Returns(ps);

            telemetryEvents.Clear();
            inst.LaunchRClientSetup(coreShell.Services, downloader);

            telemetryEvents.Should().HaveCount(2);
            telemetryEvents[0].Should().Be(RtvsTelemetry.ConfigurationEvents.RClientInstallYes);
            telemetryEvents[1].Should().Be(RtvsTelemetry.ConfigurationEvents.RClientInstallCancel);
        }

        [Test(ThreadType = ThreadType.UI)]
        public void MsRClient() {
            var rClientInstallPath = @"C:\Program Files\Microsoft\R Client\";
            var rClientRPath = @"C:\Program Files\Microsoft\R Client\R_SERVER\";
            var tr = new RegistryMock(SimulateRegistryMsRClient(rClientInstallPath, rClientRPath));

            SqlRClientInstallation.GetRClientPath(tr).Should().Be(rClientRPath);

            var shell = Substitute.For<ICoreShell>();
            var ui = shell.UI();
            ui.ShowMessage(Arg.Any<string>(), Arg.Any<MessageButtons>()).Returns(MessageButtons.Yes);

            MicrosoftRClient.CheckMicrosoftRClientInstall(shell, tr);
            ui.Received(1).ShowMessage(Arg.Any<string>(), Arg.Any<MessageButtons>());

            MicrosoftRClient.CheckMicrosoftRClientInstall(shell);
            ui.Received(1).ShowMessage(Arg.Any<string>(), Arg.Any<MessageButtons>());
        }

        private RegistryKeyMock[] SimulateRegistryMsRClient(string rClientInstallPath, string rClientRPath) {
            return new RegistryKeyMock[] {
                new RegistryKeyMock(
                        name: @"SOFTWARE\Microsoft\R Client",
                        subkeys: null,
                        valueNames: new string[] {"Path"},
                        values: new string[] {rClientInstallPath}),
                new RegistryKeyMock(
                        name: @"SOFTWARE\Microsoft\R Tools\" + Toolset.Version),
                new RegistryKeyMock(
                        name: @"SOFTWARE\Microsoft\Microsoft SQL Server\130\sql_shared_mr",
                        subkeys: null,
                        valueNames: new string[] {"Path"},
                        values: new string[] {rClientRPath}),
                new RegistryKeyMock(
                       @"SOFTWARE\R-core\R64",
                        new RegistryKeyMock[] {
                            new RegistryKeyMock(
                                name: "3.2.5",
                                subkeys: null,
                                valueNames: new string[] {"InstallPath"},
                                values: new string[] {rClientRPath}),
                        })
             };
        }
    }
}
