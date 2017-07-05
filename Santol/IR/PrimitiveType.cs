using System;
using LLVMSharp;
using Santol.Generator;

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

        public string MangledName => Name;
        public string Name { get; }

        private PrimitiveType(string name)
        {
            Name = name;
        }

        public LLVMTypeRef GetType(CodeGenerator codeGenerator)
        {
            if (this == Void)
                return LLVM.VoidTypeInContext(codeGenerator.Context);
            else if (this == Boolean)
                return LLVM.Int1TypeInContext(codeGenerator.Context);
            else if (this == Byte || this == SByte)
                return LLVM.Int8TypeInContext(codeGenerator.Context);
            else if (this == Char || this == Int16 || this == UInt16)
                return LLVM.Int16TypeInContext(codeGenerator.Context);
            else if (this == Int32 || this == UInt32)
                return LLVM.Int32TypeInContext(codeGenerator.Context);
            else if (this == Int64 || this == UInt64)
                return LLVM.Int64TypeInContext(codeGenerator.Context);
            else if (this == Single)
                return LLVM.FloatTypeInContext(codeGenerator.Context);
            else if (this == Double)
                return LLVM.DoubleTypeInContext(codeGenerator.Context);
            else if (this == IntPtr || this == UIntPtr)
                return LLVM.PointerType(LLVM.Int8TypeInContext(codeGenerator.Context), 0);
            else
                throw new ArgumentException("Unexpected type " + this);
        }
    }
}