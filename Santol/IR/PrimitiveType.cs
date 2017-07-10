using System;
using System.Collections.Generic;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;
using Santol.Loader;

namespace Santol.IR
{
    public sealed class PrimitiveType : IType
    {
        public static readonly PrimitiveType Void = new PrimitiveType("void");
        public static readonly PrimitiveType Boolean = new PrimitiveType("bool");
        public static readonly PrimitiveType Byte = new PrimitiveType("byte");
        public static readonly PrimitiveType SByte = new PrimitiveType("sbyte");
        public static readonly PrimitiveType Char = new PrimitiveType("char");
        public static readonly PrimitiveType Int16 = new PrimitiveType("int16");
        public static readonly PrimitiveType UInt16 = new PrimitiveType("uint16");
        public static readonly PrimitiveType Int32 = new PrimitiveType("int32");
        public static readonly PrimitiveType UInt32 = new PrimitiveType("uint32");
        public static readonly PrimitiveType Int64 = new PrimitiveType("int64");
        public static readonly PrimitiveType UInt64 = new PrimitiveType("uint64");
        public static readonly PrimitiveType Single = new PrimitiveType("single");
        public static readonly PrimitiveType Double = new PrimitiveType("double");
        public static readonly PrimitiveType IntPtr = new PrimitiveType("intptr");
        public static readonly PrimitiveType UIntPtr = new PrimitiveType("uintptr");

        private enum ConversionMethod
        {
            Bitcast,
            ZeroExtend,
            SignExtend,
            Truncate,
            IntToPtr
        }

        private static readonly Dictionary<Tuple<PrimitiveType, PrimitiveType>, ConversionMethod> Conversions =
            new Dictionary<Tuple<PrimitiveType, PrimitiveType>, ConversionMethod>
            {
                [new Tuple<PrimitiveType, PrimitiveType>(Boolean, Int32)] = ConversionMethod.ZeroExtend,
                [new Tuple<PrimitiveType, PrimitiveType>(Char, Byte)] = ConversionMethod.Truncate,
                [new Tuple<PrimitiveType, PrimitiveType>(Char, Int32)] = ConversionMethod.ZeroExtend,
                [new Tuple<PrimitiveType, PrimitiveType>(UInt16, Char)] = ConversionMethod.Bitcast,
                [new Tuple<PrimitiveType, PrimitiveType>(Int32, Byte)] = ConversionMethod.Truncate,
                [new Tuple<PrimitiveType, PrimitiveType>(Int32, Boolean)] = ConversionMethod.Truncate,
                [new Tuple<PrimitiveType, PrimitiveType>(Int32, Char)] = ConversionMethod.Truncate,
                [new Tuple<PrimitiveType, PrimitiveType>(Int32, UInt16)] = ConversionMethod.Truncate,
                [new Tuple<PrimitiveType, PrimitiveType>(Int32, IntPtr)] = ConversionMethod.IntToPtr,
                [new Tuple<PrimitiveType, PrimitiveType>(Int32, Int64)] = ConversionMethod.SignExtend,
                [new Tuple<PrimitiveType, PrimitiveType>(UInt32, Int32)] = ConversionMethod.Bitcast,
                [new Tuple<PrimitiveType, PrimitiveType>(UInt32, UIntPtr)] = ConversionMethod.IntToPtr,
                [new Tuple<PrimitiveType, PrimitiveType>(Int64, UInt64)] = ConversionMethod.Bitcast,
                [new Tuple<PrimitiveType, PrimitiveType>(Int64, UIntPtr)] = ConversionMethod.IntToPtr,
                [new Tuple<PrimitiveType, PrimitiveType>(IntPtr, UIntPtr)] = ConversionMethod.Bitcast
            };

        private static readonly Dictionary<Tuple<PrimitiveType, PrimitiveType>, PrimitiveType> MostComplex =
            new Dictionary<Tuple<PrimitiveType, PrimitiveType>, PrimitiveType>
            {
                [new Tuple<PrimitiveType, PrimitiveType>(Boolean, Int32)] = Int32,
                [new Tuple<PrimitiveType, PrimitiveType>(Char, Int32)] = Int32
            };

        public string MangledName => Name;
        public string Name { get; }
        public bool IsAllowedOnStack => true;

        private PrimitiveType(string name)
        {
            Name = name;
        }

        public IType GetLocalReferenceType()
        {
            return this;
        }

        public LLVMTypeRef GetType(CodeGenerator codeGenerator)
        {
            if (this == Void)
                return LLVM.VoidTypeInContext(codeGenerator.Context);
            if (this == Boolean)
                return LLVM.Int1TypeInContext(codeGenerator.Context);
            if (this == Byte || this == SByte)
                return LLVM.Int8TypeInContext(codeGenerator.Context);
            if (this == Char || this == Int16 || this == UInt16)
                return LLVM.Int16TypeInContext(codeGenerator.Context);
            if (this == Int32 || this == UInt32)
                return LLVM.Int32TypeInContext(codeGenerator.Context);
            if (this == Int64 || this == UInt64)
                return LLVM.Int64TypeInContext(codeGenerator.Context);
            if (this == Single)
                return LLVM.FloatTypeInContext(codeGenerator.Context);
            if (this == Double)
                return LLVM.DoubleTypeInContext(codeGenerator.Context);
            if (this == IntPtr || this == UIntPtr)
                return LLVM.PointerType(LLVM.Int8TypeInContext(codeGenerator.Context), 0);
            throw new ArgumentException("Unexpected type " + this);
        }

