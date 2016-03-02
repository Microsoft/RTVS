﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.R.Packages {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class ProvideDebugPortSupplierAttribute : RegistrationAttribute {
        private readonly string _id, _name;
        private readonly Type _portSupplier, _portPicker;

        public ProvideDebugPortSupplierAttribute(string name, Type portSupplier, string id, Type portPicker = null) {
            _name = name;
            _portSupplier = portSupplier;
            _id = id;
            _portPicker = portPicker;
        }

        public override void Register(RegistrationContext context) {
            var engineKey = context.CreateKey("AD7Metrics\\PortSupplier\\" + new Guid(_id).ToString("B"));
            engineKey.SetValue("Name", _name);
            engineKey.SetValue("CLSID", _portSupplier.GUID.ToString("B"));
            if (_portPicker != null) {
                engineKey.SetValue("PortPickerCLSID", _portPicker.GUID.ToString("B"));
            }

            var clsidKey = context.CreateKey("CLSID");
            var clsidGuidKey = clsidKey.CreateSubkey(_portSupplier.GUID.ToString("B"));
            clsidGuidKey.SetValue("Assembly", _portSupplier.Assembly.FullName);
            clsidGuidKey.SetValue("Class", _portSupplier.FullName);
            clsidGuidKey.SetValue("InprocServer32", context.InprocServerPath);
            clsidGuidKey.SetValue("CodeBase", Path.Combine(context.ComponentPath, _portSupplier.Module.Name));
            clsidGuidKey.SetValue("ThreadingModel", "Free");
        }

        public override void Unregister(RegistrationContext context) { }
    }
}
