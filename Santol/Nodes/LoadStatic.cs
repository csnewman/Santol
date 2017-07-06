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
    public class LoadStatic : Node
    {
        public StaticField Field { get; }
        public override bool HasResult => true;
        public override IType ResultType => Field.Type;

        public LoadStatic(StaticField field)
        {
            Field = field;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            SetRef(fgen.LoadDirect(Field.GetFieldAddress(codeGenerator)));
        }

        public override string ToFullString() => $"LoadLocal [Field: {Field}, Type: {ResultType}]";
    }
}