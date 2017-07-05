using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Santol.IR;
using Santol.Loader;
using Santol.Objects;
using MethodDefinition = Mono.Cecil.MethodDefinition;

namespace Santol.Generator
{
    public class CodeGenerator
    {
        public string TargetPlatform { get; }
        public int OptimisationLevel { get; }
        public LLVMPassManagerRef? PassManager { get; set; }
        public LLVMModuleRef Module { get; private set; }
        public LLVMContextRef Context { get; private set; }
        public LLVMBuilderRef Builder { get; private set; }

        public CodeGenerator(string targetPlatform, int optimisation, string moduleName)
        {
            TargetPlatform = targetPlatform;
            OptimisationLevel = optimisation;

            if (optimisation != -1)
            {
                LLVMPassManagerBuilderRef passManagerBuilderRef = LLVM.PassManagerBuilderCreate();
                LLVM.PassManagerBuilderSetOptLevel(passManagerBuilderRef, (uint) optimisation);
                PassManager = LLVM.CreatePassManager();
                LLVM.PassManagerBuilderPopulateModulePassManager(passManagerBuilderRef, PassManager.Value);
            }

            Module = LLVM.ModuleCreateWithName(moduleName);
            Context = LLVM.GetModuleContext(Module);
            Builder = LLVM.CreateBuilder();
            LLVM.SetTarget(Module, TargetPlatform);

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
        }

        public void DumpModuleToFile(string file)
        {
            IntPtr error;
            LLVM.PrintModuleToFile(Module, file, out error);
            LLVM.DisposeMessage(error);
        }

        public void OptimiseModule()
        {
            if (PassManager.HasValue)
                LLVM.RunPassManager(PassManager.Value, Module);
        }

        public void CompileModule(string file)
        {
            IntPtr error;
            LLVMTargetRef tref;
            LLVM.GetTargetFromTriple(TargetPlatform, out tref, out error);
            LLVM.DisposeMessage(error);

            LLVMTargetMachineRef machineRef = LLVM.CreateTargetMachine(tref, TargetPlatform, "generic", "",
                LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocDefault,
                LLVMCodeModel.LLVMCodeModelDefault);

            LLVM.TargetMachineEmitToFile(machineRef, Module, Marshal.StringToHGlobalAnsi(file),
                LLVMCodeGenFileType.LLVMObjectFile, out error);

            LLVM.DisposeTargetMachine(machineRef);
        }

        public LLVMValueRef GenerateConversion(IType from, IType to, LLVMValueRef value)
        {
            if (from.Equals(to))
                return value;
            LLVMValueRef? converted = from.ConvertTo(this, to, value);
            if (converted.HasValue)
                return converted.Value;
            converted = to.ConvertFrom(this, from, value);
            if (converted.HasValue)
                return converted.Value;
            throw new NotSupportedException("Unable to convert from " + from + " to " + to);
        }


        public FunctionGenerator DefineFunction(MethodDefinition definition)
        {
            LLVMValueRef functionRef = GetFunctionRef(definition);
            LLVM.SetLinkage(functionRef, LLVMLinkage.LLVMExternalLinkage);
            FunctionGenerator func = new FunctionGenerator(this, definition, functionRef);
            _functions[definition] = func;
            return func;
        }

        public LLVMValueRef GetFunctionRef(MethodReference definition)
        {
            string name = definition.GetName();
            if (_funcRefs.ContainsKey(name))
                return _funcRefs[name];

            LLVMTypeRef functionType = GetFunctionType(definition);
            LLVMValueRef @ref = LLVM.AddFunction(Compiler.Module, definition.GetName(), functionType);
            LLVM.SetLinkage(@ref, LLVMLinkage.LLVMAvailableExternallyLinkage);
            _funcRefs[name] = @ref;
            return @ref;
        }

        public LLVMTypeRef GetFunctionType(MethodReference definition)
        {
            LLVMTypeRef returnType = ConvertType(definition.ReturnType);
            LLVMTypeRef[] paramTypes =
                new LLVMTypeRef[definition.Parameters.Count + (definition.HasThis && !definition.ExplicitThis ? 1 : 0)];

            for (int i = 0; i < definition.Parameters.Count; i++)
                paramTypes[i + (definition.HasThis && !definition.ExplicitThis ? 1 : 0)] =
                    ConvertType(definition.Parameters[i].ParameterType);

            if (definition.HasThis)
            {
                LLVMTypeRef baseType = ConvertType(definition.DeclaringType);
                paramTypes[0] = definition.DeclaringType.IsValueType ? LLVM.PointerType(baseType, 0) : baseType;
            }

            return LLVM.FunctionType(returnType, paramTypes, false);
        }

        public void SetConstant(string name, LLVMTypeRef type, LLVMValueRef value)
        {
            LLVMValueRef glob = GetGlobal(name, type);
            LLVM.SetInitializer(glob, value);
            LLVM.SetGlobalConstant(glob, true);
        }

        public void SetGlobal(string name, LLVMTypeRef type, LLVMValueRef value)
        {
            LLVMValueRef glob = GetGlobal(name, type);
            LLVM.SetInitializer(glob, value);
        }

        public LLVMValueRef GetGlobal(string name, LLVMTypeRef type)
        {
            if (_globalRefs.ContainsKey(name))
                return _globalRefs[name];

            LLVMValueRef @ref = LLVM.AddGlobal(Compiler.Module, type, name);
            _globalRefs[name] = @ref;
            return @ref;
        }


        public LLVMValueRef GetSize(LLVMTypeRef type, LLVMTypeRef sizeType)
        {
            return LLVM.ConstPtrToInt(LLVM.ConstGEP(LLVM.ConstNull(type),
                new[] {LLVM.ConstInt(LLVM.Int32TypeInContext(Context), 1, false)}), sizeType);
//            LLVM.BuildGEP(Compiler.Builder, LLVM.ConstNull(type),
//                new[] {LLVM.ConstInt(LLVM.Int32TypeInContext(Context), 1, false)}, "");
        }


        public ObjectFormat GetObjectFormat(TypeDefinition type)
        {
            if (_objectFormatCache.ContainsKey(type.GetName()))
                return _objectFormatCache[type.GetName()];

            ObjectFormat format = new ObjectFormat(this, type);
            _objectFormatCache[type.GetName()] = format;
            return format;
        }
    }
}