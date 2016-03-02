﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Shell.Mocks {
    [ExcludeFromCodeCoverage]
    public sealed class ProjectServiceMock : ProjectService {
        public IEnumerable<UnconfiguredProject> LoadedUnconfiguredProjects {
            get {
                throw new NotImplementedException();
            }
        }

        public IImmutableSet<string> ServiceCapabilities {
            get {
                throw new NotImplementedException();
            }
        }

        public IProjectServices Services {
            get {
                throw new NotImplementedException();
            }
        }

        public IComparable Version {
            get {
                throw new NotImplementedException();
            }
        }

#pragma warning disable 67
        public event EventHandler Changed;

        public bool IsProjectCapabilityPresent(string projectCapability) {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<UnconfiguredProject> LoadProjectAsync(System.Xml.XmlReader reader, System.Collections.Immutable.IImmutableSet<string> projectCapabilities = null) {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<UnconfiguredProject> LoadProjectAsync(string projectLocation, System.Collections.Immutable.IImmutableSet<string> projectCapabilities = null) {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task UnloadProjectAsync(UnconfiguredProject project) {
            throw new NotImplementedException();
        }
    }
}
