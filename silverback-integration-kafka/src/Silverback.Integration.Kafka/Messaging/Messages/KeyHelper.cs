using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// Attribute for decorating the message properties.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    // TODO: (REVIEW) Move in another file
    [AttributeUsage(AttributeTargets.Property)]
    public class KeyMemberAttribute : Attribute
    {

    }

    /// <summary>
    /// Helps to retrieve the message properties with KeyMember attribute for creating a key.
    /// </summary>
    internal static class KeyHelper
    {
        public static byte[] GetMessageKey(IIntegrationMessage message)
        {
            var keyObj = message.GetType().GetProperties().Where(p => p.IsDefined(typeof(KeyMemberAttribute), false))
                .ToDictionary(p => p.Name, p => p.GetValue(message, null));

            return keyObj.Count > 0
                ? Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(keyObj))
                : null;
        }
    }
}