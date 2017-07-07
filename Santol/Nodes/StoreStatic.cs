using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class StoreStatic : Node
    {
        public NodeReference Value { get; }
        public StaticField Field { get; }
        public override bool HasResult => false;
        public override IType ResultType => null;

        public StoreStatic(StaticField field, NodeReference value)
        {
            Field = field;
            Value = value;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            fgen.StoreDirect(Value.GetRef(codeGenerator, Field.Type), Field.GetFieldAddress(codeGenerator));
        }

        public override string ToFullString() => $"StoreStatic [Value: {Value}, Field: {Field}]";
    }
}