// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Silverback.Messaging.Messages;
using Silverback.Tools.Generators.Docs.Headers;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: Silverback.Tools.Generators.Docs.Headers <output directory>");
    return 1;
}

string outputDirectory = args[0];
Directory.CreateDirectory(outputDirectory);

DocsGenerator.GenerateDocsTable(typeof(DefaultMessageHeaders), Path.Combine(outputDirectory, "headers-default.md"));
DocsGenerator.GenerateDocsTable(typeof(KafkaMessageHeaders), Path.Combine(outputDirectory, "headers-kafka.md"));
DocsGenerator.GenerateDocsTable(typeof(MqttMessageHeaders), Path.Combine(outputDirectory, "headers-mqtt.md"));

return 0;
