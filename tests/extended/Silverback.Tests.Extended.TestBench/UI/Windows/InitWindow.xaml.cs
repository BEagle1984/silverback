// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Tests.Extended.TestBench.ViewModel;

namespace Silverback.Tests.Extended.TestBench.UI.Windows;

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
