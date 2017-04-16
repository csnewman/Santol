using System;
using System.Collections.Generic;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public abstract class Node
    {
        private readonly IList<NodeReference> _references;
        private LLVMValueRef _llvmRef;
        public abstract bool HasResult { get; }
        public abstract TypeReference ResultType { get; }

        protected Node()
        {
            _references = new List<NodeReference>();
        }

        public NodeReference TakeReference()
        {
            NodeReference @ref = new NodeReference(this);
            _references.Add(@ref);
            return @ref;
        }

        public abstract void Generate(CodeGenerator cgen, FunctionGenerator fgen);

        protected void SetLlvmRef(CodeGenerator cgen, TypeReference from, LLVMValueRef @ref)
        {
            _llvmRef = cgen.GenerateConversion(from, ResultType, @ref);
        }

        protected void SetLlvmRef(LLVMValueRef @ref)
        {
            _llvmRef = @ref;
        }

        public LLVMValueRef GetLlvmRef(CodeGenerator cgen, TypeReference target)
        {
            return cgen.GenerateConversion(ResultType, target, _llvmRef);
        }

        public LLVMValueRef GetLlvmRef()
        {
            return _llvmRef;
        }
        
        public abstract string ToFullString();
    }

    public class NodeReference
    {
        public Node Node { get; }
        public TypeReference ResultType => Node.ResultType;

        public NodeReference(Node node)
        {
            Node = node;
        }

        public LLVMValueRef GetLlvmRef(CodeGenerator cgen, TypeReference target) => Node.GetLlvmRef(cgen, target);

        public LLVMValueRef GetLlvmRef() => Node.GetLlvmRef();
    }
}