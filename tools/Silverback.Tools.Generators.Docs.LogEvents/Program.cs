﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Diagnostics;
using Silverback.Tools.Generators.Docs.LogEvents;

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