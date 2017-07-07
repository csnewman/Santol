using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;
using Santol.IR;
using Santol.Loader;

namespace Santol.Nodes
{
    public class LoadField : Node
    {
        public NodeReference Object { get; }
        public IField Field { get; }
        public override bool HasResult => true;
        public override IType ResultType => Field.Type;

        public LoadField(NodeReference @object, IField field)
        {
            Object = @object;
            Field = field;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            SetRef(fgen.LoadDirect(Field.Type.GetFieldAddress(codeGenerator, Object.GetRef(), Field)));
        }

        public override string ToFullString() => $"LoadField [Object: {Object}, Field: {Field}]";
    }
}