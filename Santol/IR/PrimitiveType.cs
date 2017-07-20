using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;
using Santol.Loader;
using Santol.Nodes;

namespace Santol.IR
{
    public sealed class PrimitiveType : IType
    {
        public static readonly PrimitiveType Void = new PrimitiveType("void", 0, (a, i) => null);
        public static readonly PrimitiveType Boolean = new PrimitiveType("bool", 1, (a, i) => a[i] != 0);
        public static readonly PrimitiveType Byte = new PrimitiveType("byte", 1, (a, i) => a[i]);
        public static readonly PrimitiveType SByte = new PrimitiveType("sbyte", 1, (a, i) => (sbyte) a[i]);
        public static readonly PrimitiveType Char = new PrimitiveType("char", 2, (a, i) => BitConverter.ToChar(a, i));

        public static readonly PrimitiveType Int16 =
            new PrimitiveType("int16", 2, (a, i) => BitConverter.ToInt16(a, i));

        public static readonly PrimitiveType UInt16 =
            new PrimitiveType("uint16", 2, (a, i) => BitConverter.ToUInt16(a, i));

        public static readonly PrimitiveType Int32 =
            new PrimitiveType("int32", 4, (a, i) => BitConverter.ToInt32(a, i));

        public static readonly PrimitiveType UInt32 =
            new PrimitiveType("uint32", 4, (a, i) => BitConverter.ToUInt32(a, i));

        public static readonly PrimitiveType Int64 =
            new PrimitiveType("int64", 8, (a, i) => BitConverter.ToInt64(a, i));

        public static readonly PrimitiveType UInt64 =
            new PrimitiveType("uint64", 8, (a, i) => BitConverter.ToUInt64(a, i));

        public static readonly PrimitiveType Single =
            new PrimitiveType("single", 4, (a, i) => BitConverter.ToSingle(a, i));

        public static readonly PrimitiveType Double =
            new PrimitiveType("double", 8, (a, i) => BitConverter.ToDouble(a, i));

        public static readonly PrimitiveType IntPtr =
            new PrimitiveType("intptr", -1, (a, i) => throw new NotSupportedException());

        public static readonly PrimitiveType UIntPtr =
            new PrimitiveType("uintptr", -1, (a, i) => throw new NotSupportedException());

        private enum ConversionMethod
        {
            Bitcast,
            ZeroExtend,
            SignExtend,
            Truncate,
            IntToPtr,
            PtrToInt
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
                [new Tuple<PrimitiveType, PrimitiveType>(Int64, Int32)] = ConversionMethod.Truncate,
                [new Tuple<PrimitiveType, PrimitiveType>(Int64, UInt64)] = ConversionMethod.Bitcast,
                [new Tuple<PrimitiveType, PrimitiveType>(Int64, UIntPtr)] = ConversionMethod.IntToPtr,
                [new Tuple<PrimitiveType, PrimitiveType>(UInt64, Int64)] = ConversionMethod.Bitcast,
                [new Tuple<PrimitiveType, PrimitiveType>(UInt64, UIntPtr)] = ConversionMethod.IntToPtr,
                [new Tuple<PrimitiveType, PrimitiveType>(IntPtr, UIntPtr)] = ConversionMethod.Bitcast,
                [new Tuple<PrimitiveType, PrimitiveType>(UIntPtr, UInt32)] = ConversionMethod.PtrToInt,
                [new Tuple<PrimitiveType, PrimitiveType>(UIntPtr, UInt64)] = ConversionMethod.PtrToInt
            };

        private static readonly Dictionary<Tuple<PrimitiveType, PrimitiveType>, PrimitiveType> MostComplex =
            new Dictionary<Tuple<PrimitiveType, PrimitiveType>, PrimitiveType>
            {
                [new Tuple<PrimitiveType, PrimitiveType>(Boolean, Int32)] = Int32,
                [new Tuple<PrimitiveType, PrimitiveType>(Char, Int32)] = Int32
            };

        private static readonly Dictionary<PrimitiveType, PrimitiveType[]> CompatiableTypes =
            new Dictionary<PrimitiveType, PrimitiveType[]>
            {
                [Int64] = new[] {UInt64},
                [UInt64] = new[] {Int64}
            };

        public string MangledName => Name;
        public string Name { get; }
        public bool IsAllowedOnStack => true;
        public bool IsPointer => false;
        public TypeInfo TypeInfo => throw new NotImplementedException();
        public int CilElementSize { get; }
        public Func<byte[], int, object> CilElementExtractor { get; }

        private PrimitiveType(string name, int elementSize, Func<byte[], int, object> elementExtractor)
        {
            Name = name;
            CilElementSize = elementSize;
            CilElementExtractor = elementExtractor;
        }

        public IType GetLocalReferenceType()
        {
            return this;
        }

        public IType GetStackType()
        {
            return this;
        }

        public bool IsInHierarchy(IType type)
        {
            throw new NotImplementedException();
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

        public void LoadDefault(CodeGenerator codeGenerator, LLVMValueRef target)
        {
            throw new NotImplementedException();
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
                    case ConversionMethod.PtrToInt:
                        return LLVM.BuildPtrToInt(codeGenerator.Builder, value, type.GetType(codeGenerator), "");
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
                    case ConversionMethod.PtrToInt:
                        return LLVM.BuildPtrToInt(codeGenerator.Builder, value, GetType(codeGenerator), "");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return null;
        }

        public bool IsStackCompatible(IType other)
        {
            return Equals(other) || CompatiableTypes[this].Contains(other);
        }

        public IType GetMostComplexType(IType other)
        {
            if (other.Equals(this))
                return this;
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

        public LLVMValueRef Allocate(CodeGenerator codeGenerator)
        {
            throw new NotImplementedException();
        }

        public LLVMValueRef GetTypeInfoField(CodeGenerator codeGenerator, LLVMValueRef objectPtr)
        {
            throw new NotImplementedException();
        }

        public LLVMValueRef GetFieldAddress(CodeGenerator codeGenerator, LLVMValueRef objectPtr, IField field)
        {
            throw new NotImplementedException($"Primitive types have no supported fields, {field.Name}");
        }

        public LLVMValueRef ExtractField(CodeGenerator codeGenerator, LLVMValueRef objectRef, IField field)
        {
            throw new NotImplementedException();
        }

        public IMethod ResolveMethod(AssemblyLoader assemblyLoader, MethodReference method)
        {
            if (method.Name.Equals("op_Explicit"))
            {
                IType fromType = assemblyLoader.ResolveType(method.Parameters[0].ParameterType);
                IType targetType = assemblyLoader.ResolveType(method.ReturnType);

                if (fromType.Equals(this))
                    return new FakeMethod(targetType, new[] {fromType},
                        (generator, refs) =>
                        {
                            LLVMValueRef? val = ConvertTo(generator, targetType, refs[0]);
                            if (!val.HasValue)
                                throw new NotSupportedException("Failed to convert");
                            return val;
                        }
                    );
                else if (targetType.Equals(this))
                    return new FakeMethod(targetType, new[] {fromType},
                        (generator, refs) =>
                        {
                            LLVMValueRef? val = ConvertFrom(generator, fromType, refs[0]);
                            if (!val.HasValue)
                                throw new NotSupportedException("Failed to convert");
                            return val;
                        }
                    );
                else
                    throw new ArgumentException();
            }
            else
                throw new NotSupportedException($"Unknown primitive method, {method.Name}");
        }

        public IMethod FindMethodImplementation(IMethod method)
        {
            throw new NotImplementedException();
        }

        public void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator)
        {
            throw new NotImplementedException();
        }
    }
}