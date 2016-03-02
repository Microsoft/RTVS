﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.UnitTests.Core.XUnit {
    [ExcludeFromCodeCoverage]
    internal class TestFramework : XunitTestFramework {
        public TestFramework(IMessageSink messageSink) : base(messageSink) {}

        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName) {
            return new TestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
        }
    }
}