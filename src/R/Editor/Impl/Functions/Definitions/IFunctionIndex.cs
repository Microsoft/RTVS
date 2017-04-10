﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.R.Editor.Functions {
    public interface IFunctionIndex {
        Task BuildIndexAsync(IPackageIndex packageIndex = null);
        void RegisterPackageFunctions(IPackageInfo package);
        IFunctionInfo GetFunctionInfo(string functionName, string packageName, Action<object, string> infoReadyCallback = null, object parameter = null);
        Task<IFunctionInfo> GetFunctionInfoAsync(string functionName, string packageName = null);
    }
}