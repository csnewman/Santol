﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Mono.Cecil;
using Mono.Cecil.Rocks;
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

        public LLVMValueRef GeneratePrimitiveConstant(TypeReference typeReference, object value)
        {
            if (typeReference.MetadataType == MetadataType.ValueType)
            {
                LoadedType def = Resolve(typeReference);
                if (def.IsEnum)
                    return GeneratePrimitiveConstant(def.EnumType, value);
                throw new NotImplementedException("Unable to handle structs");
            }

            LLVMTypeRef type = ConvertType(typeReference);

            switch (typeReference.MetadataType)
            {
                case MetadataType.Boolean:
                    return LLVM.ConstInt(type, (ulong) (((bool) value) ? 1 : 0), false);

                case MetadataType.Byte:
                    return LLVM.ConstInt(type, (byte) value, false);
                case MetadataType.SByte:
                    return LLVM.ConstInt(type, (ulong) (sbyte) value, true);

                case MetadataType.Char:
                    throw new NotImplementedException("Char data " + value.GetType() + "=" + value);

                case MetadataType.UInt16:
                    return LLVM.ConstInt(type, (ushort) value, false);
                case MetadataType.Int16:
                    return LLVM.ConstInt(type, (ulong) (short) value, true);

                case MetadataType.UInt32:
                    return LLVM.ConstInt(type, (uint) value, false);
                case MetadataType.Int32:
                    return LLVM.ConstInt(type, (ulong) (int) value, true);

                case MetadataType.UInt64:
                    return LLVM.ConstInt(type, (ulong) value, false);
                case MetadataType.Int64:
                    return LLVM.ConstInt(type, (ulong) (long) value, true);

                case MetadataType.Single:
                    return LLVM.ConstReal(type, (float) value);
                case MetadataType.Double:
                    return LLVM.ConstReal(type, (double) value);

                case MetadataType.IntPtr:
                    throw new NotImplementedException("IntPtr data " + value.GetType() + "=" + value);
                case MetadataType.UIntPtr:
                    throw new NotImplementedException("UIntPtr data " + value.GetType() + "=" + value);
                default:
                    throw new NotImplementedException("Unknown type! " + typeReference + " (" + value + ")");
            }
        }

        public ObjectFormat GetObjectFormat(TypeDefinition type)
        {
            if (_objectFormatCache.ContainsKey(type.GetName()))
                return _objectFormatCache[type.GetName()];

            ObjectFormat format = new ObjectFormat(this, type);
            _objectFormatCache[type.GetName()] = format;
            return format;
        }

        public LLVMValueRef GenerateConversion(TypeReference sourceType, TypeReference destType, LLVMValueRef value)
        {
            if (sourceType == destType)
                return value;
            if (sourceType.Resolve().Is(destType.Resolve()))
                return value;

            if (sourceType.MetadataType == MetadataType.ValueType)
            {
                LoadedType def = Resolve(sourceType);
                if (def.IsEnum)
                    return GenerateConversion(def.EnumType, destType, value);
                throw new NotImplementedException("Unable to handle structs");
            }

            if (destType.MetadataType == MetadataType.ValueType)
            {
                LoadedType def = Resolve(destType);
                if (def.IsEnum)
                    return GenerateConversion(sourceType, def.EnumType, value);
                throw new NotImplementedException("Unable to handle structs");
            }

            switch (sourceType.MetadataType)
            {
                case MetadataType.Boolean:
                    switch (destType.MetadataType)
                    {
                        case MetadataType.Int32:
                            return LLVM.BuildZExt(Compiler.Builder, value, ConvertType(destType), "");
                        default:
                            throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
                    }

                case MetadataType.Char:
                    switch (destType.MetadataType)
                    {
                        case MetadataType.Byte:
                            return LLVM.BuildTrunc(Compiler.Builder, value, ConvertType(destType), "");
                        case MetadataType.Int32:
                            return LLVM.BuildZExt(Compiler.Builder, value, ConvertType(destType), "");
                        default:
                            throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
                    }

                case MetadataType.UInt16:
                    switch (destType.MetadataType)
                    {
                        case MetadataType.Char:
                            //TODO: Check
                            return value;
                        default:
                            throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
                    }


                case MetadataType.UInt32:
                    switch (destType.MetadataType)
                    {
                        case MetadataType.Int32:
                            //TODO: Check
                            return value;
                        case MetadataType.UIntPtr:
                            return LLVM.BuildIntToPtr(Compiler.Builder, value, ConvertType(destType), "");
                        default:
                            throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
                    }

                case MetadataType.Int32:
                    switch (destType.MetadataType)
                    {
                        case MetadataType.Byte:
                        case MetadataType.Boolean:
                        case MetadataType.Char:
                        case MetadataType.UInt16:
                            return LLVM.BuildTrunc(Compiler.Builder, value, ConvertType(destType), "");
                        case MetadataType.IntPtr:
                            return LLVM.BuildIntToPtr(Compiler.Builder, value, ConvertType(destType), "");
                        case MetadataType.Int64:
                            return LLVM.BuildSExt(Compiler.Builder, value, ConvertType(destType), "");
                        default:
                            throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
                    }

                case MetadataType.Int64:
                    switch (destType.MetadataType)
                    {
                        case MetadataType.UInt64:
                            //TODO: Check
                            return value;
                        case MetadataType.UIntPtr:
                            return LLVM.BuildIntToPtr(Compiler.Builder, value, ConvertType(destType), "");
                        default:
                            throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
                    }

                case MetadataType.IntPtr:
                    switch (destType.MetadataType)
                    {
                        case MetadataType.UIntPtr:
                            //TODO: Check
                            return value;
                        default:
                            throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
                    }

                case MetadataType.Object:
                case MetadataType.Class:
                    switch (destType.MetadataType)
                    {
                        case MetadataType.Object:
                        case MetadataType.Class:
                            return GetObjectFormat(sourceType.Resolve()).UpcastTo(value, destType.Resolve());
                        default:
                            throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
                    }

                default:
                    throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
            }
        }
    }
}