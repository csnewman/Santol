using LLVMSharp;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class NodeReference
    {
        public Node Node { get; private set; }
        public IType ResultType => Node.ResultType;

        public NodeReference(Node node)
        {
            Node = node;
        }

        public void Update(Node node)
        {
            Node = node;
        }

        public LLVMValueRef GetRef(CodeGenerator codeGenerator, IType target) => Node.GetRef(codeGenerator, target);

        public LLVMValueRef GetRef() => Node.GetRef();
    }
}