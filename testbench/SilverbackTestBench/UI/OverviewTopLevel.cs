// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Ductus.FluentDocker.Services;
using Silverback.TestBench.Configuration;
using Silverback.TestBench.Containers;
using Silverback.TestBench.Containers.Models;
using Silverback.TestBench.Producer;
using Silverback.TestBench.Producer.Models;
using Silverback.TestBench.UI.CustomControls;
using Silverback.TestBench.Utils;
using Terminal.Gui;

namespace Silverback.TestBench.UI;

public sealed class OverviewTopLevel : Toplevel
{
    private readonly ContainersOrchestrator _containersOrchestrator;

    private readonly ContainersRandomScaling _randomScaling;

    private readonly MessagesTracker _messagesTracker;

    private readonly ProducerBackgroundService _producerBackgroundService;

    private readonly DataTable _messagesStatsDataTable = new();

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed by Terminal.Gui")]
    private readonly CustomTableView _messagesStatsTableView;

    private readonly DataTable _containersDataTable = new();

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed by Terminal.Gui")]
    private readonly CustomTableView _containersTableView;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed by Terminal.Gui")]
    private readonly StatusItem _toggleProducingStatusItem;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed by Terminal.Gui")]
    private readonly StatusItem _toggleScalingStatusItem;

    private Timer? _refreshTimer;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by Terminal.Gui")]
    public OverviewTopLevel(
        ContainersOrchestrator containersOrchestrator,
        ContainersRandomScaling randomScaling,
        MessagesTracker messagesTracker,
        ProducerBackgroundService producerBackgroundService)
    {
        _containersOrchestrator = containersOrchestrator;
        _randomScaling = randomScaling;
        _messagesTracker = messagesTracker;
        _producerBackgroundService = producerBackgroundService;

        int messagesStatsTableHeight = TopicsConfiguration.All.Count + 4;
        _messagesStatsTableView = new CustomTableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = messagesStatsTableHeight,

            ColorScheme = TableStyles.DefaultColorScheme,

            FullRowSelect = true,
            CanSelect = false
        };

        _containersTableView = new CustomTableView
        {
            X = 0,
            Y = messagesStatsTableHeight + 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),

            ColorScheme = TableStyles.DefaultColorScheme,

