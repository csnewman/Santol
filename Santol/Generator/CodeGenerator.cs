using System;
using System.Runtime.InteropServices;
using LLVMSharp;
using Santol.IR;

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
    }
}