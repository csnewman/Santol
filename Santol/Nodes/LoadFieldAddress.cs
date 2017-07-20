using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class LoadFieldAddress : Node
    {
        public NodeReference Object { get; }
        public IField Field { get; }
        public override bool HasResult => true;
        public override IType ResultType => new PointerType(Field.Type);

        public LoadFieldAddress(NodeReference @object, IField field)
        {
            Object = @object;
            Field = field;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            SetRef(Field.Parent.GetFieldAddress(codeGenerator, Object.GetRef(), Field));
        }

        public override string ToFullString() => $"LoadFieldAddress [Object: {Object}, Field: {Field}]";
    }
}