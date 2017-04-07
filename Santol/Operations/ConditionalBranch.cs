using LLVMSharp;
using Mono.Cecil;
using Santol.CIL;
using Santol.Generator;

namespace Santol.Operations
{
    public class ConditionalBranch : IOperation
    {
        public CodeSegment Segment { get; }
        public CodeSegment ElseSegment { get; }
        public TypeReference SourceType { get; }
        public TypeReference[] Types { get; }
        public TypeReference ResultType => null;
        
        public ConditionalBranch(CodeSegment segment, CodeSegment elseSegment, TypeReference sourceType, TypeReference[] types)
        {
            Segment = segment;
            ElseSegment = elseSegment;
            SourceType = sourceType;
            Types = types;
        }

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            LLVMValueRef v1 = stack.PopConverted(SourceType, cgen.TypeSystem.Boolean);

            LLVMValueRef[] vals = new LLVMValueRef[Types.Length];
            if (Segment.HasIncoming)
            {
                TypeReference[] targetTypes = Segment.Incoming;
                for (int i = 0; i < Types.Length; i++)
                    vals[Types.Length - 1 - i] = stack.PopConverted(Types[i], targetTypes[i]);
            }

            fgen.BranchConditional(v1, Segment, ElseSegment, vals);
        }

        public string ToFullString()
            => $"ConditionalBranch [Source Type: {SourceType}, Target: {Segment.Name}, Else {ElseSegment.Name}]";
    }
}