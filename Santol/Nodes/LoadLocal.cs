using Mono.Cecil;
using Mono.Cecil.Cil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class LoadLocal : Node
    {
        public VariableDefinition Variable { get; }
        public override bool HasResult => true;
        public override TypeReference ResultType => Variable.VariableType;

        public LoadLocal(Compiler compiler, VariableDefinition definition) : base(compiler)
        {
            Variable = definition;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            SetLlvmRef(fgen.LoadLocal(Variable.Index));
        }

        public override string ToFullString() => $"LoadLocal [Variable: {Variable}, Type: {ResultType}]";
    }
}