using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class Call : Node
    {
        public IMethod Method { get; }
        public NodeReference[] Arguments { get; }
        public override bool HasResult => Method.ReturnType != PrimitiveType.Void;
        public override IType ResultType => Method.ReturnType;

        public Call(IMethod method, NodeReference[] arguments)
        {
            Method = method;
            Arguments = arguments;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            if (Method.Arguments.Length != Arguments.Length)
                throw new ArgumentException("Incorrect number of arguments!");

            LLVMValueRef[] args = new LLVMValueRef[Method.Arguments.Length];

            for (int i = 0; i < Method.Arguments.Length; i++)
                args[i] = Arguments[i].GetRef(codeGenerator, Method.Arguments[i]);

            LLVMValueRef? val = Method.GenerateCall(codeGenerator, args.ToArray());
            if (val.HasValue)
                SetRef(val.Value);
        }

        public override string ToFullString()
            =>
                $"Call [Args: [{string.Join<NodeReference>(", ", Arguments)}], Method: {Method}, Result: {ResultType}]";
    }
}