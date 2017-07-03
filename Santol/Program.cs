using System;

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
//
//            compiler.Graphviz = new Graphviz(@"C:\Program Files (x86)\Graphviz2.38\bin");
//            compiler.GenerateGraphs = true;
//            compiler.GraphsTargetDirectory = "Rowan";
//
//            compiler.SegmentPatchers.Add(new PrimitiveSegmentPatcher());
//            compiler.InstructionPatchers.Add(new PrimitiveInstructionPatcher());
//
            compiler.Compile("Rowan.dll", "Rowan.o");
//            compiler.Compile("../../../../Santol.Corlib/bin/Debug/Santol.Corlib.dll", "Santol.Corlib.o");

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}