﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.R.Package.Editors;
using Microsoft.VisualStudio.Shell.Mocks;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.R.Package.Test.Utility
{
    [ExcludeFromCodeCoverage]
    internal sealed class TestInstanceFactory : IObjectInstanceFactory
    {
        public object CreateInstance(ref Guid clsid, ref Guid riid, Type objectType)
        {
            if (objectType == typeof(IVsTextLines))
            {
                return new VsTextLinesMock();
            }

            if (objectType == typeof(IVsTextBuffer))
            {
                return new VsTextBufferMock();
            }

            throw new InvalidOperationException("Don't know how to create instance of " + objectType.FullName);
        }
    }
}
