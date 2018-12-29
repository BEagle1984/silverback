// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// Helps to retrieve the message properties with KeyMember attribute for creating a key.
    /// </summary>
    internal static class KeyHelper
    {
        public static byte[] GetMessageKey(IMessage message)
        {
            var keysDictionary = 
                message.GetType()
                .GetProperties()
                .Where(p => p.IsDefined(typeof(KeyMemberAttribute), true))
                .ToDictionary(p => p.Name, p => p.GetValue(message, null));

            return keysDictionary.Count > 0
                ? Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(keysDictionary))
                : null;
        }
    }
}