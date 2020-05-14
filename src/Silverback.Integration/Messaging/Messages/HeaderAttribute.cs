// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Used to decorate a property which value must be produced/consumed as message header.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class HeaderAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="HeaderAttribute" /> class specifying the name of
        ///     the header. When a property is decorated with this attribute its value will be
        ///     produced/consumed as message header.
        /// </summary>
        /// <param name="headerName"> The name of the header. </param>
        public HeaderAttribute(string headerName)
        {
            HeaderName = headerName;
        }

        /// <summary> Gets the name of the header. </summary>
        public string HeaderName { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether the header must be produced even if the property is set
        ///     to the default value for its declaring type.
        /// </summary>
        public bool PublishDefaultValue { get; set; }
    }
}
