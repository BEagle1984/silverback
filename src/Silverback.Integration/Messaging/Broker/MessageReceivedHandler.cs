// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Broker
{
    public delegate Task MessageReceivedHandler(object sender, MessageReceivedEventArgs e);
}