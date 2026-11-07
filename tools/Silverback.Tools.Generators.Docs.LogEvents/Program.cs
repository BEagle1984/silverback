// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Silverback.Diagnostics;
using Silverback.Tools.Generators.Docs.LogEvents;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: Silverback.Tools.Generators.Docs.LogEvents <output directory>");
    return 1;
}

string outputDirectory = args[0];
Directory.CreateDirectory(outputDirectory);

DocsGenerator.GenerateDocsTable(typeof(CoreLogEvents), Path.Combine(outputDirectory, "log-events-core.md"));
DocsGenerator.GenerateDocsTable(typeof(IntegrationLogEvents), Path.Combine(outputDirectory, "log-events-integration.md"));
DocsGenerator.GenerateDocsTable(typeof(KafkaLogEvents), Path.Combine(outputDirectory, "log-events-kafka.md"));
DocsGenerator.GenerateDocsTable(typeof(MqttLogEvents), Path.Combine(outputDirectory, "log-events-mqtt.md"));
DocsGenerator.GenerateDocsTable(typeof(StorageMemoryLogEvents), Path.Combine(outputDirectory, "log-events-storage-memory.md"));

return 0;
