using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class LoadLocal : Node
    {
        public Mono.Cecil.Cil.VariableDefinition Variable { get; }
        public override bool HasResult => true;
        public override TypeReference ResultType => Variable.VariableType;

        public LoadLocal(Mono.Cecil.Cil.VariableDefinition definition)
        {
            Variable = definition;
        }

        public override void Generate(CodeGenerator cgen, FunctionGenerator fgen)
        {
            SetLlvmRef(fgen.LoadLocal(Variable.Index));
        }

        public override string ToFullString() => $"LoadLocal [Variable: {Variable}, Type: {ResultType}]";
    }
}