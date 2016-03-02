﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.UnitTests.Core.XUnit {
    [ExcludeFromCodeCoverage]
    public class TestMethodInfoFixture {
        public MethodInfo Method { get; }

        public TestMethodInfoFixture() { }

        public TestMethodInfoFixture(MethodInfo method) {
            Method = method;
        }
    }
}