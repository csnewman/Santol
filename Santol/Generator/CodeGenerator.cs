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
using Santol.Loader;
using Santol.Objects;
using MethodDefinition = Mono.Cecil.MethodDefinition;

namespace Santol.Generator
{
    public class CodeGenerator
    {
        public Compiler Compiler { get; }
        private IDictionary<MethodDefinition, FunctionGenerator> _functions;
        private IDictionary<string, LLVMValueRef> _globalRefs, _funcRefs;
        private IDictionary<TypeDefinition, LLVMTypeRef> _structCache;
        private IDictionary<string, ObjectFormat> _objectFormatCache;

        public CodeGenerator(Compiler compiler)
        {
            Compiler = compiler;
            _functions = new Dictionary<MethodDefinition, FunctionGenerator>();
            _globalRefs = new Dictionary<string, LLVMValueRef>();
            _funcRefs = new Dictionary<string, LLVMValueRef>();
            _structCache = new Dictionary<TypeDefinition, LLVMTypeRef>();
            _objectFormatCache = new Dictionary<string, ObjectFormat>();
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

        public LLVMTypeRef[] ConvertTypes(TypeReference[] reference)
        {
            return reference?.Select(ConvertType).ToArray() ?? new LLVMTypeRef[0];
        }

        public LLVMTypeRef ConvertType(TypeReference reference)
        {
            switch (reference.MetadataType)
            {
                case MetadataType.Void:
                    return LLVM.VoidTypeInContext(Compiler.Context);
                case MetadataType.Boolean:
                    return LLVM.Int1TypeInContext(Compiler.Context);

                case MetadataType.Byte:
                case MetadataType.SByte:
                    return LLVM.Int8TypeInContext(Compiler.Context);

                case MetadataType.Char:
                case MetadataType.UInt16:
                case MetadataType.Int16:
                    return LLVM.Int16TypeInContext(Compiler.Context);

                case MetadataType.UInt32:
                case MetadataType.Int32:
                    return LLVM.Int32TypeInContext(Compiler.Context);

                case MetadataType.UInt64:
                case MetadataType.Int64:
                    return LLVM.Int64TypeInContext(Compiler.Context);

                case MetadataType.Single:
                    return LLVM.FloatTypeInContext(Compiler.Context);
                case MetadataType.Double:
                    return LLVM.DoubleTypeInContext(Compiler.Context);

                case MetadataType.IntPtr:
                case MetadataType.UIntPtr:
                    return LLVM.PointerType(LLVM.Int8TypeInContext(Compiler.Context), 0);

                case MetadataType.Pointer:
                    if (reference.GetElementType().MetadataType == MetadataType.Void)
                        return LLVM.PointerType(LLVM.Int8TypeInContext(Compiler.Context), 0);
                    return LLVM.PointerType(ConvertType(reference.GetElementType()), 0);

                case MetadataType.ValueType:
                {
                    TypeDefinition def = reference.Resolve();
                    return def.IsEnum ? ConvertType(def.GetEnumUnderlyingType()) : GetStructType(def);
                }

                case MetadataType.Class:
                    return LLVM.PointerType(GetObjectFormat(reference.Resolve()).GetStructType(), 0);

                default:
                    Console.WriteLine("reference " + reference);
                    Console.WriteLine(" Element type " + reference.GetElementType());
                    Console.WriteLine("  MetadataType " + reference.MetadataType);
                    Console.WriteLine("  DeclaringType " + reference.DeclaringType);
                    Console.WriteLine("  FullName " + reference.FullName);
                    Console.WriteLine("  Name " + reference.Name);
                    Console.WriteLine("  MetadataToken " + reference.MetadataToken);
                    throw new NotImplementedException("Unknown type! " + reference);
            }
        }

        public LLVMTypeRef GetStructType(TypeDefinition ltype)
        {
            Console.WriteLine("reference " + ltype);
            Console.WriteLine(" Element type " + ltype.GetElementType());
            Console.WriteLine("  MetadataType " + ltype.MetadataType);
            Console.WriteLine("  DeclaringType " + ltype.DeclaringType);
            Console.WriteLine("  FullName " + ltype.FullName);
            Console.WriteLine("  Name " + ltype.Name);
            Console.WriteLine("  MetadataToken " + ltype.MetadataToken);
            Console.WriteLine("  HasLayoutInfo " + ltype.HasLayoutInfo);
            Console.WriteLine("  IsAutoLayout " + ltype.IsAutoLayout);
            Console.WriteLine("  IsSequentialLayout " + ltype.IsSequentialLayout);
            Console.WriteLine("  IsExplicitLayout " + ltype.IsExplicitLayout);

            if (_structCache.ContainsKey(ltype))
                return _structCache[ltype];

            LLVMTypeRef type = LLVM.StructCreateNamed(Compiler.Context, ltype.GetName());
            _structCache[ltype] = type;

            if (ltype.PackingSize > 1)
                throw new NotImplementedException("Unable to add packing");

            if (!ltype.IsSequentialLayout)
                throw new NotImplementedException("Unknown layout");


            FieldDefinition[] locals = ltype.GetLocals().ToArray();
            LLVMTypeRef[] types = new LLVMTypeRef[locals.Length];

            for (int i = 0; i < locals.Length; i++)
                types[i] = ConvertType(locals[i].FieldType);

            LLVM.StructSetBody(type, types, ltype.PackingSize == 1);

            return type;
        }

        public LLVMValueRef GetSize(LLVMTypeRef type, LLVMTypeRef sizeType)
        {
            return LLVM.ConstPtrToInt(LLVM.ConstGEP(LLVM.ConstNull(type),
                new[] {LLVM.ConstInt(LLVM.Int32TypeInContext(Compiler.Context), 1, false)}), sizeType);
//            LLVM.BuildGEP(Compiler.Builder, LLVM.ConstNull(type),
//                new[] {LLVM.ConstInt(LLVM.Int32TypeInContext(Compiler.Context), 1, false)}, "");
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

        public LoadedType Resolve(TypeReference @ref)
        {
            return Compiler.Resolve(@ref.FullName);
        }

        public bool IsEnum(TypeReference @ref)
        {
            LoadedType type = Resolve(@ref);
            return type != null && type.IsEnum;
        }

        public TypeReference GetEnumType(TypeReference @ref)
        {
            return IsEnum(@ref) ? Resolve(@ref).EnumType : null;
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