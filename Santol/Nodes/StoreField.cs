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
    public class StoreField : Node
    {
        public NodeReference Object { get; }
        public IField Field { get; }
        public NodeReference Value { get; }
        public override bool HasResult => false;
        public override IType ResultType => null;

        public StoreField(NodeReference @object, IField field, NodeReference value)
        {
            Object = @object;
            Field = field;
            Value = value;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            fgen.StoreDirect(Value.GetRef(codeGenerator, Field.Type),
                Field.Parent.GetFieldAddress(codeGenerator, Object.GetRef(), Field));
        }

        public override string ToFullString()
            => $"StoreField [Oject: {Object}, Field: {Field}]";
    }
}