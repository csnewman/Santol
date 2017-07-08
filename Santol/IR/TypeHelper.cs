using System;
using Mono.Cecil;

namespace Santol.IR
{
    public static class TypeHelper
    {
        public static IType GetSimplestType(IType t1, IType t2)
        {
            if (t1 == PrimitiveType.Boolean && t2 == PrimitiveType.Int32)
                return PrimitiveType.Boolean;
            throw new NotImplementedException("Proper simplest type finding not implemented");
        }
    }
}