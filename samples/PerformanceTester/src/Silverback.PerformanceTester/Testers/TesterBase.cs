// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Subscribers;
using Silverback.PerformanceTester.Subscribers;

namespace Silverback.PerformanceTester.Testers
{
    internal abstract class TesterBase
    {
        private readonly List<TimeSpan> _elaspsedTimes = new List<TimeSpan>();
        private readonly int _iterations;
        private readonly IServiceProvider _baseServiceProvider;

        protected TesterBase(int iterations)
        {
            _iterations = iterations;
            var services = new ServiceCollection()
                .AddLogging()
                .AddBus()
                .AddMessageObservable()
                .AddSingleton<ISubscriber, Subscriber>(s => s.GetRequiredService<Subscriber>())
                .AddSingleton<Subscriber>()
                .AddSingleton<RxSubscriber>();

            _baseServiceProvider = services.BuildServiceProvider();
        }

        protected IServiceProvider ServiceProvider;

        public void Test()
        {
            Console.Clear();

            Console.WriteLine($"{GetType().Name} starting... (iterations={_iterations})");

            using (var scope = _baseServiceProvider.CreateScope())
            {
                ServiceProvider = scope.ServiceProvider;

                Setup();

                for (int i = 1; i <= _iterations; i++)
                    PerformAndMeasureIteration(i);

                ShowReport();
            }

            Console.WriteLine("\r\nPress any key to continue...");
            Console.Read();
        }

        protected virtual void Setup()
        {
        }

        private void PerformAndMeasureIteration(int i)
        {
            Console.Write($"{i}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            PerformIteration();

            stopwatch.Stop();

            _elaspsedTimes.Add(stopwatch.Elapsed);

            Console.WriteLine("DONE");
        }

        protected abstract void PerformIteration();

        private void ShowReport()
        {
            Console.WriteLine("-----------");
            Console.WriteLine($"Iterations: {_iterations}");
            Console.WriteLine($"Received Messages: {GetReceivedMessagesCount()}");

            var elapsedMilliseconds = _elaspsedTimes.Select<TimeSpan, double>(t => t.TotalMilliseconds).ToList();
            Console.WriteLine($"Total elapsed: {FormatTime(elapsedMilliseconds.Sum())}");
            Console.WriteLine($"Average elapsed: {FormatTime(elapsedMilliseconds.Average())}");
            Console.WriteLine($"Max elapsed: {FormatTime(elapsedMilliseconds.Max())}");
        }

        protected abstract int GetReceivedMessagesCount();

        private string FormatTime(double milliseconds) => TimeSpan.FromMilliseconds(milliseconds).ToString("g");
    }
}
