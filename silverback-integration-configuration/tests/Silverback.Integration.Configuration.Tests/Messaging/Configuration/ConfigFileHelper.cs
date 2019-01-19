// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using Microsoft.Extensions.Configuration;

namespace Silverback.Tests.Messaging.Configuration
{
    public static class ConfigFileHelper
    {
        public static IConfiguration GetConfiguration(string fileName) => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(Path.Combine("AppSettings", fileName + ".json"))
            .Build();

        public static IConfigurationSection GetConfigSection(string fileName, string sectionName = "Silverback") =>
            GetConfiguration(fileName).GetSection(sectionName);
    }
}