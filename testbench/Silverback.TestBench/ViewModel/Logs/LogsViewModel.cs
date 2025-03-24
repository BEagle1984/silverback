// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Silverback.TestBench.ViewModel.Containers;
using Silverback.TestBench.ViewModel.Framework;

namespace Silverback.TestBench.ViewModel.Logs;

public class LogsViewModel : ViewModelBase
{
    private readonly ConcurrentBag<LogEntry> _entries = [];

    private LogLevelFilter _levelFilter = LogLevelFilter.Any;

    private string _containerFilter = "Any";

    private string? _textFilter;

    private LogEntry? _selectedEntry;

    public static IEnumerable<LogLevelFilter> LevelFilterValues { get; } = Enum.GetValues<LogLevelFilter>();

    public ObservableCollection<string> ContainerFilterValues { get; } = ["Any", "Producer"];

    public IEnumerable<LogEntry> Entries => _entries
        .Where(
            entry => ApplyLevelFilter(entry.Level) &&
                     (ContainerFilter == "Any" ||
                      entry.Container?.ContainerService.Name == ContainerFilter ||
                      ContainerFilter == "Producer" && entry.Container is null) &&
                     (string.IsNullOrEmpty(TextFilter) || entry.Message.Contains(TextFilter, StringComparison.Ordinal)))
        .Take(200)
        .OrderBy(entry => entry.Timestamp);

    public LogEntry? SelectedEntry
    {
        get => _selectedEntry;
        set => SetProperty(ref _selectedEntry, value, nameof(SelectedEntry));
    }

    public LogLevelFilter LevelFilter
    {
        get => _levelFilter;
        set
        {
            SetProperty(ref _levelFilter, value, nameof(LevelFilter));
            NotifyPropertyChanged(nameof(Entries));
        }
    }

    public string ContainerFilter
    {
        get => _containerFilter;
        set
        {
            SetProperty(ref _containerFilter, value, nameof(ContainerFilter));
            NotifyPropertyChanged(nameof(Entries));
        }
    }

    public string? TextFilter
    {
        get => _textFilter;
        set
        {
            SetProperty(ref _textFilter, value, nameof(TextFilter));
            NotifyPropertyChanged(nameof(Entries));
        }
    }

    public LogEntry AddInformation(DateTime timestamp, string message, ContainerInstanceViewModel? container) =>
        AddEntry(new LogEntry(timestamp, message, container));

    public LogEntry AddWarning(DateTime timestamp, string message, ContainerInstanceViewModel? container) =>
        AddEntry(new LogEntry(timestamp, message, container, LogLevel.Warning));

    public LogEntry AddError(DateTime timestamp, string message, ContainerInstanceViewModel? container) =>
        AddEntry(new LogEntry(timestamp, message, container, LogLevel.Error));

    public LogEntry AddFatal(DateTime timestamp, string message, ContainerInstanceViewModel? container) =>
        AddEntry(new LogEntry(timestamp, message, container, LogLevel.Fatal));

    public LogEntry AddEntry(LogEntry entry)
    {
        _entries.Add(entry);
        NotifyPropertyChanged(nameof(Entries));
        return entry;
    }

    private bool ApplyLevelFilter(LogLevel level) => LevelFilter switch
    {
        LogLevelFilter.Information => level == LogLevel.Information,
        LogLevelFilter.Warning => level == LogLevel.Warning,
        LogLevelFilter.Error => level == LogLevel.Error,
        LogLevelFilter.Fatal => level == LogLevel.Fatal,
        _ => true
    };
}
