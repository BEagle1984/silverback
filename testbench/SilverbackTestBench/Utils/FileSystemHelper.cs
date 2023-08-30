// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.IO;
using Silverback.TestBench.UI;

namespace Silverback.TestBench.Utils;

public static class FileSystemHelper
{
    public static string GitRepositoryRoot { get; } = FindGitRoot(AppContext.BaseDirectory) ?? throw new InvalidOperationException("Git root not found");

    public static string SolutionRoot { get; } = Path.Combine(GitRepositoryRoot, "testbench");

    public static string LogsBaseFolder { get; } = Path.Combine(AppContext.BaseDirectory, "logs");

    public static string LogsFolder { get; } = Path.Combine(LogsBaseFolder, $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}");

    public static void ClearLogsFolder() => ClearFolder(LogsBaseFolder);

    public static void ClearFolder(string folderPath)
    {
        Console.Write("Clearing logs folder...");
        Stopwatch stopwatch = Stopwatch.StartNew();

        DirectoryInfo directory = new(folderPath);

        foreach (FileInfo file in directory.GetFiles())
        {
            file.Delete();
        }

        foreach (DirectoryInfo subdirectory in directory.GetDirectories())
        {
            subdirectory.Delete(true);
        }

        ConsoleHelper.WriteDone(stopwatch.Elapsed);
    }

    private static string? FindGitRoot(string currentPath)
    {
        if (Directory.Exists(Path.Combine(currentPath, ".git")))
            return currentPath;

        string? parentPath = Directory.GetParent(currentPath)?.FullName;
        return string.IsNullOrEmpty(parentPath) ? null : FindGitRoot(parentPath);
    }
}
