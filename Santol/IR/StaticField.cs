namespace Santol.IR
{
    public class StaticField : IField
    {
        public IType Parent { get; }
        public string Name { get; }
        public string MangledName { get; }
        public IType Type { get; }

        public StaticField(IType parent, IType type, string name)
        {
            Parent = parent;
            Name = name;
            MangledName = $"{parent.MangledName}_SF_{name}";
            Type = type;
        }
    }
}