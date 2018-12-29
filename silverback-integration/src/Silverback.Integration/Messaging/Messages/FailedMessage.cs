// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages
{
    public class FailedMessage : IMessage
    {
        public FailedMessage()
        { }

        public FailedMessage(IMessage message, int failedAttempts = 1)
        {
            if (failedAttempts < 1) throw new ArgumentOutOfRangeException(nameof(failedAttempts), failedAttempts, "failedAttempts must be >= 1");

            Message = message;
            FailedAttempts = failedAttempts;
        }

        public IMessage Message { get; set; }

        public int FailedAttempts { get; set; }
    }
}