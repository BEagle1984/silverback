// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.LargeMessages
{
    /// <summary>
    /// Used to decorate the properties that may contain large data and need to be
    /// offloaded to an external storage.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class OffloadAttribute : Attribute
    {
    }
}