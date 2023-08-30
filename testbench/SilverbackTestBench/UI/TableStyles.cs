// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.TestBench.UI.CustomControls;
using Terminal.Gui;

namespace Silverback.TestBench.UI;

public static class TableStyles
{
    public static ColorScheme DefaultColorScheme { get; } = new()
    {
        Normal = new Attribute(Color.White, Color.Black),
        Focus = new Attribute(Color.Blue, Color.Black),
        HotFocus = new Attribute(Color.White, Color.DarkGray),
        HotNormal = new Attribute(Color.White, Color.Black)
    };

    public static ColorScheme GreenCellColorScheme { get; } = new()
    {
        Normal = new Attribute(Color.BrightGreen, Colors.TopLevel.Normal.Background),
        Focus = new Attribute(Color.BrightGreen, Colors.TopLevel.Focus.Background),
        HotFocus = new Attribute(Color.BrightGreen, Colors.TopLevel.HotFocus.Background),
    };

    public static ColorScheme RedCellColorScheme { get; } = new()
    {
        Normal = new Attribute(Color.BrightRed, Colors.TopLevel.Normal.Background),
        Focus = new Attribute(Color.BrightRed, Colors.TopLevel.Focus.Background),
        HotFocus = new Attribute(Color.BrightRed, Colors.TopLevel.HotFocus.Background),
    };

    public static ColorScheme GrayCellColorScheme { get; } = new()
    {
        Normal = new Attribute(Color.Gray, Colors.TopLevel.Normal.Background),
        Focus = new Attribute(Color.Gray, Colors.TopLevel.Focus.Background),
        HotFocus = new Attribute(Color.Gray, Colors.TopLevel.HotFocus.Background),
    };

    public static ColorScheme BrownCellColorScheme { get; } = new()
    {
        Normal = new Attribute(Color.Brown, Colors.TopLevel.Normal.Background),
        Focus = new Attribute(Color.Brown, Colors.TopLevel.Focus.Background),
        HotFocus = new Attribute(Color.Brown, Colors.TopLevel.HotFocus.Background),
    };

    public static CustomTableView.ColumnStyle NumberColumnStyle { get; } = new()
    {
        Alignment = TextAlignment.Right
    };

    public static CustomTableView.ColumnStyle TimeColumnStyle { get; } = new()
    {
        Alignment = TextAlignment.Centered,
        Format = "HH:mm:ss"
    };

    public static CustomTableView.ColumnStyle ErrorsColumnStyle { get; } = new()
    {
        Alignment = TextAlignment.Right,
        ColorGetter = args => (int?)args.CellValue switch
        {
            <= 0 => Colors.TopLevel,
            _ => RedCellColorScheme
        }
    };

    public static CustomTableView.ColumnStyle GetScaleColoringColumnStyle(int orangeThreshold = 5, int redThreshold = 100) => new()
    {
        Alignment = TextAlignment.Right,
        ColorGetter = args =>
        {
            int intValue = (int)args.CellValue;

            if (intValue >= redThreshold)
                return RedCellColorScheme;

            if (intValue >= orangeThreshold)
                return BrownCellColorScheme;

            return GreenCellColorScheme;
        }
    };
}
