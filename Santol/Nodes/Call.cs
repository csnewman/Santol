using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class Call : Node
    {
        public MethodReference Method { get; }
        public NodeReference[] Arguments { get; }
        public override bool HasResult => Method.ReturnType.MetadataType != MetadataType.Void;
        public override TypeReference ResultType => Method.ReturnType;

        public Call(Compiler compiler, MethodReference method, NodeReference[] arguments) : base(compiler)
        {
            Method = method;
            Arguments = arguments;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            Console.WriteLine("WE " + ToFullString());
            foreach (NodeReference nodeReference in Arguments)
            {
                Console.WriteLine(nodeReference.ResultType + "   " + LLVM.TypeOf(nodeReference.GetRef()));
            }

            if (Method.Parameters.Count + (Method.ImplicitThis() ? 1 : 0) != Arguments.Length)
                throw new ArgumentException("Incorrect number of arguments!");

            IList<LLVMValueRef> args = new List<LLVMValueRef>();
            if (Method.ImplicitThis())
                args.Add(Arguments[0].GetLlvmRef(Method.DeclaringType));

            for (int i = 0; i < Method.Parameters.Count; i++)
                args.Add(Arguments[(Method.ImplicitThis() ? 1 : 0) + i].GetLlvmRef(Method.Parameters[i].ParameterType));
            
            LLVMValueRef? val = fgen.GenerateCall(Method, args.ToArray());
            if (val.HasValue)
                SetLlvmRef(val.Value);
        }

        public override string ToFullString()
            =>
                $"Call [Args: [{string.Join<NodeReference>(", ", Arguments)}], Method: {Method}, Result: {ResultType}]";
    }
}