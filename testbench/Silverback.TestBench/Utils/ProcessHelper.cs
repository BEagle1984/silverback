// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;

namespace Silverback.TestBench.Utils;

public static class ProcessHelper
{
    public static void Start(string fileName, string args)
    {
        using Process process = new();
        process.StartInfo.UseShellExecute = true;
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = args;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
    }
}
