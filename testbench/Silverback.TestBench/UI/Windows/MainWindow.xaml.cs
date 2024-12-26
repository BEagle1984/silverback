// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Windows.Input;
using Silverback.TestBench.ViewModel;

namespace Silverback.TestBench.UI.Windows;

public partial class MainWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key is Key.D1 or Key.NumPad1)
            Tabs.SelectedIndex = 0;
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key is Key.D2 or Key.NumPad2)
            Tabs.SelectedIndex = 1;
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key is Key.D3 or Key.NumPad3)
            Tabs.SelectedIndex = 2;
    }
}
