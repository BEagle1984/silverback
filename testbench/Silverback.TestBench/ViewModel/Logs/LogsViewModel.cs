// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Silverback.TestBench.ViewModel.Containers;
using Silverback.TestBench.ViewModel.Framework;

namespace Silverback.TestBench.ViewModel.Logs;

public class LogsViewModel : ViewModelBase
{
    private readonly ConcurrentBag<LogEntry> _entries = [];

    private LogEntry? _selectedEntry;

    public IEnumerable<LogEntry> Entries => _entries.OrderBy(entry => entry.Timestamp);

    public LogEntry? SelectedEntry
    {
        get => _selectedEntry;
        set => SetProperty(ref _selectedEntry, value, nameof(SelectedEntry));
    }

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
}
