// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    public class MessageHeaderCollection : List<MessageHeader>
    {
        public void Add(string key, object value) =>
            Add(key, value.ToString());

        public void Add(string key, string value) => 
            Add(new MessageHeader {Key = key, Value = value});

        public void Remove(string key) =>
            RemoveAll(x => x.Key == key);

        public void AddOrReplace(string key, object newValue) =>
            AddOrReplace(key, newValue.ToString());

        public void AddOrReplace(string key, string newValue)
        {
            Remove(key);
            Add(key, newValue);
        }
    }
}