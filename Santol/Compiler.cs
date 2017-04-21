using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Santol.Generator;
using Santol.Loader;
using Santol.Nodes;

namespace Santol
{
    public class Compiler
    {
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
        public TypeSystem TypeSystem { get; private set; }
        public CodeGenerator CodeGenerator { get; private set; }
        public IDictionary<string, LoadedType> _loadedTypes;

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
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(source);
            TypeSystem = assembly.MainModule.TypeSystem;
            _loadedTypes = new Dictionary<string, LoadedType>();

            foreach (TypeDefinition type in assembly.MainModule.Types)
            {
                if (type.Name.Equals("<Module>")) continue;
                LoadType(type);
            }

            //Generate types
            CodeGenerator = new CodeGenerator(this);

            foreach (LoadedType loadedType in _loadedTypes.Values)
                GenerateType(loadedType);


            //Complete debug info
            if (GenerateDebug)
            {
                LLVM.DIBuilderFinalize(DIBuilder);
            }

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
            if (PassManager.HasValue)
                LLVM.RunPassManager(PassManager.Value, Module);

            //Compile module
            LLVMTargetRef tref;
            IntPtr error;
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

        private void LoadType(TypeDefinition typeDefinition)
        {
            Console.WriteLine("Loading " + typeDefinition.FullName);

            IList<FieldDefinition> localFields = new List<FieldDefinition>();
            IList<FieldDefinition> staticFields = new List<FieldDefinition>();
            IList<FieldDefinition> constantFields = new List<FieldDefinition>();
            foreach (FieldDefinition field in typeDefinition.Fields)
            {
                if (field.HasConstant)
                    constantFields.Add(field);
                else if (field.IsStatic)
                    staticFields.Add(field);
                else
                    localFields.Add(field);
            }

            IList<MethodInfo> staticMethods = new List<MethodInfo>();
            IList<MethodInfo> localMethods = new List<MethodInfo>();
            IList<MethodInfo> virtualMethods = new List<MethodInfo>();
            foreach (MethodDefinition methodD in typeDefinition.Methods)
            {
                MethodInfo method = new MethodInfo(methodD);

//                Console.WriteLine("\n\n" + methodD.GetName());

                methodD.Body.SimplifyMacros();
                method.FixFallthroughs();
                method.FixMidBranches();
//                method.PrintInstructions();

                method.ParseRegions();
//                method.PrintRegions();

                method.GenerateSegments();
                method.DetectNoIncomings();
                foreach (CodeSegment segment in method.Segments)
                {
                    segment.ParseInstructions(this);
                    segment.PatchNodes(this);
                }
//                method.PrintSegments();

                if (methodD.IsStatic)
                    staticMethods.Add(method);
                else if (methodD.IsVirtual || methodD.IsAbstract)
                    virtualMethods.Add(method);
                else
                    localMethods.Add(method);
            }

            _loadedTypes.Add(typeDefinition.FullName,
                new LoadedType(typeDefinition, staticFields, constantFields, localFields, staticMethods, localMethods,
                    virtualMethods));
        }

        public void GenerateType(LoadedType type)
        {
            Console.WriteLine($"Generating {type.Definition.FullName}");


            foreach (FieldDefinition field in type.ConstantFields)
            {
                CodeGenerator.SetConstant(field.GetName(), CodeGenerator.ConvertType(field.FieldType),
                    CodeGenerator.GeneratePrimitiveConstant(field.FieldType, field.Constant));
            }

            foreach (FieldDefinition field in type.StaticFields)
            {
                LLVMTypeRef ftype = CodeGenerator.ConvertType(field.FieldType);
                CodeGenerator.SetGlobal(field.GetName(), ftype, LLVM.ConstNull(ftype));
            }

            foreach (MethodInfo methodDefinition in type.StaticMethods)
                GenerateMethod(methodDefinition);

            foreach (MethodInfo methodDefinition in type.LocalMethods)
                GenerateMethod(methodDefinition);

            foreach (MethodInfo methodDefinition in type.VirtualMethods)
                GenerateMethod(methodDefinition);
        }

        public void GenerateMethod(MethodInfo method)
        {
            MethodDefinition definition = method.Definition;
            FunctionGenerator fgen = CodeGenerator.DefineFunction(definition);

            //Allocate locals
            fgen.CreateBlock("entry", null);
            {
                ICollection<VariableDefinition> variables = definition.Body.Variables;
                fgen.Locals = new LLVMValueRef[variables.Count];
                foreach (VariableDefinition variable in variables)
                {
                    string name = "local_" +
                                  (string.IsNullOrEmpty(variable.Name) ? variable.Index.ToString() : variable.Name);
                    LLVMTypeRef type = CodeGenerator.ConvertType(variable.VariableType);
                    fgen.Locals[variable.Index] = LLVM.BuildAlloca(Builder, type, name);
                }
            }

            IList<CodeSegment> segments = method.Segments;
            foreach (CodeSegment segment in segments)
                fgen.CreateBlock(segment, CodeGenerator.ConvertTypes(segment.Incoming));

            //Enter first segment
            fgen.SelectBlock("entry");
            fgen.Branch(segments[0], null);

            foreach (CodeSegment segment in segments)
            {
                fgen.SelectBlock(segment);

                foreach (NodeReference node in segment.Nodes)
                    node.Node.Generate(fgen);
            }
        }

        public LoadedType Resolve(string name)
        {
            return _loadedTypes.ContainsKey(name) ? _loadedTypes[name] : null;
        }
    }
}