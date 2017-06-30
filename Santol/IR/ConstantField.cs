namespace Santol.IR
{
    public class ConstantField : IField
    {
        public IType Parent { get; }
        public string Name { get; }
        public string MangledName { get; }
        public IType Type { get; }
        public object Value { get; set; }

        public ConstantField(IType parent, IType type, string name, object value)
        {
            Parent = parent;
            Name = name;
            MangledName = $"{parent.MangledName}_CF_{name}";
            Type = type;
            Value = value;
        }
    }
}