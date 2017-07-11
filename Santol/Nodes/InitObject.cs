using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class InitObject : Node
    {
        public NodeReference Target { get; }
        public IType TargetType { get; }
        public override bool HasResult => false;
        public override IType ResultType => null;

        public InitObject(IType targetType, NodeReference target)
        {
            TargetType = targetType;
            Target = target;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            TargetType.LoadDefault(codeGenerator, Target.GetRef());
        }

        public override string ToFullString() => $"InitObject [Target: {Target}, TargetType: {TargetType}]";
    }
}