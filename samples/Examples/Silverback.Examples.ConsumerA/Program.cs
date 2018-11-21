using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Silverback.Examples.ConsumerA
{
public    class Program
    {
        static void Main(string[] args)
        {
            new ConsumerServiceA().Init();
            new LegacyConsumerService().Init();

            while (true)
            {
                Console.WriteLine(".");
                Thread.Sleep(5000);
            }
        }
    }
}
