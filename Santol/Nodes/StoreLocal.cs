using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Santol.Generator;
using Santol.IR;
using Santol.Nodes;

namespace Santol.Nodes
{
    public class StoreLocal : Node
    {
        public NodeReference Value { get; }
        public IType FieldType { get; }
        public int FieldIndex { get; }
        public override bool HasResult => false;
        public override IType ResultType => null;

        public StoreLocal(IType type, int index, NodeReference value)
        {
            FieldType = type;
            FieldIndex = index;
            Value = value;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            fgen.StoreLocal(FieldIndex, Value.GetRef(codeGenerator, FieldType));
        }

        public override string ToFullString() => $"StoreLocal [Value: {Value}, Index: {FieldIndex}, Type: {FieldType}]";
    }
}