// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace Silverback.TestBench.Utils;

public class ExceptionHandler
{
    private readonly ILogger<ExceptionHandler> _logger;

    public ExceptionHandler(ILogger<ExceptionHandler> logger)
    {
        _logger = logger;
    }

    public void HandleException(Exception exception)
    {
        _logger.LogError(exception, "An error occurred");

        MessageBox.Show(exception.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
