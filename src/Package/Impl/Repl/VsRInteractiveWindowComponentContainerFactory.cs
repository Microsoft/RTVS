﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.Common.Core.Shell;
using Microsoft.R.Components.ContentTypes;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Components.InteractiveWorkflow.Implementation;
using Microsoft.R.Host.Client;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Shell;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.R.Package.Repl {
    [Export(typeof(IInteractiveWindowComponentContainerFactory))]
    internal class VsRInteractiveWindowComponentContainerFactory : IInteractiveWindowComponentContainerFactory {
        private readonly Lazy<IVsInteractiveWindowFactory> _vsInteractiveWindowFactoryLazy;
        private readonly IContentTypeRegistryService _contentTypeRegistryService;
        private readonly ICoreShell _shell;

        [ImportingConstructor]
        public VsRInteractiveWindowComponentContainerFactory(
            Lazy<IVsInteractiveWindowFactory> vsInteractiveWindowFactory,
            IContentTypeRegistryService contentTypeRegistryService,
            ICoreShell shell) {
            _vsInteractiveWindowFactoryLazy = vsInteractiveWindowFactory;
            _contentTypeRegistryService = contentTypeRegistryService;
            _shell = shell;
        }

        public IInteractiveWindowVisualComponent Create(int instanceId, IInteractiveEvaluator evaluator, IRSessionProvider sessionProvider) {
            VsAppShell.Current.AssertIsOnMainThread();

            IVsInteractiveWindow vsWindow;
            var vsf2 = _vsInteractiveWindowFactoryLazy.Value as IVsInteractiveWindowFactory2; // Temporary for VS 2017 RC2
            if (vsf2 != null) {
                vsWindow = vsf2.Create(RGuidList.ReplInteractiveWindowProviderGuid, instanceId, string.Empty, evaluator, 
                                      0, RGuidList.RCmdSetGuid, RPackageCommandId.replWindowToolBarId, null);
            } else {
                vsWindow = _vsInteractiveWindowFactoryLazy.Value.Create(RGuidList.ReplInteractiveWindowProviderGuid, instanceId, string.Empty, evaluator);
            }

            var contentType = _contentTypeRegistryService.GetContentType(RContentTypeDefinition.ContentType);
            vsWindow.SetLanguage(RGuidList.RLanguageServiceGuid, contentType);

            var toolWindow = (ToolWindowPane)vsWindow;
            var componentContainer = new VisualComponentToolWindowAdapter<IInteractiveWindowVisualComponent>(toolWindow);
            var component = new RInteractiveWindowVisualComponent(vsWindow.InteractiveWindow, componentContainer, sessionProvider, _shell);
            componentContainer.Component = component;
            return component;
        }
    }
}