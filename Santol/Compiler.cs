using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using LLVMSharp;
using Santol.Generator;
using Santol.IR;
using Santol.Loader;

namespace Santol
{
    public class Compiler
    {
        public bool GenerateGraphs { get; set; }
        public string GraphsTargetDirectory { get; set; }
        public string TargetPlatform { get; set; }
        public string HostPlatform => Marshal.PtrToStringAnsi(LLVM.GetDefaultTargetTriple());
        public int OptimisationLevel { get; set; }

        public void InitLLVM()
        {
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetMC();
            LLVM.InitializeX86AsmParser();
            LLVM.InitializeX86AsmPrinter();
        }

        public void Compile(string source, string dest)
        {
            // Load types
            AssemblyLoader loader = new AssemblyLoader();
            IList<IType> types = loader.Load(source);

            // Generate types
            CodeGenerator codeGenerator = new CodeGenerator(TargetPlatform, OptimisationLevel,
                "Module_" + Path.GetFileNameWithoutExtension(dest));

            foreach (IType type in types)
            {
                Console.WriteLine($"Generating {type.Name}");
                type.Generate(loader, codeGenerator);
            }

            // Ouput module
            codeGenerator.DumpModuleToFile(dest + ".preopt.ll");
            codeGenerator.OptimiseModule();
            codeGenerator.DumpModuleToFile(dest + ".ll");
            codeGenerator.CompileModule(dest);
        }
    }
}