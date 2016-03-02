﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.UnitTests.Core.XUnit {
    [ExcludeFromCodeCoverage]
    public sealed class UnitTestTraitDiscoverer : ITraitDiscoverer {
        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute) {
            yield return new KeyValuePair<string, string>("UnitTests", null);
        }
    }
}