using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LLVMSharp;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class NewObject : Node
    {
        public IMethod Constructor { get; }
        public NodeReference[] Arguments { get; }
        public override bool HasResult => true;
        public override IType ResultType => Constructor.Parent.GetStackType();

        public NewObject(IMethod constructor, NodeReference[] arguments)
        {
            Constructor = constructor;
            Arguments = arguments;
        }

        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            //Calculate size
            LLVMTypeRef objectPtrType = Constructor.Parent.GetStackType().GetType(codeGenerator);
            LLVMValueRef nullRef = LLVM.ConstNull(objectPtrType);
            LLVMValueRef sizeRef = LLVM.ConstGEP(nullRef,
                new[] {LLVM.ConstInt(LLVM.Int32TypeInContext(codeGenerator.Context), 1, false)});
            LLVMValueRef objectSize = LLVM.BuildBitCast(codeGenerator.Builder, sizeRef,
                PrimitiveType.UIntPtr.GetType(codeGenerator), "");

            // Allocate space
            LLVMValueRef spacePtr = Hooks.PlatformAllocate.GenerateCall(codeGenerator, new[] {objectSize}).Value;
            LLVMValueRef objectPtr = LLVM.BuildBitCast(codeGenerator.Builder, spacePtr, objectPtrType, "");

            // Call constructor
            LLVMValueRef[] args = new LLVMValueRef[Constructor.Arguments.Length];
            args[0] = objectPtr;

            for (int i = 0; i < Arguments.Length; i++)
                args[1 + i] = Arguments[i].GetRef(codeGenerator, Constructor.Arguments[i]);

            Constructor.GenerateCall(codeGenerator, args.ToArray());

            SetRef(objectPtr);
        }

        public override string ToFullString()
            =>
                $"NewObject [Args: [{string.Join<NodeReference>(", ", Arguments)}], Constructor: {Constructor}, Type: {ResultType}]";
    }
}