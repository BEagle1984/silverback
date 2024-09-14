// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Terminal.Gui;

namespace Silverback.TestBench.UI;

public class TestBenchApplication
{
    private readonly OverviewTopLevel _overviewTopLevel;

    public TestBenchApplication(OverviewTopLevel overviewTopLevel)
    {
        _overviewTopLevel = overviewTopLevel;
    }

    public void Run()
    {
        Application.Init();
        PatchColors();

        Application.Run(_overviewTopLevel);
        Application.Shutdown();
    }

    private static void PatchColors()
    {
        Colors.TopLevel.Normal = new Attribute(Color.White, Color.Black);
        Colors.TopLevel.Focus = new Attribute(Color.Blue, Color.Black);
        Colors.TopLevel.HotFocus = new Attribute(Color.White, Color.DarkGray);
    }
}
