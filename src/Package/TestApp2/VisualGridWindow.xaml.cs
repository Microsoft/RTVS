﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.R.Package.DataInspect;
using static System.FormattableString;

namespace Microsoft.VisualStudio.R.TestApp {
    /// <summary>
    /// Interaction logic for VisualGridWindow.xaml
    /// </summary>
    public partial class VisualGridWindow : Window {
        public VisualGridWindow() {
            InitializeComponent();
        }

        private void AssignDataProvider_Click(object sender, RoutedEventArgs e) {
            RootGrid.Initialize(new DataProvider(1000, 1000));
        }

        private void AssignSmallProvider_Click(object sender, RoutedEventArgs e) {
            RootGrid.Initialize(new DataProvider(3, 2));
        }

        private void ChangeForeground_Click(object sender, RoutedEventArgs e) {
            RootGrid.Foreground = ToggleColor(RootGrid.Foreground, Brushes.White, Brushes.Yellow);
        }

        private void ChangeGridBackground_Click(object sender, RoutedEventArgs e) {
            RootGrid.GridBackground = ToggleColor(RootGrid.GridBackground, Brushes.Green, Brushes.Purple);
            RootGrid.GridLinesBrush = ToggleColor(RootGrid.GridLinesBrush, Brushes.Blue, Brushes.Brown);
        }

        private Brush ToggleColor(Brush brush, Brush value1, Brush value2) {
            if (brush == value1) {
                return value2;
            } else if (brush == value2) {
                return value1;
            }
            return brush;
        }
    }

    class MockGridData : IGridData<string> {
        public MockGridData(GridRange range) {
            ColumnHeader = new DefaultHeaderData(range.Columns, DefaultHeaderData.Mode.Column);

            RowHeader = new DefaultHeaderData(range.Rows, DefaultHeaderData.Mode.Row);

            Grid = new Grid<string>(range, (r, c) => Invariant($"{r}:{c}"));
        }

        public IRange<string> ColumnHeader { get; private set; }

        public IRange<string> RowHeader { get; private set; }

        public IGrid<string> Grid { get; private set; }

    }

    class DataProvider : IGridProvider<string> {
        public DataProvider(int rowCount, int columnCount) {
            RowCount = rowCount;
            ColumnCount = columnCount;
        }

        public int ColumnCount { get; }

        public int RowCount { get; }

        public Task<IGridData<string>> GetAsync(GridRange range) {
            return Task.Run(async () => {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                return (IGridData<string>)new MockGridData(range);
            });
        }
    }
}
