// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Silverback.Examples.Common
{
    public class JobScheduler : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobScheduler> _logger;
        private readonly Dictionary<string, Timer> _timers = new Dictionary<string, Timer>();
        private readonly Dictionary<string, bool> _isRunning = new Dictionary<string, bool>();

        public JobScheduler(IServiceProvider serviceProvider, ILogger<JobScheduler> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void AddJob(string name, TimeSpan interval, Action<IServiceProvider> job)
        {
            _isRunning.Add(name, false);

            _timers.Add(name,
                new Timer(_ => RunJob(name, job, _serviceProvider.CreateScope().ServiceProvider), null, TimeSpan.Zero,
                    interval));
        }

        private void RunJob(string name, Action<IServiceProvider> job, IServiceProvider serviceProvider)
        {
            if (IsRunning(name))
                return;

            _logger.LogInformation($"Running job '{name}'...");
            try
            {
                job(serviceProvider);
                _logger.LogInformation($"Job '{name}' was successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Job '{name}' failed.");
            }
            finally
            {
                _isRunning[name] = false;
            }
        }

        private bool IsRunning(string name)
        {
            lock (_isRunning)
            {
                if (_isRunning[name])
                    return true;

                _isRunning[name] = true;
            }
            return false;
        }

        public void Dispose()
        {
            if (_timers == null)
                return;

            foreach (var timer in _timers.Values)
            {
                timer?.Dispose();
            }
        }
    }
}