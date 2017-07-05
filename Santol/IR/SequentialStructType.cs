﻿using System.Collections.Generic;
using LLVMSharp;
using Santol.Generator;

namespace Santol.IR
{
    public class SequentialStructType : IType
    {
        public string Name { get; }
        public string MangledName { get; }
        public bool Packed { get; }
        private IList<IField> _fields;

        public SequentialStructType(string name, bool packed)
        {
            Name = name;
            MangledName = $"SS_{name.Replace('.', '_').Replace("*", "PTR")}";
            Packed = packed;
            _fields = new List<IField>();
        }

        public void AddField(IField field)
        {
            _fields.Add(field);
        }

        public LLVMTypeRef GetType(CodeGenerator codeGenerator)
        {
            throw new System.NotImplementedException();
        }
    }
}