using System.Linq;
using LLVMSharp;
using Mono.Cecil.Cil;

namespace Santol.IR
{
    public class StandardMethod : IMethod
    {
        public IType Parent { get; }
        public string Name { get; }
        public string MangledName { get; }
        public bool IsStatic { get; }
        public bool IsLocal { get; }
        public bool ImplicitThis { get; }
        public IType ReturnType { get; }
        public IType[] Arguments { get; }
        private MethodBody _body;

        public StandardMethod(IType parent, string name, bool isStatic, bool isLocal, bool implicitThis,
            IType returnType,
            IType[] arguments, MethodBody body)
        {
            Parent = parent;
            Name = name;
            MangledName =
                $"{parent.MangledName}_SM_{returnType.MangledName}_{name}_{string.Join("_", arguments.Select(f => f.MangledName))}";
            IsStatic = isStatic;
            IsLocal = isLocal;
            ImplicitThis = implicitThis;
            ReturnType = returnType;
            Arguments = arguments;
            _body = body;
        }

        public LLVMValueRef? GenerateCall(LLVMValueRef[] arguments)
        {
            throw new System.NotImplementedException();
        }
    }
}