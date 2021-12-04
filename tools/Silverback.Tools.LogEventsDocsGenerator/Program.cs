// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Diagnostics;

namespace Silverback.Tools.LogEventsDocsGenerator;

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("### Core");
        Console.WriteLine();
        DocsGenerator.GenerateDocsTable(typeof(CoreLogEvents));
        Console.WriteLine();
        Console.WriteLine("### Integration");
        Console.WriteLine();
        DocsGenerator.GenerateDocsTable(typeof(IntegrationLogEvents));
        Console.WriteLine();
        Console.WriteLine("### Kafka");
        Console.WriteLine();
        DocsGenerator.GenerateDocsTable(typeof(KafkaLogEvents));
        Console.WriteLine();
        Console.WriteLine("### MQTT");
        Console.WriteLine();
        DocsGenerator.GenerateDocsTable(typeof(MqttLogEvents));
    }
}
