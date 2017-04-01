using Mono.Cecil;

namespace Santol
{
    public interface IOperation
    {
        TypeReference ResultType { get; }

        string ToFullString();
    }

    public class LoadNullConstant : IOperation
    {
        public TypeReference ResultType => new TypeReference(null, null, null, null, false);

        public string ToFullString() => "Null";
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

        public string ToFullString() => $"LoadPrimitiveConstant [Type: {ResultType}, Value: {Value}]";
    }

    public class StoreLocal : IOperation
    {
        public TypeReference SourceType { get; }
        public Mono.Cecil.Cil.VariableDefinition Destination { get; }
        public TypeReference ResultType => null;

        public StoreLocal(Mono.Cecil.Cil.VariableDefinition definition, TypeReference type)
        {
            SourceType = type;
            Destination = definition;
        }

        public string ToFullString() => $"StoreLocal [Source Type: {SourceType}, Destination: {Destination}]";
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

        public string ToFullString()
            => $"StoreDirect [Source Type: {SourceType}, Address Type: {AddressType}, Type: {Type}]";
    }

    public class LoadLocal : IOperation
    {
        public Mono.Cecil.Cil.VariableDefinition Variable { get; }
        public TypeReference ResultType => Variable.VariableType;

        public LoadLocal(Mono.Cecil.Cil.VariableDefinition definition)
        {
            Variable = definition;
        }

        public string ToFullString() => $"LoadLocal [Variable: {Variable}, Type: {ResultType}]";
    }

    public class LoadArg : IOperation
    {
        public ParameterDefinition Parameter { get; }
        public int Slot => Parameter.Index;
        public TypeReference ResultType => Parameter.ParameterType;

        public LoadArg(ParameterDefinition definition)
        {
            Parameter = definition;
        }

        public string ToFullString() => $"LoadArg [Slot: {Slot}, Parameter: {Parameter}, Type: {ResultType}]";
    }

    public class Numeric : IOperation
    {
        public enum Operations
        {
            Add
        }

        public TypeReference Lhs { get; }
        public TypeReference Rhs { get; }
        public TypeReference ResultType { get; }
        public Operations Operation { get; }

        public Numeric(Operations operation, TypeReference lhs, TypeReference rhs, TypeReference type)
        {
            Operation = operation;
            Lhs = lhs;
            Rhs = rhs;
            ResultType = type;
        }

        public string ToFullString()
            => $"Numeric [Operation: {Operation}, LHS: {Lhs}, RHS: {Rhs}, Result: {ResultType}]";
    }

    public class Comparison : IOperation
    {
        public enum Operations
        {
            LessThan
        }

        public TypeReference Lhs { get; }
        public TypeReference Rhs { get; }
        public TypeReference ResultType { get; }
        public Operations Operation { get; }

        public Comparison(TypeSystem typeSystem, Operations operation, TypeReference lhs, TypeReference rhs)
        {
            Operation = operation;
            Lhs = lhs;
            Rhs = rhs;
            ResultType = typeSystem.Int32;
        }

        public string ToFullString()
            => $"Comparison [Operation: {Operation}, LHS: {Lhs}, RHS: {Rhs}, Result: {ResultType}]";
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

        public string ToFullString() => $"Convert [Source: {SourceType}, Target: {ResultType}]";
    }

    public class Branch : IOperation
    {
        public CodeSegment Segment { get; }
        public TypeReference ResultType => null;

        public Branch(CodeSegment segment)
        {
            Segment = segment;
        }

        public string ToFullString() => $"Branch [Target: {Segment.Name}]";
    }

    public class ConditionalBranch : IOperation
    {
        public enum Types
        {
            True,
            False
        }

        public CodeSegment Segment { get; }
        public CodeSegment ElseSegment { get; }
        public Types Type { get; }
        public TypeReference SourceType { get; }
        public TypeReference ResultType => null;


        public ConditionalBranch(CodeSegment segment, CodeSegment elseSegment, Types type, TypeReference sourceType)
        {
            Segment = segment;
            ElseSegment = elseSegment;
            Type = type;
            SourceType = sourceType;
        }

        public string ToFullString()
            => $"ConditionalBranch [Type: {Type}, Source Type: {SourceType}, Target: {Segment.Name}, Else {ElseSegment.Name}]";
    }

    public class Call : IOperation
    {
        public Mono.Cecil.MethodDefinition Definition;
        public TypeReference[] ArgTypes { get; }
        public TypeReference ResultType => Definition.ReturnType;

        public Call(Mono.Cecil.MethodDefinition definition, TypeReference[] argTypes)
        {
            Definition = definition;
            ArgTypes = argTypes;
        }

        public string ToFullString()
            =>
                $"Call [Args: [{string.Join<TypeReference>(", ", ArgTypes)}], Method: {Definition}, Result: {ResultType}]";
    }

    public class Return : IOperation
    {
        public bool HasValue => ValueType != null;
        public TypeReference ValueType { get; }
        public TypeReference ResultType => null;

        public Return(TypeReference valueType)
        {
            ValueType = valueType;
        }

        public string ToFullString() => $"Return [HasValue: {HasValue}, Value: {ValueType}]";
    }
}