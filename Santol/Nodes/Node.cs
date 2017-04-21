using System;
using System.Collections.Generic;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public abstract class Node
    {
        protected readonly Compiler Compiler;
        protected CodeGenerator CodeGenerator => Compiler.CodeGenerator;
        private readonly IList<NodeReference> _references;
        private Node _replacement;
        private LLVMValueRef _llvmRef;
        public abstract bool HasResult { get; }
        public abstract TypeReference ResultType { get; }

        protected Node(Compiler compiler)
        {
            Compiler = compiler;
            _references = new List<NodeReference>();
        }

        public NodeReference TakeReference()
        {
            if (_replacement != null)
            {
                Console.WriteLine("Tried to take reference to replace node!");
                return _replacement.TakeReference();
            }

            NodeReference @ref = new NodeReference(this);
            _references.Add(@ref);
            return @ref;
        }

        public void Replace(Node with)
        {
            _replacement = with;
            foreach (NodeReference nodeReference in _references)
                nodeReference.Update(with);
        }

        public abstract void Generate(FunctionGenerator fgen);

        protected void SetLlvmRef(TypeReference from, LLVMValueRef @ref)
        {
            _llvmRef = CodeGenerator.GenerateConversion(from, ResultType, @ref);
        }

        protected void SetLlvmRef(LLVMValueRef @ref)
        {
            _llvmRef = @ref;
        }

        public LLVMValueRef GetLlvmRef(TypeReference target)
        {
            return CodeGenerator.GenerateConversion(ResultType, target, _llvmRef);
        }

        public LLVMValueRef GetLlvmRef()
        {
            return _llvmRef;
        }

        public abstract string ToFullString();
    }

    public class NodeReference
    {
        public Node Node { get; private set; }
        public TypeReference ResultType => Node.ResultType;

        public NodeReference(Node node)
        {
            Node = node;
        }

        public void Update(Node node)
        {
            Node = node;
        }

        public LLVMValueRef GetLlvmRef(TypeReference target) => Node.GetLlvmRef(target);

        public LLVMValueRef GetLlvmRef() => Node.GetLlvmRef();
    }
}