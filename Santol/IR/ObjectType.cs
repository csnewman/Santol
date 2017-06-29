namespace Santol.IR
{
    public abstract class ObjectType : IType
    {
        public string Name { get; }
        public string MangledName { get; }

        protected ObjectType(string name)
        {
            Name = name;
            MangledName = name.Replace('.', '_').Replace("*", "PTR");
        }
    }
}