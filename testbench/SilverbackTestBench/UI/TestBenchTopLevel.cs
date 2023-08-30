// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Terminal.Gui;

namespace Silverback.TestBench.UI;

public sealed class TestBenchTopLevel : Toplevel
{
    private readonly OverviewTopLevel _overviewTopLevel;

    public TestBenchTopLevel(OverviewTopLevel overviewView)
    {
        _overviewTopLevel = overviewView;

        PositionAndResize();
        IsMdiContainer = true;

        MenuBar = CreateAndAddMenuBar();
    }

    public override void OnLoaded()
    {
        base.OnLoaded();
        SwitchTo(_overviewTopLevel);
    }

    private void PositionAndResize()
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();
    }

    private MenuBar CreateAndAddMenuBar()
    {
        MenuBar menuBar = new(
            new MenuBarItem[]
            {
                new("_Overview", string.Empty, () => SwitchTo(_overviewTopLevel)),
                new("_Exit", string.Empty, () => Application.RequestStop(this))
            });

        Add(menuBar);

        return menuBar;
    }

    private void SwitchTo(Toplevel childView)
    {
        //if (!Application.MdiChildes.Contains(childView))
            Application.Top.Add(childView);

        //childView.ShowChild();
    }
}
