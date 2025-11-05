using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Pikcube.ReadWriteScript.Core.Mutable;
using Pikcube.ReadWriteScript.Core.Special;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs.AppFeatures;

/// <summary>
/// Generic Contorl for selecting a time and ability scope
/// </summary>
public partial class TimeScopeGrid : UserControl
{
    private List<List<CheckBox>> Checkboxes { get; set; }

    private List<CheckBox> AllRowCheckBoxes { get; set; }
    private List<CheckBox> AllColumnCheckBoxes { get; set; }
    private List<TextBlock> AllColumnTextBlocks { get; set; }

    /// <inheritdoc />
    public TimeScopeGrid()
    {
        InitializeComponent();
        Checkboxes = [];
        for (int rowNum = 0; rowNum < 7; ++rowNum)
        {
            Checkboxes.Add([]);
            for (int colNum = 0; colNum < 5; ++colNum)
            {
                int row = rowNum;
                int col = colNum;
                CheckBox checkBox = new()
                {
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Checkboxes[row].Add(checkBox);
                MoreInfoGrid.Children.Add(checkBox);
                Grid.SetColumn(checkBox, col * 2 + 2);
                Grid.SetRow(checkBox, row * 2 + 4);
                checkBox.IsCheckedChanged += (_, _) => CheckBoxCheckedChanged(row, col, checkBox);
            }
        }

        AllRowCheckBoxes =
        [
            AllSelfCheckBox,
            AllTownsfolkCheckBox,
            AllOutsidersCheckBox,
            AllMinionsCheckBox,
            AllDemonsCheckBox,
            AllTravellersCheckBox,
            AllDeadCheckBox
        ];

        AllColumnCheckBoxes =
        [
            AllPregameCheckBox,
            AllFirstNightCheckBox,
            AllFirstDayCheckBox,
            AllOtherNightCheckBox,
            AllOtherDayCheckBox,
        ];

        AllColumnTextBlocks =
        [
            PregameTextBlock,
            FirstNightTextBlock,
            FirstDayTextBlock,
            OtherNightTextBlock,
            OtherDayTextBlock
        ];

        for (int n = 0; n < 7; ++n)
        {
            int n1 = n;
            AllRowCheckBoxes[n1].IsCheckedChanged += (_, _) => AllRowCheckBoxesChanged(n1, AllRowCheckBoxes[n1]);
        }

        for (int n = 0; n < 5; ++n)
        {
            int n1 = n;
            AllColumnCheckBoxes[n1].IsCheckedChanged += (_, _) => AllColumnCheckBoxesChanged(n1, AllColumnCheckBoxes[n1]);
        }
    }

    /// <summary>
    /// Show or hide a specific column by index
    /// </summary>
    /// <param name="colNum">The column number</param>
    /// <param name="isShown">True if the column is shown</param>
    public void ShowHideColumn(int colNum, bool isShown)
    {
        for (int n = 0; n < 7; ++n)
        {
            if (n != 0 && !AllRowCheckBoxes[n].IsEnabled)
            {
                continue;
            }
            Checkboxes[n][colNum].IsEnabled = isShown;
            Checkboxes[n][colNum].IsVisible = isShown;
        }

        AllColumnCheckBoxes[colNum].IsEnabled = isShown;
        AllColumnCheckBoxes[colNum].IsVisible = isShown;
        AllColumnTextBlocks[colNum].IsVisible = isShown;

        MoreInfoGrid.ColumnDefinitions[colNum * 2 + 2].Width = isShown ? GridLength.Star : new GridLength(0, GridUnitType.Pixel);
        MoreInfoGrid.ColumnDefinitions[colNum * 2 + 2].MinWidth = isShown ? 70 : 0;

        UpdateBuffers();
    }

    private void UpdateBuffers()
    {
        GridLength zero = new(0, GridUnitType.Pixel);
        for (int n = 0; n + 2 < MoreInfoGrid.ColumnDefinitions.Count; n += 2)
        {
            if (MoreInfoGrid.ColumnDefinitions[n + 2].Width == zero)
            {
                MoreInfoGrid.ColumnDefinitions[n + 1].Width = zero;
            }
            else
            {
                MoreInfoGrid.ColumnDefinitions[n + 1].Width = new GridLength(10);
            }
        }

        for (int n = 0; n + 2 < MoreInfoGrid.RowDefinitions.Count; n += 2)
        {
            if (MoreInfoGrid.RowDefinitions[n].Height == zero)
            {
                MoreInfoGrid.RowDefinitions[n + 1].Height = zero;
            }
            else
            {
                MoreInfoGrid.RowDefinitions[n + 1].Height = new GridLength(10);
            }
        }
    }

    private void RemoveAllScope()
    {
        for (int rowNum = 1; rowNum < 7; ++rowNum)
        {
            for (int n = 0; n < 5; ++n)
            {
                Checkboxes[rowNum][n].IsEnabled = false;
                Checkboxes[rowNum][n].IsVisible = false;
            }

            AllRowCheckBoxes[rowNum].IsEnabled = false;
            AllRowCheckBoxes[rowNum].IsVisible = false;
        }

        for (int n = 0; n < 5; ++n)
        {
            AllColumnCheckBoxes[n].IsEnabled = false;
            AllColumnCheckBoxes[n].IsVisible = false;
        }

        AllRowCheckBoxes[0].IsEnabled = false;
        AllRowCheckBoxes[0].IsVisible = false;
        ScopeTextBlock.IsVisible = false;

        for (int n = 0; n < 5; ++n)
        {
            Grid.SetRow(Checkboxes[0][n], 2);
        }

        UpdateBuffers();
    }

    private void RestoreAllScope()
    {
        for (int n = 0; n < 5; ++n)
        {
            Grid.SetRow(Checkboxes[0][n], 4);
        }

        AllRowCheckBoxes[0].IsEnabled = true;
        AllRowCheckBoxes[0].IsVisible = true;
        ScopeTextBlock.IsVisible = true;

        for (int n = 0; n < 5; ++n)
        {
            if (!AllColumnTextBlocks[n].IsVisible)
            {
                continue;
            }
            AllColumnCheckBoxes[n].IsEnabled = true;
            AllColumnCheckBoxes[n].IsVisible = true;
        }

        for (int rowNum = 1; rowNum < 7; ++rowNum)
        {
            for (int n = 0; n < 5; ++n)
            {
                if (!AllColumnTextBlocks[n].IsVisible)
                {
                    continue;
                }
                Checkboxes[rowNum][n].IsEnabled = true;
                Checkboxes[rowNum][n].IsVisible = true;
            }

            AllRowCheckBoxes[rowNum].IsEnabled = true;
            AllRowCheckBoxes[rowNum].IsVisible = true;
        }
    }

    /// <summary>
    /// Update all grid values based on the current ScopeTimes
    /// </summary>
    /// <param name="gridData">The new time values</param>
    public void TryUpdateGrid(ScopeTimes gridData)
    {
        for (int rowNum = 0; rowNum < 7; ++rowNum)
        {
            for (int colNum = 0; colNum < 5; ++colNum)
            {
                if (!Checkboxes[rowNum][colNum].IsEnabled)
                {
                    continue;
                }

                Checkboxes[rowNum][colNum].IsChecked = GetIsChecked(gridData, rowNum, colNum);
            }
        }


        for (int n = 0; n < 7; ++n)
        {
            UpdateRow(n);
        }

        for (int n = 0; n < 5; ++n)
        {
            UpdateColumn(n);
        }
    }

    private static bool GetIsChecked(ScopeTimes gridData, int rowNum, int colNum)
    {
        GlobalEnum scope = (GlobalEnum)rowNum;
        TimeOfDay time = (TimeOfDay)(1 << colNum);

        return scope switch
        {
            GlobalEnum.None => (gridData.None & time) != 0,
            GlobalEnum.Townsfolk => (gridData.Townsfolk & time) != 0,
            GlobalEnum.Outsider => (gridData.Outsider & time) != 0,
            GlobalEnum.Minion => (gridData.Minion & time) != 0,
            GlobalEnum.Demon => (gridData.Demon & time) != 0,
            GlobalEnum.Traveller => (gridData.Traveller & time) != 0,
            GlobalEnum.Dead => (gridData.Dead & time) != 0,
            _ => throw new ArgumentOutOfRangeException(nameof(rowNum))
        };
    }

    /// <summary>
    /// Raised when a checkbox is toggled
    /// </summary>
    public event EventHandler<GridEventArgs>? CheckBoxChanged;

    private void AllColumnCheckBoxesChanged(int n1, CheckBox allColumnCheckBox)
    {
        if (allColumnCheckBox.IsChecked is null)
        {
            return;
        }
        SetColumn(n1, allColumnCheckBox.IsChecked.Value);
    }

    private void AllRowCheckBoxesChanged(int i, CheckBox allRowCheckBox)
    {
        if (allRowCheckBox.IsChecked is null)
        {
            return;
        }
        SetRow(i, allRowCheckBox.IsChecked.Value);
    }

    private void CheckBoxCheckedChanged(int rowNum, int colNum, CheckBox checkBox)
    {
        UpdateRow(rowNum);
        UpdateColumn(colNum);
        CheckBoxChanged?.Invoke(this, GetGridEventArgs(rowNum, colNum, checkBox.IsChecked is true));
    }

    private void SetRow(int rowNum, bool value)
    {
        for (int n = 0; n < 5; ++n)
        {
            if (Checkboxes[rowNum][n].IsEnabled)
            {
                Checkboxes[rowNum][n].IsChecked = value;
            }
        }
    }

    private void SetColumn(int colNum, bool value)
    {
        for (int n = 0; n < 7; ++n)
        {
            if (Checkboxes[n][colNum].IsEnabled)
            {
                Checkboxes[n][colNum].IsChecked = value;
            }
        }
    }

    private void UpdateRow(int rowNum)
    {
        int totalPossible = 5;
        int totalChecked = 0;
        for (int n = 0; n < 5; ++n)
        {
            if (!Checkboxes[rowNum][n].IsEnabled)
            {
                --totalPossible;
                continue;
            }
            if (Checkboxes[rowNum][n].IsChecked is true)
            {
                ++totalChecked;
            }
        }

        if (totalChecked == 0)
        {
            AllRowCheckBoxes[rowNum].IsChecked = false;
        }
        else if (totalChecked == totalPossible)
        {
            AllRowCheckBoxes[rowNum].IsChecked = true;
        }
        else
        {
            AllRowCheckBoxes[rowNum].IsChecked = null;
        }
    }

    private void UpdateColumn(int columnNumber)
    {
        int totalPossible = 7;
        int totalChecked = 0;
        for (int n = 0; n < 7; ++n)
        {
            if (!Checkboxes[n][columnNumber].IsEnabled)
            {
                --totalPossible;
                continue;
            }
            if (Checkboxes[n][columnNumber].IsChecked is true)
            {
                ++totalChecked;
            }
        }

        if (totalChecked == 0)
        {
            AllColumnCheckBoxes[columnNumber].IsChecked = false;
        }
        else if (totalChecked == totalPossible)
        {
            AllColumnCheckBoxes[columnNumber].IsChecked = true;
        }
        else
        {
            AllColumnCheckBoxes[columnNumber].IsChecked = null;
        }
    }

    /// <summary>
    /// Data encapsulating the grid changing state
    /// </summary>
    public class GridEventArgs : EventArgs
    {
        /// <summary>
        /// The toggled scope
        /// </summary>
        public required GlobalEnum Scope { get; init; }
        /// <summary>
        /// The time changed
        /// </summary>
        public required TimeOfDay FlagChanged { get; init; }
        /// <summary>
        /// True if the new time is now set
        /// </summary>
        public required bool IsSet { get; init; }
    }

    private static GridEventArgs GetGridEventArgs(int rowNum, int colNum, bool isSet) =>
        new()
        {
            IsSet = isSet,
            Scope = (GlobalEnum)rowNum,
            FlagChanged = (TimeOfDay)(1 << colNum)
        };

    /// <summary>
    /// Load the current ScopeTimes
    /// </summary>
    /// <param name="times">The current data</param>
    public void Load(ScopeTimes times)
    {
        if (times.IsAlternateScopesEnabled)
        {
            RestoreAllScope();
        }
        else
        {
            RemoveAllScope();
        }
        TryUpdateGrid(times);
    }
}