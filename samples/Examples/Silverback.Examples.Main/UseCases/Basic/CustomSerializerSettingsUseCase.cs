using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Main.UseCases.Basic
{
    public class CustomSerializerSettingsUseCase : UseCase
    {
        public CustomSerializerSettingsUseCase() : base("Custom serializer settings", 30)
        {
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddBus()
            .AddBroker<FileSystemBroker>();

        protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints) => endpoints
            .AddOutbound<IIntegrationEvent>(CreateEndpoint("custom-serializer-settings-events"));

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher<CustomSerializedIntegrationEvent>>();

            await publisher.PublishAsync(new CustomSerializedIntegrationEvent { Content = DateTime.Now.ToString("HH:mm:ss.fff") });
        }

        private IEndpoint CreateEndpoint(string name) =>
            new FileSystemEndpoint(name, Configuration.FileSystemBrokerBasePath)
            {
                Serializer = GetSerializer()
            };

        private static JsonMessageSerializer GetSerializer()
        {
            var serializer = new JsonMessageSerializer
            {
                Encoding = MessageEncoding.Unicode
            };

            serializer.Settings.Formatting = Newtonsoft.Json.Formatting.Indented;

            return serializer;
        }
    }
}