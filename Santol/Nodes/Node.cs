using System;
using System.Collections.Generic;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public abstract class Node
    {
        private readonly IList<NodeReference> _references;
        private Node _replacement;
        private LLVMValueRef _llvmRef;
        public abstract bool HasResult { get; }
        public abstract IType ResultType { get; }

        protected Node()
        {
            _references = new List<NodeReference>();
        }

        public NodeReference TakeReference()
        {
            if (_replacement != null)
                return _replacement.TakeReference();

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

        public abstract void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen);

        protected void SetRef(CodeGenerator codeGenerator, IType from, LLVMValueRef @ref)
        {
            _llvmRef = codeGenerator.GenerateConversion(from, ResultType, @ref);
        }

        protected void SetRef(LLVMValueRef @ref)
        {
            _llvmRef = @ref;
        }

        public LLVMValueRef GetRef(CodeGenerator codeGenerator, IType target)
        {
            return codeGenerator.GenerateConversion(ResultType, target, _llvmRef);
        }

        public LLVMValueRef GetRef()
        {
            return _llvmRef;
        }

        public abstract string ToFullString();
    }
}