﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.R.Components.PackageManager.ViewModel;

namespace Microsoft.R.Components.PackageManager.Implementation.View {
    /// <summary>
    /// Interaction logic for PackageManagerControl.xaml
    /// </summary>
    public partial class PackageManagerControl : UserControl {
        private IRPackageManagerViewModel Model => DataContext as IRPackageManagerViewModel;

        public PackageManagerControl() {
            InitializeComponent();
        }

        private void PackageManagerControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (TabLoaded.IsChecked.HasValue && TabLoaded.IsChecked.Value) {
                Model?.SwitchToLoadedPackages();
            } else if (TabInstalled.IsChecked.HasValue && TabInstalled.IsChecked.Value) {
                Model?.SwitchToInstalledPackages();
            } else if (TabAvailable.IsChecked.HasValue && TabAvailable.IsChecked.Value) {
                Model?.SwitchToAvailablePackages();
            }
        }

        private void CheckBoxSuppressLegalDisclaimer_Checked(object sender, RoutedEventArgs e) {
            throw new NotImplementedException();
        }

        private void TabLoaded_Checked(object sender, RoutedEventArgs e) {
            Model?.SwitchToLoadedPackages();
        }

        private void TabInstalled_Checked(object sender, RoutedEventArgs e) {
            Model?.SwitchToInstalledPackages();
        }

        private void TabAvailable_Checked(object sender, RoutedEventArgs e) {
            Model?.SwitchToAvailablePackages();
        }

        private void ButtonSettings_Click(object sender, RoutedEventArgs e) {
            throw new NotImplementedException();
        }

        private void ListPackages_Loaded(object sender, RoutedEventArgs e) {
            Model?.ReloadItems();
        }
    }
}
