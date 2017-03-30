using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Santol
{
    public interface IOperation
    {
        TypeReference ResultType { get; }
    }

    public class LoadNullConstant : IOperation
    {
        public TypeReference ResultType => new TypeReference(null, null, null, null, false);
    }

    public class LoadPrimitiveConstant : IOperation
    {
        public TypeReference ResultType { get; }

        public object Value { get; }

        public LoadPrimitiveConstant(TypeReference type, object value)
        {
            ResultType = type;
            Value = value;
        }
    }

    public class StoreLocal : IOperation
    {
        public Mono.Cecil.Cil.VariableDefinition Destination { get; }
        public TypeReference ResultType => null;

        public StoreLocal(Mono.Cecil.Cil.VariableDefinition definition)
        {
            Destination = definition;
        }
    }

    public class StoreDirect : IOperation
    {
        public TypeReference ResultType => null;
        public TypeReference Type { get; }
        public TypeReference SourceType { get; }
        public TypeReference AddressType { get; }

        public StoreDirect(TypeReference type, TypeReference sourceType, TypeReference addressType)
        {
            Type = type;
            SourceType = sourceType;
            AddressType = addressType;
        }
    }

    public class LoadLocal : IOperation
    {
        public int Slot { get; }
        public TypeReference ResultType { get; }

        public LoadLocal(Mono.Cecil.Cil.VariableDefinition definition)
        {
            Slot = definition.Index;
            ResultType = definition.VariableType;
        }
    }

    public class Numeric : IOperation
    {
        public enum Actions
        {
            Add
        }

        public TypeReference Lhs { get; }
        public TypeReference Rhs { get; }
        public TypeReference ResultType { get; }
        public Actions Action { get; }

        public Numeric(Actions action, TypeReference lhs, TypeReference rhs, TypeReference type)
        {
            Action = action;
            Lhs = lhs;
            Rhs = rhs;
            ResultType = type;
        }
    }

    public class Comparison : IOperation
    {
        public enum Actions
        {
            LessThan
        }

        public TypeReference Lhs { get; }
        public TypeReference Rhs { get; }
        public TypeReference ResultType { get; }
        public Actions Action { get; }

        public Comparison(TypeSystem typeSystem, Actions action, TypeReference lhs, TypeReference rhs)
        {
            Action = action;
            Lhs = lhs;
            Rhs = rhs;
            ResultType = typeSystem.Int32;
        }
    }

    public class Convert : IOperation
    {
        public TypeReference SourceType { get; }
        public TypeReference ResultType { get; }

        public Convert(TypeReference source, TypeReference type)
        {
            SourceType = source;
            ResultType = type;
        }
    }

    public class Branch : IOperation
    {
        public TypeReference ResultType => null;
        public CodeSegment Segment { get; }

        public Branch(CodeSegment segment)
        {
            Segment = segment;
        }
    }
}