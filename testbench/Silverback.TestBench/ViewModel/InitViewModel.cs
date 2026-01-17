// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows.Input;
using Silverback.TestBench.Containers;
using Silverback.TestBench.Producer;
using Silverback.TestBench.Utils;
using Silverback.TestBench.ViewModel.Framework;

namespace Silverback.TestBench.ViewModel;

[SuppressMessage("Performance", "CA1823:Avoid unused private fields", Justification = "Used by the XAML")]
[SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Modified by SetValue")]
public class InitViewModel : ViewModelBase
{
    private readonly FileSystemHelper _fileSystemHelper;

    private readonly DockerImagesBuilder _dockerImagesBuilder;

    private readonly KafkaTopicsCreator _kafkaTopicsCreator;

    private readonly ContainersOrchestrator _containersOrchestrator;

    private readonly ExceptionHandler _exceptionHandler;

    private readonly App _silverbackTestBenchApp;

    public InitViewModel(
        FileSystemHelper fileSystemHelper,
        DockerImagesBuilder dockerImagesBuilder,
        KafkaTopicsCreator kafkaTopicsCreator,
        ContainersOrchestrator containersOrchestrator,
        ExceptionHandler exceptionHandler,
        App silverbackTestBenchApp)
    {
        _fileSystemHelper = fileSystemHelper;
        _dockerImagesBuilder = dockerImagesBuilder;
        _kafkaTopicsCreator = kafkaTopicsCreator;
        _containersOrchestrator = containersOrchestrator;
        _exceptionHandler = exceptionHandler;
        _silverbackTestBenchApp = silverbackTestBenchApp;

        StartCommand = new AsyncRelayCommand(OnStartAsync, () => !IsInitializing);
    }

    public event EventHandler? InitializationCompleted;

    public ICommand StartCommand { get; }

    public bool RebuildDockerImages
    {
        get;
        set => SetProperty(ref field, value, nameof(RebuildDockerImages));
    }

    public bool RecreateAllTopics
    {
        get;
        set => SetProperty(ref field, value, nameof(RecreateAllTopics));
    } = true;

    public bool ClearLogs
    {
        get;
        set => SetProperty(ref field, value, nameof(ClearLogs));
    }

    public bool IsInitializing
    {
        get;
        set => SetProperty(ref field, value, nameof(IsInitializing));
    }

    public double Progress
    {
        get;
        set => SetProperty(ref field, value, nameof(Progress));
    }

    public string? ProgressMessage
    {
        get;
        set => SetProperty(ref field, value, nameof(ProgressMessage));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching all is intended")]
    private async Task OnStartAsync()
    {
        IsInitializing = true;
        Progress = 0.1;

        try
        {
            await ExecuteStepAsync(
                () => Task.Run(() => _dockerImagesBuilder.BuildAll()),
                RebuildDockerImages,
                "Rebuilding Docker images",
                0.3);

            await ExecuteStepAsync(
                _kafkaTopicsCreator.RecreateAllTopicsAsync,
                RecreateAllTopics,
                "Recreating all topics",
                0.2);

            await ExecuteStepAsync(
                () => Task.Run(() => _fileSystemHelper.ClearLogsFolder()),
                ClearLogs,
                "Clearing logs",
                0.1);

            await ExecuteStepAsync(
                () => Task.Run(() => _containersOrchestrator.InitDefaultInstances()),
                true,
                "Starting containers",
                0.2);

            await ExecuteStepAsync(
                _silverbackTestBenchApp.StartHostAsync,
                true,
                "Starting Silverback Test Bench (Host)",
                0.2);

            ProgressMessage = "Done!";

            InitializationCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _exceptionHandler.HandleException(ex);
            ProgressMessage = "An error occurred!";
        }
        finally
        {
            IsInitializing = false;
        }
    }

    private async Task ExecuteStepAsync(Func<Task> initStep, bool enabled, string progressMessage, double progressIncrement)
    {
        if (enabled)
        {
            ProgressMessage = progressMessage;
            await initStep();
        }

        Progress += progressIncrement;
    }
}
