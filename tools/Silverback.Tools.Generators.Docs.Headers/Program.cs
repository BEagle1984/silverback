// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Tools.Generators.Docs.Headers;

Console.WriteLine("### Default");
Console.WriteLine();
DocsGenerator.GenerateDocsTable(typeof(DefaultMessageHeaders));
Console.WriteLine();
Console.WriteLine("### Kafka");
Console.WriteLine();
DocsGenerator.GenerateDocsTable(typeof(KafkaMessageHeaders));
Console.WriteLine();
Console.WriteLine("### MQTT");
Console.WriteLine();
DocsGenerator.GenerateDocsTable(typeof(MqttMessageHeaders));
