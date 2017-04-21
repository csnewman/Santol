using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LLVMSharp;
using Santol.Loader;
using Santol.Generator;
using Santol.Patchers;

namespace Santol
{
    class Program
    {
        static void Main(string[] args)
        {
            Compiler compiler = new Compiler();
            compiler.Init();
            compiler.TargetPlatform = "i386-pc-none-elf";
            compiler.OptimisationLevel = 3;

            compiler.SegmentPatchers.Add(new PrimitiveSegmentPatcher());

            compiler.Compile("Rowan.dll", "Rowan.o");

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}