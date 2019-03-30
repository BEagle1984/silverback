// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    public class MessageHeaderCollection : List<MessageHeader>
    {
        public void Add(string key, string value) => 
            Add(new MessageHeader {Key = key, Value = value});
    }
}