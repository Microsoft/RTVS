﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UnitTests.Core.XUnit;
using Microsoft.VisualStudio.ProjectSystem.FileSystemMirroring.Test;
using Microsoft.VisualStudio.R.Package.Test.Fixtures;

[assembly: TestFrameworkOverride]
[assembly: CpsAssemblyLoader]
[assembly: AssemblyFixtureImport(typeof(RPackageServicesFixture))]