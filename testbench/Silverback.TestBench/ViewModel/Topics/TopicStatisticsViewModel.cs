// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using Silverback.TestBench.ViewModel.Framework;

namespace Silverback.TestBench.ViewModel.Topics;

public class TopicStatisticsViewModel : ViewModelBase
{
    private int _producedCount;

    private int _produceErrorsCount;

    private int _consumedCount;

    private int _processedCount;

    private int _skippedCount;

    private int _lostCount;

    public int ProducedCount => _producedCount;

    public int ProduceErrorsCount => _produceErrorsCount;

    public int ConsumedCount => _consumedCount;

    public int ProcessedCount => _processedCount;

    public int SkippedCount => _skippedCount;

    public int LostCount => _lostCount;

    public int LagCount { get; private set; }

    public void IncrementProducedCount()
    {
        Interlocked.Increment(ref _producedCount);
        NotifyPropertyChanged(nameof(ProducedCount));
        UpdateLagCount();
    }

    public void IncrementProduceErrorsCount()
    {
        Interlocked.Increment(ref _produceErrorsCount);
        NotifyPropertyChanged(nameof(ProduceErrorsCount));
    }

    public void IncrementConsumedCount()
    {
        Interlocked.Increment(ref _consumedCount);
        NotifyPropertyChanged(nameof(ConsumedCount));
    }

    public void IncrementProcessedCount()
    {
        Interlocked.Increment(ref _processedCount);
        NotifyPropertyChanged(nameof(ProcessedCount));
        UpdateLagCount();
    }

    public void IncrementSkippedCount()
    {
        Interlocked.Increment(ref _skippedCount);
        NotifyPropertyChanged(nameof(SkippedCount));
    }

    public void IncrementLostCount()
    {
        Interlocked.Increment(ref _lostCount);
        NotifyPropertyChanged(nameof(LostCount));
        UpdateLagCount();
    }

    private void UpdateLagCount()
    {
        LagCount = Volatile.Read(ref _producedCount) - Volatile.Read(ref _processedCount) - Volatile.Read(ref _lostCount);
        NotifyPropertyChanged(nameof(LagCount));
    }
}