        public LLVMValueRef GenerateConstantValue(CodeGenerator codeGenerator, object value)
        {
            LLVMTypeRef type = GetType(codeGenerator);
            if (this == Void)
                throw new NotSupportedException("Unable to generate a constant for void type");
            if (this == Boolean)
                return LLVM.ConstInt(type, (ulong) ((bool) value ? 1 : 0), false);
            if (this == Byte)
                return LLVM.ConstInt(type, (byte) value, false);
            if (this == SByte)
                return LLVM.ConstInt(type, (ulong) (sbyte) value, false);
            if (this == Char)
                throw new NotImplementedException("Char constants not implemented");
            if (this == Int16)
                return LLVM.ConstInt(type, (ulong) (short) value, true);
            if (this == UInt16)
                return LLVM.ConstInt(type, (ushort) value, false);
            if (this == Int32)
                return LLVM.ConstInt(type, (ulong) (int) value, true);
            if (this == UInt32)
                return LLVM.ConstInt(type, (uint) value, false);
            if (this == Int64)
                return LLVM.ConstInt(type, (ulong) (long) value, true);
            if (this == UInt64)
                return LLVM.ConstInt(type, (ulong) value, false);
            if (this == Single)
                return LLVM.ConstReal(type, (float) value);
            if (this == Double)
                return LLVM.ConstReal(type, (double) value);
            if (this == IntPtr || this == UIntPtr)
                throw new NotImplementedException("IntPtr/UIntPtr constants not implemented");
            throw new ArgumentException("Unexpected type " + this);
        }

        public LLVMValueRef? ConvertTo(CodeGenerator codeGenerator, IType type, LLVMValueRef value)
        {
            if (type is PrimitiveType)
            {
                Tuple<PrimitiveType, PrimitiveType> conversion =
                    new Tuple<PrimitiveType, PrimitiveType>(this, (PrimitiveType) type);
                if (!Conversions.ContainsKey(conversion))
                    return null;
                switch (Conversions[conversion])
                {
                    case ConversionMethod.Bitcast:
                        return LLVM.BuildBitCast(codeGenerator.Builder, value, type.GetType(codeGenerator), "");
                    case ConversionMethod.ZeroExtend:
                        return LLVM.BuildZExt(codeGenerator.Builder, value, type.GetType(codeGenerator), "");
                    case ConversionMethod.SignExtend:
                        return LLVM.BuildSExt(codeGenerator.Builder, value, type.GetType(codeGenerator), "");
                    case ConversionMethod.Truncate:
                        return LLVM.BuildTrunc(codeGenerator.Builder, value, type.GetType(codeGenerator), "");
                    case ConversionMethod.IntToPtr:
                        return LLVM.BuildIntToPtr(codeGenerator.Builder, value, type.GetType(codeGenerator), "");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return null;
        }

        public LLVMValueRef? ConvertFrom(CodeGenerator codeGenerator, IType type, LLVMValueRef value)
        {
            if (type is PrimitiveType)
            {
                Tuple<PrimitiveType, PrimitiveType> conversion =
                    new Tuple<PrimitiveType, PrimitiveType>((PrimitiveType) type, this);
                if (!Conversions.ContainsKey(conversion))
                    return null;
                switch (Conversions[conversion])
                {
                    case ConversionMethod.Bitcast:
                        return LLVM.BuildBitCast(codeGenerator.Builder, value, GetType(codeGenerator), "");
                    case ConversionMethod.ZeroExtend:
                        return LLVM.BuildZExt(codeGenerator.Builder, value, GetType(codeGenerator), "");
                    case ConversionMethod.SignExtend:
                        return LLVM.BuildSExt(codeGenerator.Builder, value, GetType(codeGenerator), "");
                    case ConversionMethod.Truncate:
                        return LLVM.BuildTrunc(codeGenerator.Builder, value, GetType(codeGenerator), "");
                    case ConversionMethod.IntToPtr:
                        return LLVM.BuildIntToPtr(codeGenerator.Builder, value, GetType(codeGenerator), "");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return null;
        }

        public IType GetMostComplexType(IType other)
        {
            if (other is PrimitiveType)
            {
                PrimitiveType result;
                if (MostComplex.TryGetValue(new Tuple<PrimitiveType, PrimitiveType>((PrimitiveType) other, this),
                    out result))
                    return result;
                if (MostComplex.TryGetValue(new Tuple<PrimitiveType, PrimitiveType>(this, (PrimitiveType) other),
                    out result))
                    return result;
                throw new NotImplementedException($"Common complex type between {this} and {other} not set yet");
            }
            throw new NotSupportedException($"No common complex type between {this} and {other}");
        }

        public IField ResolveField(FieldReference field)
        {
            throw new NotImplementedException($"Primitive types have no supported fields, {field.Name}");
        }

        public LLVMValueRef GetFieldAddress(CodeGenerator codeGenerator, LLVMValueRef objectPtr, IField field)
        {
            throw new NotImplementedException($"Primitive types have no supported fields, {field.Name}");
        }

        public IMethod ResolveMethod(MethodReference method)
        {
            throw new NotImplementedException($"Primitive types have no supported fields, {method.Name}");
        }

        public void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator)
        {
            throw new NotImplementedException();
        }
    }
}