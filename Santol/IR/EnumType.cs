using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santol.IR
{
    public class EnumType : IType
    {
        public string Name { get; }
        public string MangledName { get; }
        public IType UnderlyingType { get; }
        private IList<IField> _fields;

        public EnumType(string name, IType underlyingType)
        {
            Name = name;
            MangledName = $"C_{name.Replace('.', '_').Replace("*", "PTR")}";
            UnderlyingType = underlyingType;
            _fields = new List<IField>();
        }

        public void AddField(IField constant)
        {
            _fields.Add(constant);
        }
    }
}