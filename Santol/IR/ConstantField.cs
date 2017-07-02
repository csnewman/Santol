namespace Santol.IR
{
    public class ConstantField : IField
    {
        public IType Parent { get; }
        public string Name { get; }
        public string MangledName { get; }
        public IType Type { get; }
        public bool IsShared => true;
        public object Value { get; set; }

        public ConstantField(IType parent, IType type, string name, object value)
        {
            Parent = parent;
            Name = name;
            MangledName = $"{parent.MangledName}_CF_{type.MangledName}_{name}";
            Type = type;
            Value = value;
        }
    }
}