// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.TestBench.ViewModel.Topics;

public class OverallTopicViewModel : TopicViewModel
{
    public OverallTopicViewModel()
        : base("*", TimeSpan.Zero, 0, true)
    {
    }
}
