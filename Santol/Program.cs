using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LLVMSharp;
using Santol.Loader;
using Santol.Generator;

namespace Santol
{

    class Program
    {
        static void Main(string[] args)
        {
            AssemblyLoader loader = new AssemblyLoader();
            IDictionary<string, LoadedType> types = loader.Load("TestOS.exe");


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

            foreach (LoadedType type in types.Values)
            {
                ModuleGenerator generator = new ModuleGenerator(target, passManagerRef, types);
                generator.GenerateType(type);
            }

            Console.ReadLine();
        }
        
    }
}