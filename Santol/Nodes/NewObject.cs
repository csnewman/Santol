using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class NewObject : Node
    {
        public MethodReference Constructor { get; }
        public NodeReference[] Arguments { get; }
        public override bool HasResult => true;
        public override TypeReference ResultType => Constructor.DeclaringType;

        public NewObject(Compiler compiler, MethodReference constructor, NodeReference[] arguments) : base(compiler)
        {
            Constructor = constructor;
            Arguments = arguments;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            TypeDefinition type = ResultType.Resolve();

            if (type.IsValueType)
            {
                LLVMTypeRef structType = Compiler.CodeGenerator.GetStructType(type);
                LLVMValueRef allocated = LLVM.BuildAlloca(Compiler.Builder, structType, "");

                LLVMValueRef[] args =
                    new LLVMValueRef[Arguments.Length + (Constructor.HasThis && !Constructor.ExplicitThis ? 1 : 0)];
                for (int i = 0; i < Arguments.Length; i++)
                    args[args.Length - 1 - i] = Arguments[Arguments.Length - 1 - i].GetLlvmRef(
                        Constructor.Parameters[Arguments.Length - 1 - i].ParameterType);
                args[0] = allocated;

                foreach (LLVMValueRef llvmValueRef in args)
                {
                    Console.WriteLine(">> " + LLVM.TypeOf(llvmValueRef));
                }

                fgen.GenerateCall(Constructor, args);
                SetLlvmRef(fgen.LoadDirect(allocated));
            }
            else
            {
                throw new NotImplementedException();
            }


//            LLVMValueRef[] args = new LLVMValueRef[Arguments.Length];
//            for (int i = 0; i < args.Length; i++)
//                args[args.Length - 1 - i] = Arguments[args.Length - 1 - i].GetRef(
//                    Constructor.Parameters[args.Length - 1 - i].ParameterType);
//
//            LLVMValueRef? val = fgen.GenerateCall(Constructor, args);
//            if (val.HasValue)
//                SetLlvmRef(val.Value);
        }

        public override string ToFullString()
            =>
                $"NewObject [Args: [{string.Join<NodeReference>(", ", Arguments)}], Constructor: {Constructor}, Type: {ResultType}]";
    }
}