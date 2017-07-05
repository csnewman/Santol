using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using GraphvizWrapper;
using LLVMSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Santol.Generator;
using Santol.IR;
using Santol.Loader;
using Santol.Nodes;
using Santol.Patchers;

namespace Santol
{
    public class Compiler
    {
        public Graphviz Graphviz { get; set; }
        public bool GenerateGraphs { get; set; }
        public string GraphsTargetDirectory { get; set; }
        public string TargetPlatform { get; set; }
        public string HostPlatform => Marshal.PtrToStringAnsi(LLVM.GetDefaultTargetTriple());
        public int OptimisationLevel { get; set; }

        public CodeGenerator CodeGenerator { get; private set; }
        public IList<ISegmentPatcher> SegmentPatchers { get; } = new List<ISegmentPatcher>();
        public IList<IInstructionPatcher> InstructionPatchers { get; } = new List<IInstructionPatcher>();
        
        public void InitLLVM()
        {
            LLVM.InitializeAllTargetInfos();
            LLVM.InitializeAllTargets();
            LLVM.InitializeAllTargetMCs();
            LLVM.InitializeAllAsmParsers();
            LLVM.InitializeAllAsmPrinters();
        }

        public void Compile(string source, string dest)
        {
            // Load types
            AssemblyLoader loader = new AssemblyLoader();
            IList<IType> types = loader.Load(source);

            // Generate types
            CodeGenerator = new CodeGenerator(TargetPlatform, OptimisationLevel,
                "Module_" + Path.GetFileNameWithoutExtension(dest));

//            foreach (LoadedType loadedType in _loadedTypes.Values)
//                GenerateType(loadedType);

            // Ouput module
            CodeGenerator.DumpModuleToFile(dest + ".preopt.ll");
            CodeGenerator.OptimiseModule();
            CodeGenerator.DumpModuleToFile(dest + ".ll");
            CodeGenerator.CompileModule(dest);
        }
    }
}