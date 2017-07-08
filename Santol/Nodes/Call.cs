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
            Console.WriteLine("Generating call " + ToFullString());
            foreach (NodeReference nodeReference in Arguments)
            {
                Console.WriteLine(nodeReference.ResultType + "   " + LLVM.TypeOf(nodeReference.GetRef()));
            }

            if (Method.Arguments.Length + (Method.ImplicitThis ? 1 : 0) != Arguments.Length)
                throw new ArgumentException("Incorrect number of arguments!");

            IList<LLVMValueRef> args = new List<LLVMValueRef>();
            if (Method.ImplicitThis)
                args.Add(Arguments[0].GetRef(codeGenerator, Method.Parent));

            for (int i = 0; i < Method.Arguments.Length; i++)
                args.Add(Arguments[(Method.ImplicitThis ? 1 : 0) + i].GetRef(codeGenerator, Method.Arguments[i]));

            LLVMValueRef? val = Method.GenerateCall(args.ToArray());
            if (val.HasValue)
                SetRef(val.Value);
        }

        public override string ToFullString()
            =>
                $"Call [Args: [{string.Join<NodeReference>(", ", Arguments)}], Method: {Method}, Result: {ResultType}]";
    }
}