using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LLVMSharp;
using Santol.CIL;
using Santol.Generator;

namespace Santol
{
    class Program
    {
        static void Main(string[] args)
        {
            AssemblyLoader loader = new AssemblyLoader();
            IList<ClassDefinition> classes = loader.Load("TestOS.exe");


            //Find target
            LLVM.InitializeAllTargetInfos();
            LLVM.InitializeAllTargets();
            LLVM.InitializeAllTargetMCs();
            LLVM.InitializeAllAsmParsers();
            LLVM.InitializeAllAsmPrinters();

            LLVMPassManagerBuilderRef passManagerBuilderRef = LLVM.PassManagerBuilderCreate();
            LLVM.PassManagerBuilderSetOptLevel(passManagerBuilderRef, 3);
            LLVMPassManagerRef passManagerRef = LLVM.CreatePassManager();
            LLVM.PassManagerBuilderPopulateModulePassManager(passManagerBuilderRef, passManagerRef);

            string target = "i386-pc-none-elf";
            Console.WriteLine("Current Platform: " + Marshal.PtrToStringAnsi((IntPtr)LLVM.GetDefaultTargetTriple()));
            Console.WriteLine("Target Platform: " + target);

            foreach (ClassDefinition classDefinition in classes)
            {
                ClassGenerator generator = new ClassGenerator(target, passManagerRef);
                generator.GenerateClass(classDefinition);
            }

            Console.ReadLine();
        }
    }
}