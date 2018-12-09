// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.PerformanceTester.Testers;

namespace Silverback.PerformanceTester
{
    class Program
    {
        private const int Iterations = 1000;

        static void Main(string[] args)
        {
            while (true)
                Prompt();
        }

        private static void Prompt()
        {
            Console.Clear();

            Console.WriteLine("Select test case:");
            Console.WriteLine("[0] Direct method call (baseline)");
            Console.WriteLine("[1] Internal event");
            Console.WriteLine("[2] Internal query (message with respose)");
            Console.WriteLine("[3] Internal event consumed with Rx");

            var input = Console.ReadLine();

            if (!int.TryParse(input, out var choice))
                return;

            switch (choice)
            {
                case 0:
                    new DirectMethodCallTester(Iterations).Test();
                    break;
                case 1:
                    new InternalEventTester(Iterations).Test();
                    break;
                case 2:
                    new InternalQueryTester(Iterations).Test();
                    break;
                case 3:
                    new InternalEventRxTester(Iterations).Test();
                    break;
            }
        }
    }
}
