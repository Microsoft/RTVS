﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.R.Package.Commands;

namespace Microsoft.VisualStudio.R.Package.Documentation {
    internal class OpenDocumentationCommand : PackageCommand {
        private string _url;

        public OpenDocumentationCommand(Guid group, int id, string url) :
            base(group, id) {
            _url = url;
        }

        internal override void SetStatus() {
            Enabled = true;
        }

        internal override void Handle() {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.UseShellExecute = true;
            psi.FileName = _url;
            Process.Start(psi);
        }
    }
}
