using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Mono.Cecil;

namespace Santol.Generator
{
    public class StackBuilder
    {
        private CodeGenerator _cgen;
        private FunctionGenerator _fgen;
        private Stack<LLVMValueRef> _stack;

        public StackBuilder(CodeGenerator cgen, FunctionGenerator fgen)
        {
            _cgen = cgen;
            _fgen = fgen;
            _stack = new Stack<LLVMValueRef>();
        }

        public void Push(LLVMValueRef @ref)
        {
            _stack.Push(@ref);
        }

        public void PushConverted(LLVMValueRef @ref, TypeReference from, TypeReference to)
        {
            _stack.Push(_cgen.GenerateConversion(from, to, @ref));
        }

        public LLVMValueRef Pop()
        {
            return _stack.Pop();
        }

        public LLVMValueRef PopConverted(TypeReference from, TypeReference to)
        {
            return _cgen.GenerateConversion(from, to, Pop());
        }
    }
}