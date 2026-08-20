using System;
namespace STSAutoPlay
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            Thread.Sleep(1000);
            char key = Console.ReadKey().KeyChar;
            Console.WriteLine("You pressed: " + key);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

        }
    }
}