            FullRowSelect = true,
            CanSelect = false
        };

        _toggleProducingStatusItem = new StatusItem(Key.F2, "~F2~ Producing ~OFF~", ToggleProducing);
        _toggleScalingStatusItem = new StatusItem(Key.F3, "~F3~ Autoscaling ~OFF~", ToggleRandomScaling);
        StatusBar statusBar = new(
        [
            _toggleProducingStatusItem,
                _toggleScalingStatusItem,
                new StatusItem(Key.F4, "~F4~ Scale out", ScaleOut),
                new StatusItem(Key.F5, "~F5~ Scale in", ScaleIn),
                new StatusItem(Key.F7, "~F7~ Show logs", ShowLogs),
                new StatusItem(Key.CtrlMask | Key.Q, "~Ctrl+Q~ Quit", () => Application.RequestStop())
        ])
        {
            Visible = true
        };

        Add(_messagesStatsTableView, _containersTableView, statusBar);
    }

    public override void OnLoaded()
    {
        _messagesStatsTableView.Table = _messagesStatsDataTable;
        _containersTableView.Table = _containersDataTable;

        InitMessagesStatsTableColumns();
        InitContainersTableColumns();

        _refreshTimer = new Timer(_ => RefreshData(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        base.OnLoaded();
    }

    protected override void Dispose(bool disposing)
    {
        _messagesStatsDataTable.Dispose();
        _containersDataTable.Dispose();
        _refreshTimer?.Dispose();

        base.Dispose(disposing);
    }

    private void InitMessagesStatsTableColumns()
    {
        DataColumn topicColumn = _messagesStatsDataTable.Columns.Add("Topic");
        DataColumn producedCountColumn = _messagesStatsDataTable.Columns.Add("Produced", typeof(int));
        DataColumn failedProduceCountColumn = _messagesStatsDataTable.Columns.Add("Produce Errors", typeof(int));
        DataColumn consumedCountColumn = _messagesStatsDataTable.Columns.Add("Consumed", typeof(int));
        DataColumn processedCountColumn = _messagesStatsDataTable.Columns.Add("Processed", typeof(int));
        DataColumn skippedCountColumn = _messagesStatsDataTable.Columns.Add("Skipped", typeof(int));
        DataColumn lagCountColumn = _messagesStatsDataTable.Columns.Add("Lag", typeof(int));
        DataColumn lostCountColumn = _messagesStatsDataTable.Columns.Add("Lost", typeof(int));

        _messagesStatsDataTable.PrimaryKey = [topicColumn];

        _messagesStatsTableView.Style.ColumnStyles.Add(producedCountColumn, TableStyles.NumberColumnStyle);
        _messagesStatsTableView.Style.ColumnStyles.Add(failedProduceCountColumn, TableStyles.ErrorsColumnStyle);
        _messagesStatsTableView.Style.ColumnStyles.Add(consumedCountColumn, TableStyles.NumberColumnStyle);
        _messagesStatsTableView.Style.ColumnStyles.Add(processedCountColumn, TableStyles.NumberColumnStyle);
        _messagesStatsTableView.Style.ColumnStyles.Add(skippedCountColumn, TableStyles.NumberColumnStyle);
        _messagesStatsTableView.Style.ColumnStyles.Add(lagCountColumn, TableStyles.GetScaleColoringColumnStyle());
        _messagesStatsTableView.Style.ColumnStyles.Add(lostCountColumn, TableStyles.ErrorsColumnStyle);
    }

    private void InitContainersTableColumns()
    {
        DataColumn containerColumn = _containersDataTable.Columns.Add("Container");
        DataColumn statusColumn = _containersDataTable.Columns.Add("Status");
        DataColumn startedColumn = _containersDataTable.Columns.Add("Started", typeof(DateTime));
        DataColumn stoppedColumn = _containersDataTable.Columns.Add("Stopped", typeof(DateTime));
        DataColumn consumedColumn = _containersDataTable.Columns.Add("Consumed", typeof(int));
        DataColumn processedColumn = _containersDataTable.Columns.Add("Processed", typeof(int));
        DataColumn errorsColumn = _containersDataTable.Columns.Add("Errors", typeof(int));
        DataColumn fatalErrorsColumn = _containersDataTable.Columns.Add("Fatal Errors", typeof(int));
        DataColumn warningsColumn = _containersDataTable.Columns.Add("Warnings", typeof(int));

        _containersDataTable.PrimaryKey = [containerColumn];

        CustomTableView.ColumnStyle statusColumnStyle = new()
        {
            Alignment = TextAlignment.Centered,
            ColorGetter = args => (string?)args.CellValue switch
            {
                nameof(ServiceRunningState.Starting) => TableStyles.GreenCellColorScheme,
                nameof(ServiceRunningState.Running) => TableStyles.GreenCellColorScheme,
                nameof(ServiceRunningState.Stopping) => TableStyles.BrownCellColorScheme,
                nameof(ServiceRunningState.Stopped) => TableStyles.BrownCellColorScheme,
                nameof(ServiceRunningState.Removing) => TableStyles.BrownCellColorScheme,
                nameof(ServiceRunningState.Removed) => TableStyles.BrownCellColorScheme,
                _ => Colors.TopLevel
            }
        };

        _containersTableView.Style.ColumnStyles.Add(statusColumn, statusColumnStyle);
        _containersTableView.Style.ColumnStyles.Add(startedColumn, TableStyles.TimeColumnStyle);
        _containersTableView.Style.ColumnStyles.Add(stoppedColumn, TableStyles.TimeColumnStyle);
        _containersTableView.Style.ColumnStyles.Add(consumedColumn, TableStyles.NumberColumnStyle);
        _containersTableView.Style.ColumnStyles.Add(processedColumn, TableStyles.NumberColumnStyle);
        _containersTableView.Style.ColumnStyles.Add(errorsColumn, TableStyles.ErrorsColumnStyle);
        _containersTableView.Style.ColumnStyles.Add(fatalErrorsColumn, TableStyles.ErrorsColumnStyle);
        _containersTableView.Style.ColumnStyles.Add(warningsColumn, TableStyles.ErrorsColumnStyle);
    }

    private void RefreshData()
    {
        UpdateMessagesStatsTableView();
        UpdateContainersTableView();

        // Clear selection to preserve cells colors
        _messagesStatsTableView.SetSelection(-1, -1, false);
        _containersTableView.SetSelection(-1, -1, false);

        Application.DoEvents();
    }

    private void UpdateMessagesStatsTableView()
    {
        _messagesStatsDataTable.Rows.Clear();

        AddMessagesStatsTableRow(_messagesTracker.GlobalStats);

        foreach (MessagesStats stats in _messagesTracker.StatsByTopic.Values)
        {
            AddMessagesStatsTableRow(stats);
        }
    }

    private void AddMessagesStatsTableRow(MessagesStats stats) =>
        _messagesStatsDataTable.Rows.Add(
            stats.TopicConfiguration?.ToString() ?? "global",
            stats.ProducedCount,
            stats.FailedProduceCount,
            stats.ConsumedCount,
            stats.ProcessedCount,
            stats.SkippedCount,
            stats.LagCount,
            stats.LostCount);

    private void UpdateContainersTableView()
    {
        _containersDataTable.Rows.Clear();

        foreach (ContainerStats stats in _containersOrchestrator.GetContainerStats())
        {
            _containersDataTable.Rows.Add(
                stats.ContainerService.Name,
                stats.ContainerService.State,
                stats.Started,
                stats.Stopped,
                stats.ConsumedMessagesCount,
                stats.ProcessedMessagesCount,
                stats.ErrorsCount,
                stats.FatalErrorsCount,
                stats.WarningsCount);
        }

        _containersTableView.Update();
    }

    private void ToggleProducing()
    {
        if (_producerBackgroundService.IsEnabled)
        {
            _producerBackgroundService.Disable();
            _toggleProducingStatusItem.Title = "~F2~ Producing ~OFF~";
        }
        else
        {
            _producerBackgroundService.Enable();
            _toggleProducingStatusItem.Title = "~F2~ Producing ~ON~";
        }
    }

    private void ToggleRandomScaling()
    {
        if (_randomScaling.IsEnabled)
        {
            _randomScaling.Disable();
            _toggleScalingStatusItem.Title = "~F3~ Autoscaling ~OFF~";
        }
        else
        {
            _randomScaling.Enable();
            _toggleScalingStatusItem.Title = "~F3~ Autoscaling ~ON~";
        }
    }

    private void ScaleOut() => _containersOrchestrator.ScaleOut(DockerContainers.Consumer);

    private void ScaleIn() => _containersOrchestrator.ScaleIn(DockerContainers.Consumer);

    private void ShowLogs()
    {
        string? containerName = _containersDataTable.Rows[_containersTableView.SelectedRow].Field<string>(0);

        if (!string.IsNullOrEmpty(containerName))
            ProcessHelper.Start("code", FileSystemHelper.LogsFolder);
    }
}
