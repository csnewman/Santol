using System;

namespace Santol
{
    class Program
    {
        static void Main(string[] args)
        {
            AssemblyLoader loader = new AssemblyLoader();
            loader.Load("TestOS.exe");
            Console.ReadLine();
        }
    }
}