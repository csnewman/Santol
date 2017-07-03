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
        public string HostPlatform => Marshal.PtrToStringAnsi(LLVM.GetDefaultTargetTriple());
        public string TargetPlatform { get; set; }
        private int _optimisationLevel;
        private LLVMPassManagerRef? _passManager;
        public bool GenerateDebug { get; set; }
        public LLVMModuleRef Module { get; private set; }
        public LLVMContextRef Context { get; private set; }
        public LLVMBuilderRef Builder { get; private set; }
        public LLVMDIBuilderRef DIBuilder { get; private set; }
        public LLVMMetadataRef CompileUnit { get; private set; }
        public CodeGenerator CodeGenerator { get; private set; }
        public IList<ISegmentPatcher> SegmentPatchers { get; } = new List<ISegmentPatcher>();
        public IList<IInstructionPatcher> InstructionPatchers { get; } = new List<IInstructionPatcher>();

        public int OptimisationLevel
        {
            get { return _optimisationLevel; }
            set
            {
                _optimisationLevel = value;
                LLVMPassManagerBuilderRef passManagerBuilderRef = LLVM.PassManagerBuilderCreate();
                LLVM.PassManagerBuilderSetOptLevel(passManagerBuilderRef, (uint) _optimisationLevel);
                LLVMPassManagerRef passManagerRef = LLVM.CreatePassManager();
                LLVM.PassManagerBuilderPopulateModulePassManager(passManagerBuilderRef, passManagerRef);
                PassManager = passManagerRef;
            }
        }

        public LLVMPassManagerRef? PassManager
        {
            get { return _passManager; }
            set
            {
                if (_passManager.HasValue)
                    LLVM.DisposePassManager(_passManager.Value);
                _passManager = value;
            }
        }

        public void Init()
        {
            LLVM.InitializeAllTargetInfos();
            LLVM.InitializeAllTargets();
            LLVM.InitializeAllTargetMCs();
            LLVM.InitializeAllAsmParsers();
            LLVM.InitializeAllAsmPrinters();
        }

        public void Compile(string source, string dest)
        {
            //Create module
            Module = LLVM.ModuleCreateWithName("Module_" + Path.GetFileNameWithoutExtension(dest));
            Context = LLVM.GetModuleContext(Module);
            Builder = LLVM.CreateBuilder();
            LLVM.SetTarget(Module, TargetPlatform);

            //Create debug info
            if (GenerateDebug)
            {
                DIBuilder = LLVM.NewDIBuilder(Module);
                CompileUnit = LLVM.DIBuilderCreateCompileUnit(DIBuilder, 12, Path.GetFileName(source),
                    Path.GetDirectoryName(source),
                    "Santol Compiler",
                    1, "", 0);
            }

            //Load types
            AssemblyLoader loader = new AssemblyLoader();
            IList<IType> types = loader.Load(source);

            //Generate types
            CodeGenerator = new CodeGenerator(this);

//            foreach (LoadedType loadedType in _loadedTypes.Values)
//                GenerateType(loadedType);


            //Complete debug info
            if (GenerateDebug)
                LLVM.DIBuilderFinalize(DIBuilder);

            LLVM.AddNamedMetadataOperand(Module, "llvm.module.flags", LLVM.MDNode(new[]
            {
                LLVM.ConstInt(LLVM.Int32Type(), 2, false),
                LLVM.MDString("Dwarf Version", (uint) "Dwarf Version".Length),
                LLVM.ConstInt(LLVM.Int32Type(), 4, false)
            }));
            LLVM.AddNamedMetadataOperand(Module, "llvm.module.flags", LLVM.MDNode(new[]
            {
                LLVM.ConstInt(LLVM.Int32Type(), 2, false),
                LLVM.MDString("Debug Info Version", (uint) "Debug Info Version".Length),
                LLVM.ConstInt(LLVM.Int32Type(), 3, false)
            }));

            //Optimise module
            IntPtr error;
            LLVM.PrintModuleToFile(Module, dest + ".preopt.ll", out error);
            LLVM.DisposeMessage(error);

            if (PassManager.HasValue)
                LLVM.RunPassManager(PassManager.Value, Module);

            //Compile module
            LLVMTargetRef tref;
            LLVM.GetTargetFromTriple(TargetPlatform, out tref, out error);
            LLVM.DisposeMessage(error);

            LLVM.PrintModuleToFile(Module, dest + ".ll", out error);
            LLVM.DisposeMessage(error);

            LLVMTargetMachineRef machineRef = LLVM.CreateTargetMachine(tref, TargetPlatform,
                "generic", "",
                LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocDefault,
                LLVMCodeModel.LLVMCodeModelDefault);

            LLVM.TargetMachineEmitToFile(machineRef, Module,
                Marshal.StringToHGlobalAnsi(dest),
                LLVMCodeGenFileType.LLVMObjectFile,
                out error);

            LLVM.DisposeTargetMachine(machineRef);
        }
    }
}