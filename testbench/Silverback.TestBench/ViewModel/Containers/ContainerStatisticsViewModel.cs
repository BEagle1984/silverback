// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using Silverback.TestBench.ViewModel.Framework;

namespace Silverback.TestBench.ViewModel.Containers;

public class ContainerStatisticsViewModel : ViewModelBase
{
    private int _consumedCount;

    private int _processedCount;

    private int _errorsCount;

    private int _warningsCount;

    private int _fatalErrorsCount;

    public int ConsumedCount => _consumedCount;

    public int ProcessedCount => _processedCount;

    public int ErrorsCount => _errorsCount;

    public int WarningsCount => _warningsCount;

    public int FatalErrorsCount => _fatalErrorsCount;

    public void IncrementConsumedMessagesCount()
    {
        Interlocked.Increment(ref _consumedCount);
        NotifyPropertyChanged(nameof(ConsumedCount));
    }

    public void IncrementProcessedMessagesCount()
    {
        Interlocked.Increment(ref _processedCount);
        NotifyPropertyChanged(nameof(ProcessedCount));
    }

    public void IncrementErrorsCount()
    {
        Interlocked.Increment(ref _errorsCount);
        NotifyPropertyChanged(nameof(ErrorsCount));
    }

    public void IncrementWarningsCount()
    {
        Interlocked.Increment(ref _warningsCount);
        NotifyPropertyChanged(nameof(WarningsCount));
    }

    public void IncrementFatalErrorsCount()
    {
        Interlocked.Increment(ref _fatalErrorsCount);
        NotifyPropertyChanged(nameof(FatalErrorsCount));
    }
}
