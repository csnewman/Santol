using System;

namespace Santol
{
    class Program
    {
        static void Main(string[] args)
        {
            Compiler compiler = new Compiler();
            compiler.InitLLVM();
            compiler.TargetPlatform = "i386-pc-none-elf";
            compiler.OptimisationLevel = 3;

            compiler.Compile("Rowan.dll", "Rowan.o");
//            compiler.Compile("../../../../Santol.Corlib/bin/Debug/Santol.Corlib.dll", "Santol.Corlib.o");

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}