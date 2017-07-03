using System.Linq;

namespace Santol.IR
{
    public class StandardMethod : IMethod
    {
        public IType Parent { get; }
        public string Name { get; }
        public string MangledName { get; }
        public IType ReturnType { get; }
        public IType[] Arguments { get; }

        public StandardMethod(IType parent, string name, IType returnType, IType[] arguments)
        {
            Parent = parent;
            Name = name;
            MangledName =
                $"{parent.MangledName}_SM_{returnType.MangledName}_{name}_{string.Join("_", arguments.Select(f => f.MangledName))}";
            ReturnType = returnType;
            Arguments = arguments;
        }
    }
}