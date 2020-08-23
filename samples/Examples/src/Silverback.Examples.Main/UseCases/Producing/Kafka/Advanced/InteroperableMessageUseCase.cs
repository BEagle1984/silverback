// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class InteroperableMessageUseCase : UseCase
    {
        public InteroperableMessageUseCase()
        {
            Title = "Interoperability";
            Description = "A message is sent using a custom serializer and without the headers needed by Silverback" +
                          "to deserialize it. The consumer serializer is tweaked to work with this 'legacy' messages.";
        }
    }
}
