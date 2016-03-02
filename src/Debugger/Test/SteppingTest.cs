﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Common.Core.Test.Script;
using Microsoft.Languages.Editor.Shell;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Test.Script;
using Microsoft.UnitTests.Core.XUnit;
using Xunit;

namespace Microsoft.R.Debugger.Test {
    [ExcludeFromCodeCoverage]
    [Collection(CollectionNames.NonParallel)]
    public class SteppingTest {
        private const string code =
@"f <- function(x) {
  x + 1
}
x <- 1
y <- f(x)
z <- x + y";

        [Test]
        [Category.R.Debugger]
        public async Task StepOver() {
            var sessionProvider = EditorShell.Current.ExportProvider.GetExportedValue<IRSessionProvider>();
            using (new RHostScript(sessionProvider)) {
                IRSession session = sessionProvider.GetOrCreate(GuidList.InteractiveWindowRSessionGuid, new RHostClientTestApp());
                using (var debugSession = new DebugSession(session)) {
                    using (var sf = new SourceFile(code)) {
                        await debugSession.EnableBreakpointsAsync(true);

                        var bp = await debugSession.CreateBreakpointAsync(new DebugBreakpointLocation(sf.FilePath, 5));
                        var bpHit = new TaskCompletionSource<bool>();
                        bp.BreakpointHit += (s, e) => {
                            bpHit.SetResult(true);
                        };

                        await sf.Source(session);
                        await bpHit.Task;

                        var stackFrames = (await debugSession.GetStackFramesAsync()).Reverse().ToArray();
                        stackFrames.Should().NotBeEmpty();
                        stackFrames[0].LineNumber.Should().Be(5);

                        bool stepCompleted = await debugSession.StepOverAsync();
                        stepCompleted.Should().Be(true);

                        stackFrames = (await debugSession.GetStackFramesAsync()).Reverse().ToArray();
                        stackFrames.Should().NotBeEmpty();
                        stackFrames[0].LineNumber.Should().Be(6);
                    }
                }
            }
        }

        [Test]
        [Category.R.Debugger]
        public async Task StepInto() {
            var sessionProvider = EditorShell.Current.ExportProvider.GetExportedValue<IRSessionProvider>();
            using (new RHostScript(sessionProvider)) {
                IRSession session = sessionProvider.GetOrCreate(GuidList.InteractiveWindowRSessionGuid, new RHostClientTestApp());
                using (var debugSession = new DebugSession(session)) {
                    using (var sf = new SourceFile(code)) {
                        await debugSession.EnableBreakpointsAsync(true);

                        var bp = await debugSession.CreateBreakpointAsync(new DebugBreakpointLocation(sf.FilePath, 5));
                        var bpHit = new TaskCompletionSource<bool>();
                        bp.BreakpointHit += (s, e) => {
                            bpHit.SetResult(true);
                        };

                        await sf.Source(session);
                        await bpHit.Task;

                        var stackFrames = (await debugSession.GetStackFramesAsync()).Reverse().ToArray();
                        stackFrames.Should().NotBeEmpty();
                        stackFrames[0].LineNumber.Should().Be(5);

                        bool stepCompleted = await debugSession.StepIntoAsync();
                        stepCompleted.Should().Be(true);

                        stackFrames = (await debugSession.GetStackFramesAsync()).Reverse().ToArray();
                        stackFrames.Should().HaveCount(n => n >= 2);
                        stackFrames[0].LineNumber.Should().Be(1);
                        stackFrames[1].Call.Should().Be("f(x)");
                    }
                }
            }
        }

