// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Basic
{
    public class TranslateUseCase : UseCase
    {
        public TranslateUseCase()
        {
            Title = "Message translation";
            Description = "A translation/mapping method is used to transform the messages to be published.";
        }
    }
}
