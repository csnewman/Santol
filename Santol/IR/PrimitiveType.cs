using System;

namespace Santol.IR
{
    public class PrimitiveType : IType
    {
        public static readonly PrimitiveType Void = new PrimitiveType("void");
        public static readonly PrimitiveType Boolean = new PrimitiveType("bool");
        public static readonly PrimitiveType Int32 = new PrimitiveType("int32");
        public static readonly PrimitiveType UInt32 = new PrimitiveType("uint32");
        public static readonly PrimitiveType Int16 = new PrimitiveType("int16");
        public static readonly PrimitiveType UInt16 = new PrimitiveType("uint16");

        public string MangledName => Name;

        public string Name { get; }

        private PrimitiveType(string name)
        {
            Name = name;
        }
    }
}