﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    public class ValueTaskFactoryTests
    {
        [Fact]
        public void CompletedTask_DefaultReturned()
        {
            ValueTaskFactory.CompletedTask.Should().BeEquivalentTo(default(ValueTask));
        }

        [Fact]
        [SuppressMessage("", "CA2012", Justification = "Test method")]
        [SuppressMessage("", "VSTHRD104", Justification = "Test method")]
        public async Task FromResult_IntResult_SuccessfulTaskCreated()
        {
            var result = ValueTaskFactory.FromResult(42);

            result.IsCompleted.Should().BeTrue();
            result.IsCompletedSuccessfully.Should().BeTrue();
            (await result).Should().Be(42);
        }

        [Fact]
        public void FromCanceled_WithoutResult_CanceledTaskCreated()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var result = ValueTaskFactory.FromCanceled(cancellationTokenSource.Token);

            result.IsCompleted.Should().BeTrue();
            result.IsCanceled.Should().BeTrue();
        }

        [Fact]
        public void FromCanceled_WithIntResult_CanceledTaskCreated()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var result = ValueTaskFactory.FromCanceled<int>(cancellationTokenSource.Token);

            result.IsCompleted.Should().BeTrue();
            result.IsCanceled.Should().BeTrue();
        }

        [Fact]
        public void FromException_WithoutResult_FaultedTaskCreated()
        {
            var result = ValueTaskFactory.FromException(new ArgumentNullException());

            result.IsCompleted.Should().BeTrue();
            result.IsFaulted.Should().BeTrue();
        }

        [Fact]
        public void FromException_WithIntResult_FaultedTaskCreated()
        {
            var result = ValueTaskFactory.FromException<int>(new ArgumentNullException());

            result.IsCompleted.Should().BeTrue();
            result.IsFaulted.Should().BeTrue();
        }
    }
}
