// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class HeadersUseCase : UseCase
    {
        public HeadersUseCase()
        {
            Title = "Custom message headers";
            Description = "The Timestamp property of the message is published as header. Additionally a behavior is " +
                          "used to add an extra 'generated-by' header.";
        }
    }
}
