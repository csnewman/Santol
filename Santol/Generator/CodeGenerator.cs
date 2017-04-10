using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Mono.Cecil;
using Santol.Loader;
using MethodDefinition = Mono.Cecil.MethodDefinition;

namespace Santol.Generator
{
    public class CodeGenerator
    {
        private string _moduleName, _target;
        public LLVMModuleRef Module { get; }
        private LLVMContextRef _context;
        public LLVMBuilderRef Builder { get; }
        public TypeSystem TypeSystem { get; }
        private IDictionary<MethodDefinition, FunctionGenerator> _functions;
        private IDictionary<string, LLVMValueRef> _globalRefs, _funcRefs;
        private IDictionary<string, LoadedType> _types;

        public CodeGenerator(string moduleName, string target, TypeSystem typeSystem,
            IDictionary<string, LoadedType> types)
        {
            _moduleName = moduleName;
            _target = target;
            Module = LLVM.ModuleCreateWithName("Module_" + moduleName);
            _context = LLVM.GetModuleContext(Module);
            Builder = LLVM.CreateBuilder();
            LLVM.SetTarget(Module, target);
            TypeSystem = typeSystem;
            _functions = new Dictionary<MethodDefinition, FunctionGenerator>();
            _globalRefs = new Dictionary<string, LLVMValueRef>();
            _funcRefs = new Dictionary<string, LLVMValueRef>();
            _types = types;
        }

        public FunctionGenerator DefineFunction(MethodDefinition definition)
        {
            LLVMValueRef functionRef = GetFunctionRef(definition);
            LLVM.SetLinkage(functionRef, LLVMLinkage.LLVMExternalLinkage);
            FunctionGenerator func = new FunctionGenerator(this, definition, functionRef);
            _functions[definition] = func;
            return func;
        }
        
        public LLVMValueRef GetFunctionRef(MethodDefinition definition)
        {
            string name = definition.GetName();
            if (_funcRefs.ContainsKey(name))
                return _funcRefs[name];

            LLVMTypeRef functionType = GetFunctionType(definition);
            LLVMValueRef @ref = LLVM.AddFunction(Module, definition.GetName(), functionType);
            LLVM.SetLinkage(@ref, LLVMLinkage.LLVMAvailableExternallyLinkage);
            _funcRefs[name] = @ref;
            return @ref;
        }

        public LLVMTypeRef GetFunctionType(MethodDefinition definition)
        {
            LLVMTypeRef returnType = ConvertType(definition.ReturnType);
            LLVMTypeRef[] paramTypes = definition.Parameters.Select(p => ConvertType(p.ParameterType)).ToArray();

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

            LLVMValueRef @ref = LLVM.AddGlobal(Module, type, name);
            _globalRefs[name] = @ref;
            return @ref;
        }

        public LLVMTypeRef[] ConvertTypes(TypeReference[] reference)
        {
            return reference?.Select(ConvertType).ToArray() ?? new LLVMTypeRef[0];
        }

        public LLVMTypeRef ConvertType(TypeReference reference)
        {
            switch (reference.MetadataType)
            {
                case MetadataType.Void:
                    return LLVM.VoidTypeInContext(_context);
                case MetadataType.Boolean:
                    return LLVM.Int1TypeInContext(_context);

                case MetadataType.Byte:
                case MetadataType.SByte:
                    return LLVM.Int8TypeInContext(_context);

                case MetadataType.Char:
                case MetadataType.UInt16:
                case MetadataType.Int16:
                    return LLVM.Int16TypeInContext(_context);

                case MetadataType.UInt32:
                case MetadataType.Int32:
                    return LLVM.Int32TypeInContext(_context);

                case MetadataType.UInt64:
                case MetadataType.Int64:
                    return LLVM.Int64TypeInContext(_context);

                case MetadataType.Single:
                    return LLVM.FloatTypeInContext(_context);
                case MetadataType.Double:
                    return LLVM.DoubleTypeInContext(_context);

                case MetadataType.IntPtr:
                case MetadataType.UIntPtr:
                    return LLVM.PointerType(LLVM.Int8TypeInContext(_context), 0);

                case MetadataType.ValueType:
                {
                    LoadedType def = _types[reference.FullName];
                    if (def.IsEnum)
                        return ConvertType(def.EnumType);
                    throw new NotImplementedException("Unable to handle structs");
                }
                default:
                    Console.WriteLine("reference " + reference);
                    Console.WriteLine("  MetadataType " + reference.MetadataType);
                    Console.WriteLine("  DeclaringType " + reference.DeclaringType);
                    Console.WriteLine("  FullName " + reference.FullName);
                    Console.WriteLine("  Name " + reference.Name);
                    Console.WriteLine("  MetadataToken " + reference.MetadataToken);
                    throw new NotImplementedException("Unknown type! " + reference);
            }
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

        public bool IsEnum(TypeReference @ref)
        {
            return _types.ContainsKey(@ref.FullName) && _types[@ref.FullName].IsEnum;
        }

        public TypeReference GetEnumType(TypeReference @ref)
        {
            return IsEnum(@ref) ? Resolve(@ref).EnumType : null;
        }

        public LoadedType Resolve(TypeReference @ref)
        {
            return _types.ContainsKey(@ref.FullName) ? _types[@ref.FullName] : null;
        }

        public LLVMValueRef GenerateConversion(TypeReference sourceType, TypeReference destType, LLVMValueRef value)
        {
            if (sourceType == destType)
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
                            return LLVM.BuildZExt(Builder, value, ConvertType(destType), "");
                        default:
                            throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
                    }

                case MetadataType.Char:
                    switch (destType.MetadataType)
                    {
                        case MetadataType.Byte:
                            return LLVM.BuildTrunc(Builder, value, ConvertType(destType), "");
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

                case MetadataType.Int32:
                    switch (destType.MetadataType)
                    {
                        case MetadataType.Byte:
                        case MetadataType.Boolean:
                        case MetadataType.Char:
                        case MetadataType.UInt16:
                            return LLVM.BuildTrunc(Builder, value, ConvertType(destType), "");
                        case MetadataType.IntPtr:
                            return LLVM.BuildIntToPtr(Builder, value, ConvertType(destType), "");
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


                default:
                    throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
            }
        }

        public void Optimise(LLVMPassManagerRef passManagerRef)
        {
            LLVM.RunPassManager(passManagerRef, Module);
        }

        public void Dump()
        {
            LLVM.DumpModule(Module);
        }

        public void Compile()
        {
            LLVMTargetRef tref;
            IntPtr error;
            LLVM.GetTargetFromTriple(_target, out tref, out error);

            LLVMTargetMachineRef machineRef = LLVM.CreateTargetMachine(tref, _target,
                "generic", "",
                LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocDefault,
                LLVMCodeModel.LLVMCodeModelDefault);

            LLVM.TargetMachineEmitToFile(machineRef, Module,
                Marshal.StringToHGlobalAnsi(_moduleName + ".o"),
                LLVMCodeGenFileType.LLVMObjectFile,
                out error);

            LLVM.PrintModuleToFile(Module, _moduleName + ".ll", out error);
        }
    }
}