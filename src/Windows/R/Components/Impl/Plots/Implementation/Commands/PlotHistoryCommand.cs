﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.R.Components.InteractiveWorkflow;

namespace Microsoft.R.Components.Plots.Implementation.Commands {
    internal abstract class PlotHistoryCommand : InteractiveWorkflowAsyncCommand {
        protected IRPlotHistoryVisualComponent VisualComponent { get; }

        public PlotHistoryCommand(IRInteractiveWorkflow interactiveWorkflow, IRPlotHistoryVisualComponent visualComponent) :
            base(interactiveWorkflow) {
            if (visualComponent == null) {
                throw new ArgumentNullException(nameof(visualComponent));
            }

            VisualComponent = visualComponent;
        }
    }
}
