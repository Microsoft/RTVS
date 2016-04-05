﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.R.Components.PackageManager.Implementation.View {
    /// <summary>
    /// Interaction logic for LoadingStatusBar.xaml
    /// </summary>
    public partial class LoadingStatusBar : UserControl {
        public LoadingStatusBar() {
            InitializeComponent();
        }
        
        private void ButtonShowErrors_Click(object sender, RoutedEventArgs e) {
            throw new NotImplementedException();
        }

        private void ButtonDismiss_Click(object sender, RoutedEventArgs e) {
            throw new NotImplementedException();
        }
    }
}
