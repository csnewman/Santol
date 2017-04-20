using System;
using System.Collections.Generic;
using LLVMSharp;
using Mono.Cecil;
using Santol.Loader;

namespace Santol.Generator
{
    public class FunctionGenerator
    {
        private readonly Compiler _compiler;
        private readonly CodeGenerator _cgen;
        public MethodDefinition Definition;
        public LLVMValueRef FunctionRef { get; }
        public LLVMValueRef[] Locals { get; set; }
        private readonly IDictionary<string, LLVMBasicBlockRef> _namedBlocks;
        private readonly IDictionary<string, LLVMValueRef[]> _blockPhis;

        public string CurrentBlock { get; private set; }
        public LLVMValueRef[] CurrentPhis => _blockPhis[CurrentBlock];

        public FunctionGenerator(CodeGenerator cgen, Mono.Cecil.MethodDefinition definition, LLVMValueRef functionRef)
        {
            _compiler = cgen.Compiler;
            _cgen = cgen;
            Definition = definition;
            FunctionRef = functionRef;
            _namedBlocks = new Dictionary<string, LLVMBasicBlockRef>();
            _blockPhis = new Dictionary<string, LLVMValueRef[]>();
        }

        public void CreateBlock(string name, LLVMTypeRef[] incomings)
        {
            _namedBlocks[name] = LLVM.AppendBasicBlock(FunctionRef, name);
            SelectBlock(name);

            LLVMValueRef[] phis = new LLVMValueRef[incomings?.Length ?? 0];
            for (int i = 0; i < phis.Length; i++)
            {
                LLVMValueRef phi = LLVM.BuildPhi(_compiler.Builder, incomings[i], "in_" + i);
                phis[i] = phi;
            }
            _blockPhis[name] = phis;
        }

        public void CreateBlock(CodeSegment segment, LLVMTypeRef[] incomings)
        {
            CreateBlock(segment.Name, incomings);
        }

        public void SelectBlock(string name)
        {
            LLVM.PositionBuilderAtEnd(_compiler.Builder, _namedBlocks[name]);
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
            LLVM.BuildBr(_compiler.Builder, _namedBlocks[block]);

            if (vals == null) return;
            LLVMValueRef[] phis = _blockPhis[block];
            LLVMBasicBlockRef blockref = _namedBlocks[CurrentBlock];
            for (int i = 0; i < vals.Length; i++)
                LLVM.AddIncoming(phis[i], new[] {vals[i]}, new[] {blockref}, 1);
        }

        public void Branch(CodeSegment segment, LLVMValueRef[] vals)
        {
            Branch(segment.Name, vals);
        }

        public void BranchConditional(LLVMValueRef condition, string block, string elseBlock, LLVMValueRef[] vals)
        {
            LLVM.BuildCondBr(_compiler.Builder, condition, _namedBlocks[block], _namedBlocks[elseBlock]);

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
            return LLVM.BuildLoad(_compiler.Builder, Locals[index], "");
        }

        public LLVMValueRef LoadDirect(LLVMValueRef address)
        {
            return LLVM.BuildLoad(_compiler.Builder, address, "");
        }

        public void StoreLocal(int index, LLVMValueRef value)
        {
            LLVM.BuildStore(_compiler.Builder, value, Locals[index]);
        }

        public void StoreDirect(LLVMValueRef value, LLVMValueRef address)
        {
            LLVM.BuildStore(_compiler.Builder, value, address);
        }

        public LLVMValueRef GetStructElement(LLVMValueRef address, int index)
        {
            return LLVM.BuildStructGEP(_compiler.Builder, address, (uint) index, "");
        }

        public LLVMValueRef AddInts(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildAdd(_compiler.Builder, v1, v2, "");
        }

        public LLVMValueRef SubtractInts(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildSub(_compiler.Builder, v1, v2, "");
        }

        public LLVMValueRef MultiplyInts(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildMul(_compiler.Builder, v1, v2, "");
        }

        public LLVMValueRef DivideInts(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildSDiv(_compiler.Builder, v1, v2, "");
        }

        public LLVMValueRef RemainderInts(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildSRem(_compiler.Builder, v1, v2, "");
        }

        public LLVMValueRef ShiftLeft(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildShl(_compiler.Builder, v1, v2, "");
        }

        public LLVMValueRef Or(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildOr(_compiler.Builder, v1, v2, "");
        }

        public LLVMValueRef XOr(LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildXor(_compiler.Builder, v1, v2, "");
        }

        public LLVMValueRef CompareInts(LLVMIntPredicate op, LLVMValueRef v1, LLVMValueRef v2)
        {
            return LLVM.BuildICmp(_compiler.Builder, op, v1, v2, "");
        }

//        public LLVMValueRef? GenerateCall(MethodDefinition method, TypeReference[] argTypes,
//            LLVMValueRef[] args)
//        {
//            if (method.HasThis)
//                throw new NotImplementedException("Instance methods not supported");
//
//            LLVMValueRef func = _cgen.GetFunctionRef(method);
//
//            LLVMValueRef[] convArgs = new LLVMValueRef[args.Length];
//            for (int i = 0; i < args.Length; i++)
//                convArgs[i] = _cgen.GenerateConversion(argTypes[i], method.Parameters[i].ParameterType, args[i]);
//
//            if (method.ReturnType.MetadataType != MetadataType.Void)
//                return LLVM.BuildCall(_cgen.Builder, func, convArgs, "");
//
//            LLVM.BuildCall(_cgen.Builder, func, convArgs, "");
//            return null;
//        }

        public LLVMValueRef? GenerateCall(MethodReference method, LLVMValueRef[] args)
        {
            if (method.HasThis)
                throw new NotImplementedException("Instance methods not supported");

            LLVMValueRef func = _cgen.GetFunctionRef(method);

            if (method.ReturnType.MetadataType != MetadataType.Void)
                return LLVM.BuildCall(_compiler.Builder, func, args, "");
            LLVM.BuildCall(_compiler.Builder, func, args, "");
            return null;
        }

        public void Return(LLVMValueRef? val)
        {
            if (val.HasValue)
                LLVM.BuildRet(_compiler.Builder, val.Value);
            else
                LLVM.BuildRetVoid(_compiler.Builder);
        }
    }
}