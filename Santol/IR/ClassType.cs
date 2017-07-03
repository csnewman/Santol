using System.Collections.Generic;

namespace Santol.IR
{
    public class ClassType : IType
    {
        public string Name { get; }
        public string MangledName { get; }
        private IList<IField> _fields;
        private IList<IMethod> _methods;

        public ClassType(string name)
        {
            Name = name;
            MangledName = $"C_{name.Replace('.', '_').Replace("*", "PTR")}";
            _fields = new List<IField>();
            _methods = new List<IMethod>();
        }

        public void AddField(IField field)
        {
            _fields.Add(field);
        }

        public void AddMethod(IMethod method)
        {
            _methods.Add(method);
        }
    }
}