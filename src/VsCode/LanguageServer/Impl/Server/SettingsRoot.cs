﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Based on https://github.com/CXuesong/LanguageServer.NET

using Microsoft.R.LanguageServer.Server;

namespace Microsoft.R.LanguageServer.Settings {
    public sealed class SettingsRoot {
        public LanguageServerSettings R { get; set; }
    }
}