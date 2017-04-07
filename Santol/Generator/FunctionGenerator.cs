using System;
using System.Collections.Generic;
using LLVMSharp;
using Mono.Cecil;
using Santol.CIL;
using Santol.Operations;

namespace Santol.Generator
{
    public class FunctionGenerator
    {
        private readonly CodeGenerator _cgen;
        public Mono.Cecil.MethodDefinition Definition;
        public LLVMValueRef FunctionRef { get; }
        public LLVMValueRef[] Locals { get; set; }
        private readonly IDictionary<string, LLVMBasicBlockRef> _namedBlocks;
        private readonly IDictionary<string, LLVMValueRef[]> _blockPhis;
        private readonly IDictionary<string, StackBuilder> _blockStackBuilders;

        public string CurrentBlock { get; private set; }
        public LLVMValueRef[] CurrentPhis => _blockPhis[CurrentBlock];
        public StackBuilder CurrentStackBuilder => _blockStackBuilders[CurrentBlock];

        public FunctionGenerator(CodeGenerator cgen, Mono.Cecil.MethodDefinition definition, LLVMValueRef functionRef)
        {
            _cgen = cgen;
            Definition = definition;
            FunctionRef = functionRef;
            _namedBlocks = new Dictionary<string, LLVMBasicBlockRef>();
            _blockPhis = new Dictionary<string, LLVMValueRef[]>();
            _blockStackBuilders = new Dictionary<string, StackBuilder>();
        }

        public void CreateBlock(string name, LLVMTypeRef[] incomings)
        {
            _namedBlocks[name] = LLVM.AppendBasicBlock(FunctionRef, name);
            SelectBlock(name);

            StackBuilder builder = new StackBuilder(_cgen, this);
            LLVMValueRef[] phis = new LLVMValueRef[incomings?.Length ?? 0];
            for (int i = 0; i < phis.Length; i++)
            {
                LLVMValueRef phi = LLVM.BuildPhi(_cgen.Builder, incomings[i], "in_" + i);
                phis[i] = phi;
                builder.Push(phi);
            }
            _blockPhis[name] = phis;
            _blockStackBuilders[name] = builder;
        }

        public void CreateBlock(CodeSegment segment, LLVMTypeRef[] incomings)
        {
            CreateBlock(segment.Name, incomings);
        }

        public void SelectBlock(string name)
        {
            LLVM.PositionBuilderAtEnd(_cgen.Builder, _namedBlocks[name]);
            CurrentBlock = name;
        }

        public void SelectBlock(CodeSegment segment)
        {
            SelectBlock(segment.Name);
        }

        public LLVMValueRef[] GetPhis(string block)
        {
            return _blockPhis[block];
        }

        public LLVMValueRef[] GetPhis(CodeSegment segment)
        {
            return GetPhis(segment.Name);
        }

        public LLVMValueRef GetParam(int slot)
        {
            return LLVM.GetParam(FunctionRef, (uint) slot);
        }

        public void Branch(string block, LLVMValueRef[] vals)
        {
            LLVM.BuildBr(_cgen.Builder, _namedBlocks[block]);

            if (vals == null) return;
            LLVMValueRef[] phis = _blockPhis[block];
            LLVMBasicBlockRef blockref = _namedBlocks[CurrentBlock] ;
            for (int i = 0; i < vals.Length; i++)
                LLVM.AddIncoming(phis[i], new[] {vals[i]}, new[] {blockref}, 1);
        }

        public void Branch(CodeSegment segment, LLVMValueRef[] vals)
        {
            Branch(segment.Name, vals);
        }

        public void BranchConditional(LLVMValueRef condition, string block, string elseBlock, LLVMValueRef[] vals)
        {
            LLVM.BuildCondBr(_cgen.Builder, condition, _namedBlocks[block], _namedBlocks[elseBlock]);

            if (vals == null) return;
            LLVMBasicBlockRef blockref = _namedBlocks[CurrentBlock];
            LLVMValueRef[] phis = _blockPhis[block];
            LLVMValueRef[] otherphis = _blockPhis[elseBlock];
            for (int i = 0; i < vals.Length; i++)
            {
                LLVM.AddIncoming(phis[i], new[] {vals[i]}, new[] {blockref}, 1);
                LLVM.AddIncoming(otherphis[i], new[] {vals[i]}, new[] {blockref}, 1);
            }
        }

        public void BranchConditional(LLVMValueRef condition, CodeSegment segment, CodeSegment elseSegment,
            LLVMValueRef[] vals)
        {
            BranchConditional(condition, segment.Name, elseSegment.Name, vals);
        }

        public LLVMValueRef LoadLocal(int index)
        {
            return LLVM.BuildLoad(_cgen.Builder, Locals[index], "");
        }

        public void StoreLocal(int index, LLVMValueRef value)
        {
            LLVM.BuildStore(_cgen.Builder, value, Locals[index]);
        }

        public void StoreDirect(LLVMValueRef value, LLVMValueRef address)
        {
            LLVM.BuildStore(_cgen.Builder, value, address);
        }

        public LLVMValueRef AddInts(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildAdd(_cgen.Builder, v1, v2, "");
        }

        public LLVMValueRef CompareInts(LLVMIntPredicate op, LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildICmp(_cgen.Builder, op, v1, v2, "");
        }

        public LLVMValueRef? GenerateCall(Mono.Cecil.MethodDefinition method, TypeReference[] argTypes,
            LLVMValueRef[] args)
        {
            if (method.HasThis)
                throw new NotImplementedException("Instance methods not supported");

            LLVMValueRef func = _cgen.GetFunction(method).FunctionRef;

            LLVMValueRef[] convArgs = new LLVMValueRef[args.Length];
            for (int i = 0; i < args.Length; i++)
                convArgs[i] = _cgen.GenerateConversion(argTypes[i], method.Parameters[i].ParameterType, args[i]);

            if (method.ReturnType.MetadataType != MetadataType.Void)
                return LLVM.BuildCall(_cgen.Builder, func, convArgs, "");

            LLVM.BuildCall(_cgen.Builder, func, convArgs, "");
            return null;
        }

        public void Return(LLVMValueRef? val)
        {
            if (val.HasValue)
                LLVM.BuildRet(_cgen.Builder, val.Value);
            else
                LLVM.BuildRetVoid(_cgen.Builder);
        }
    }
}