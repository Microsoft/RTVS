﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.R.Components.PackageManager.Model;

namespace Microsoft.R.Components.Test.PackageManager {
    internal static class TestPackages {
        public static readonly RPackage RtvsLib1 = new RPackage() {
            Package = TestPackages.RtvsLib1Description.Package,
            Version = TestPackages.RtvsLib1Description.Version,
            Depends = TestPackages.RtvsLib1Description.Depends,
            License = TestPackages.RtvsLib1Description.License,
            Built = TestPackages.RtvsLib1Description.Built,
            Author = TestPackages.RtvsLib1Description.Author,
            Title = TestPackages.RtvsLib1Description.Title,
            NeedsCompilation = "no",
            Description = TestPackages.RtvsLib1Description.DescriptionFromInstalled,
        };

        public static readonly RPackage RtvsLib1Additional = new RPackage {
            Package = TestPackages.RtvsLib1Description.Package,
            Version = TestPackages.RtvsLib1Description.Version,
            Depends = TestPackages.RtvsLib1Description.Depends,
            License = TestPackages.RtvsLib1Description.License,
            Built = TestPackages.RtvsLib1Description.Built,
            Author = TestPackages.RtvsLib1Description.Author,
            Title = TestPackages.RtvsLib1Description.Title,
            NeedsCompilation = "no",
            Description = TestPackages.RtvsLib1Description.Description,
            Published = TestPackages.RtvsLib1Description.Published,
            Maintainer = TestPackages.RtvsLib1Description.Maintainer,
        };

        /// <summary>
        /// Contents of DESCRIPTION and index.html for rtvslib1 package.
        /// </summary>
        public static class RtvsLib1Description {
            public const string Package = "rtvslib1";
            public const string Version = "0.1.0";
            public const string Depends = "R (>= 3.2.0)";
            public const string License = "MIT";
            public const string Built = "3.2.3";
            public const string Author = "RTVS Team";
            public const string Title = "The title for rtvslib1";
            public const string Description = "This is a library that is used only for testing package installation. It doesn't do anything.";
            public const string DescriptionFromInstalled = "This is a library that is used only for testing package installation.  It doesn't do anything.";
            public const string Published = "2016-03-28";
            public const string Maintainer = "RTVS Team <rtvsuserfeedback@microsoft.com>";
        }
    }
}