        [Test(Skip = "https://github.com/Microsoft/RTVS/issues/975")]
        [Category.R.Debugger]
        public async Task StepOutToGlobal() {
            var sessionProvider = EditorShell.Current.ExportProvider.GetExportedValue<IRSessionProvider>();
            using (new RHostScript(sessionProvider)) {
                IRSession session = sessionProvider.GetOrCreate(GuidList.InteractiveWindowRSessionGuid, new RHostClientTestApp());
                using (var debugSession = new DebugSession(session)) {
                    using (var sf = new SourceFile(code)) {
                        await debugSession.EnableBreakpointsAsync(true);

                        var bp = await debugSession.CreateBreakpointAsync(new DebugBreakpointLocation(sf.FilePath, 2));
                        var bpHit = new TaskCompletionSource<bool>();
                        bp.BreakpointHit += (s, e) => {
                            bpHit.SetResult(true);
                        };

                        await sf.Source(session);
                        await bpHit.Task;

                        var stackFrames = (await debugSession.GetStackFramesAsync()).Reverse().ToArray();
                        stackFrames.Should().HaveCount(n => n >= 2);
                        stackFrames[0].LineNumber.Should().Be(bp.Location.LineNumber);
                        stackFrames[1].Call.Should().Be("f(x)");

                        bool stepCompleted = await debugSession.StepOutAsync();
                        stepCompleted.Should().Be(true);

                        stackFrames = (await debugSession.GetStackFramesAsync()).Reverse().ToArray();
                        stackFrames.Should().HaveCount(n => n >= 2);
                        stackFrames[0].LineNumber.Should().Be(6);
                        stackFrames[1].Call.Should().NotBe("f(x)");
                    }
                }
            }
        }

        [Test]
        [Category.R.Debugger]
        public async Task StepOutToFunction() {
            const string code =
@"f <- function() {
    1
}
g <- function() {
    f()
    1
}
g()";

            var sessionProvider = EditorShell.Current.ExportProvider.GetExportedValue<IRSessionProvider>();
            using (new RHostScript(sessionProvider)) {
                IRSession session = sessionProvider.GetOrCreate(GuidList.InteractiveWindowRSessionGuid, new RHostClientTestApp());
                using (var debugSession = new DebugSession(session)) {
                    using (var sf = new SourceFile(code)) {
                        await debugSession.EnableBreakpointsAsync(true);

                        var bp = await debugSession.CreateBreakpointAsync(new DebugBreakpointLocation(sf.FilePath, 2));
                        var bpHit = new TaskCompletionSource<bool>();
                        bp.BreakpointHit += (s, e) => {
                            bpHit.SetResult(true);
                        };

                        await sf.Source(session);
                        await bpHit.Task;

                        var stackFrames = (await debugSession.GetStackFramesAsync()).Reverse().ToArray();
                        stackFrames.Should().HaveCount(n => n >= 2);
                        stackFrames[0].LineNumber.Should().Be(bp.Location.LineNumber);
                        stackFrames[1].Call.Should().Be("f()");

                        bool stepCompleted = await debugSession.StepOutAsync();
                        stepCompleted.Should().Be(true);

                        stackFrames = (await debugSession.GetStackFramesAsync()).Reverse().ToArray();
                        stackFrames.Should().HaveCount(n => n >= 2);
                        stackFrames[0].LineNumber.Should().Be(6);
                        stackFrames[1].Call.Should().Be("g()");
                    }
                }
            }
        }

        [Test]
        [Category.R.Debugger]
        public async Task StepOutFromGlobal() {
            var sessionProvider = EditorShell.Current.ExportProvider.GetExportedValue<IRSessionProvider>();
            using (new RHostScript(sessionProvider)) {
                IRSession session = sessionProvider.GetOrCreate(GuidList.InteractiveWindowRSessionGuid, new RHostClientTestApp());
                using (var debugSession = new DebugSession(session)) {
                    using (var sf = new SourceFile(code)) {
                        await debugSession.EnableBreakpointsAsync(true);

                        var bp1 = await debugSession.CreateBreakpointAsync(new DebugBreakpointLocation(sf.FilePath, 4));
                        var bp2 = await debugSession.CreateBreakpointAsync(new DebugBreakpointLocation(sf.FilePath, 5));

                        var bpHit = new TaskCompletionSource<bool>();
                        bp1.BreakpointHit += (s, e) => {
                            bpHit.SetResult(true);
                        };

                        await sf.Source(session);
                        await bpHit.Task;

                        var stackFrames = (await debugSession.GetStackFramesAsync()).Reverse().ToArray();
                        stackFrames[0].LineNumber.Should().Be(bp1.Location.LineNumber);

                        bool stepSuccessful = await debugSession.StepOutAsync();
                        stepSuccessful.Should().Be(false);

                        stackFrames = (await debugSession.GetStackFramesAsync()).Reverse().ToArray();
                        stackFrames[0].LineNumber.Should().Be(bp2.Location.LineNumber);
                    }
                }
            }
        }
    }
}
