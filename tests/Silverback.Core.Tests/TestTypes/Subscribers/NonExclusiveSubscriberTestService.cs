﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.Core.TestTypes.Subscribers;

public class NonExclusiveSubscriberTestService
{
    public ParallelTestingUtil Parallel { get; } = new();

    [Subscribe(Exclusive = false)]
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "IDE0051", Justification = Justifications.CalledBySilverback)]
    private void OnMessageReceived(object message) => Parallel.DoWork();

    [Subscribe(Exclusive = false)]
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "IDE0051", Justification = Justifications.CalledBySilverback)]
    private Task OnMessageReceivedAsync(object message) => Parallel.DoWorkAsync();
}
