// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;

namespace Silverback.Examples.ConsumerA
{
    public class Program
    {
        static void Main(string[] args)
        {
            new ConsumerServiceA().Init();

            while (true)
            {
                Console.WriteLine(".");
                Thread.Sleep(5000);
            }
        }
    }
}
