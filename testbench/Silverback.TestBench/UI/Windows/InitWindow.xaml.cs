// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.TestBench.ViewModel;

namespace Silverback.TestBench.UI.Windows;

public partial class InitWindow
{
    public InitWindow(InitViewModel viewModel, MainWindow mainWindow)
    {
        InitializeComponent();

        DataContext = viewModel;

        viewModel.InitializationCompleted += (_, _) =>
        {
            mainWindow.Show();
            Close();
        };
    }
}
