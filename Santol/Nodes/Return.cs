using System;
using System.CodeDom;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class Return : Node
    {
        public bool HasValue => Value != null;
        public NodeReference Value { get; }
        public override bool HasResult => false;
        public override IType ResultType => null;

        public Return(NodeReference value) 
        {
            Value = value;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            fgen.Return(HasValue ? (LLVMValueRef?) Value.GetRef(codeGenerator, fgen.Method.ReturnType) : null);
        }

        public override string ToFullString() => $"Return [HasValue: {HasValue}, Value: {Value}]";
    }
}