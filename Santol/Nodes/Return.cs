using System;
using System.CodeDom;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class Return : Node
    {
        public bool HasValue => Value != null;
        public NodeReference Value { get; }
        public override bool HasResult => false;
        public override TypeReference ResultType => null;

        public Return(Compiler compiler, NodeReference value) : base(compiler)
        {
            Value = value;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            fgen.Return(HasValue ? (LLVMValueRef?) Value.GetLlvmRef(fgen.Definition.ReturnType) : null);
        }

        public override string ToFullString() => $"Return [HasValue: {HasValue}, Value: {Value}]";
    }
}