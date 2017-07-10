using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using Mono.Cecil;
using Santol.IR;
using Santol.Loader;

namespace Santol.Generator
{
    public class FunctionGenerator
    {
        private readonly CodeGenerator _codeGenerator;
        public IMethod Method { get; }
        public LLVMValueRef FunctionRef { get; }
        public LLVMValueRef[] Locals { get; set; }
        private readonly IDictionary<string, LLVMBasicBlockRef> _namedBlocks;
        private readonly IDictionary<string, LLVMValueRef[]> _blockPhis;

        public string CurrentBlock { get; private set; }
        public LLVMValueRef[] CurrentPhis => _blockPhis[CurrentBlock];

        public FunctionGenerator(CodeGenerator codeGenerator, IMethod method, LLVMValueRef functionRef)
        {
            _codeGenerator = codeGenerator;
            Method = method;
            FunctionRef = functionRef;
            _namedBlocks = new Dictionary<string, LLVMBasicBlockRef>();
            _blockPhis = new Dictionary<string, LLVMValueRef[]>();
        }

        public void CreateBlock(Block block, CodeGenerator codeGenerator)
        {
            CreateBlock(block.Name,
                block.HasIncoming
                    ? block.IncomingTypes.Select(type => type.GetType(codeGenerator)).ToArray()
                    : new LLVMTypeRef[0]);
        }

        public void CreateBlock(string name, LLVMTypeRef[] incomings)
        {
            _namedBlocks[name] = LLVM.AppendBasicBlock(FunctionRef, name);
            SelectBlock(name);

            LLVMValueRef[] phis = new LLVMValueRef[incomings?.Length ?? 0];
            for (int i = 0; i < phis.Length; i++)
            {
                LLVMValueRef phi = LLVM.BuildPhi(_codeGenerator.Builder, incomings[i], "in_" + i);
                phis[i] = phi;
            }
            _blockPhis[name] = phis;
        }

        public void SelectBlock(string name)
        {
            LLVM.PositionBuilderAtEnd(_codeGenerator.Builder, _namedBlocks[name]);
            CurrentBlock = name;
        }

        public void SelectBlock(Block block)
        {
            SelectBlock(block.Name);
        }

        public LLVMValueRef[] GetPhis(string block)
        {
            return _blockPhis[block];
        }

        public LLVMValueRef[] GetPhis(Block block)
        {
            return GetPhis(block.Name);
        }

        public LLVMValueRef GetArgument(int slot)
        {
            return LLVM.GetParam(FunctionRef, (uint) slot);
        }

        public void Branch(Block block, LLVMValueRef[] vals)
        {
            Branch(block.Name, vals);
        }

        public void Branch(string block, LLVMValueRef[] vals)
        {
            LLVM.BuildBr(_codeGenerator.Builder, _namedBlocks[block]);

            if (vals == null) return;
            LLVMValueRef[] phis = _blockPhis[block];
            LLVMBasicBlockRef blockref = _namedBlocks[CurrentBlock];
            for (int i = 0; i < vals.Length; i++)
                LLVM.AddIncoming(phis[i], new[] {vals[i]}, new[] {blockref}, 1);
        }

        public void BranchConditional(LLVMValueRef condition, string block, string elseBlock, LLVMValueRef[] vals)
        {
            LLVM.BuildCondBr(_codeGenerator.Builder, condition, _namedBlocks[block], _namedBlocks[elseBlock]);

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

        public void BranchConditional(LLVMValueRef condition, Block target, Block elseTarget,
            LLVMValueRef[] vals)
        {
            BranchConditional(condition, target.Name, elseTarget.Name, vals);
        }

        public LLVMValueRef LoadLocal(int index)
        {
            return LLVM.BuildLoad(_codeGenerator.Builder, Locals[index], "");
        }

        public LLVMValueRef LoadDirect(LLVMValueRef address)
        {
            return LLVM.BuildLoad(_codeGenerator.Builder, address, "");
        }

        public void StoreLocal(int index, LLVMValueRef value)
        {
            LLVM.BuildStore(_codeGenerator.Builder, value, Locals[index]);
        }

        public void StoreDirect(LLVMValueRef value, LLVMValueRef address)
        {
            LLVM.BuildStore(_codeGenerator.Builder, value, address);
        }

        public LLVMValueRef GetStructElement(LLVMValueRef address, int index)
        {
            return LLVM.BuildStructGEP(_codeGenerator.Builder, address, (uint) index, "");
        }

        public LLVMValueRef AddInts(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildAdd(_codeGenerator.Builder, v1, v2, "");
        }

        public LLVMValueRef SubtractInts(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildSub(_codeGenerator.Builder, v1, v2, "");
        }

        public LLVMValueRef MultiplyInts(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildMul(_codeGenerator.Builder, v1, v2, "");
        }

        public LLVMValueRef DivideInts(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildSDiv(_codeGenerator.Builder, v1, v2, "");
        }

        public LLVMValueRef RemainderInts(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildSRem(_codeGenerator.Builder, v1, v2, "");
        }

        public LLVMValueRef ShiftLeft(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildShl(_codeGenerator.Builder, v1, v2, "");
        }

        public LLVMValueRef Or(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildOr(_codeGenerator.Builder, v1, v2, "");
        }

        public LLVMValueRef XOr(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildXor(_codeGenerator.Builder, v1, v2, "");
        }

        public LLVMValueRef CompareInts(LLVMIntPredicate op, LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildICmp(_codeGenerator.Builder, op, v1, v2, "");
        }

        public void Return(LLVMValueRef? val)
        {
            if (val.HasValue)
                LLVM.BuildRet(_codeGenerator.Builder, val.Value);
            else
                LLVM.BuildRetVoid(_codeGenerator.Builder);
        }
    }
}