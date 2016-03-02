﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Common.Core.OS;
using Microsoft.Win32;

namespace Microsoft.R.Actions.Test.Installation {
    internal sealed class RegistryMock : IRegistry {
        private readonly RegistryKeyMock[] _keys;

        public RegistryMock(RegistryKeyMock[] keys) {
            _keys = keys;
        }

        public IRegistryKey OpenBaseKey(RegistryHive hive, RegistryView view) {
            return new RegistryBaseKeyMock(_keys);
        }
    }
}
