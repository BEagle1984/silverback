// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestSerializer : IMessageSerializer
    {
        public int MustFailCount { get; set; }

        public int FailCount { get; private set; }

        public byte[] Serialize(object message)
        {
            throw new NotImplementedException();
        }

        public object Deserialize(byte[] message)
        {
            if (MustFailCount > FailCount)
            {
                FailCount++;
                throw new Exception("Test failure");
            }

            return new JsonMessageSerializer().Deserialize(message);
        }
    }
}