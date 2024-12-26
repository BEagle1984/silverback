// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Silverback.TestBench.Utils;

public class FileSystemHelper
{
    private readonly ILogger<FileSystemHelper> _logger;

    public FileSystemHelper(ILogger<FileSystemHelper> logger)
    {
        _logger = logger;
    }

    public static string GitRepositoryRoot { get; } = FindGitRoot(AppContext.BaseDirectory) ?? throw new InvalidOperationException("Git root not found");

    public static string SolutionRoot { get; } = Path.Combine(GitRepositoryRoot, "testbench");

    public static string LogsBaseFolder { get; } = Path.Combine(AppContext.BaseDirectory, "logs");

    public static string LogsFolder { get; } = Path.Combine(LogsBaseFolder, $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}");

    public void ClearLogsFolder() => ClearFolder(LogsBaseFolder);

    private static string? FindGitRoot(string currentPath)
    {
        if (Directory.Exists(Path.Combine(currentPath, ".git")))
            return currentPath;

        string? parentPath = Directory.GetParent(currentPath)?.FullName;
        return string.IsNullOrEmpty(parentPath) ? null : FindGitRoot(parentPath);
    }

    private void ClearFolder(string folderPath)
    {
        _logger.LogInformation("Clearing logs folder");
        Stopwatch stopwatch = Stopwatch.StartNew();

        DirectoryInfo directory = new(folderPath);

        foreach (FileInfo file in directory.GetFiles())
        {
            file.Delete();
        }

        foreach (DirectoryInfo subdirectory in directory.GetDirectories())
        {
            if (subdirectory.Name == Path.GetFileName(LogsFolder))
                continue;

            subdirectory.Delete(true);
        }

        _logger.LogInformation("Cleared logs folder in {Elapsed}", stopwatch.Elapsed);
    }
}
