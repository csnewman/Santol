using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Objects
{
    public class ObjectFormat
    {
        private Compiler _compiler;
        private CodeGenerator _codeGenerator;
        public TypeDefinition Type { get; }
        private LLVMTypeRef? _structType;
        public ObjectFormat ParentFormat => _codeGenerator.GetObjectFormat(Type.BaseType.Resolve());

        public ObjectFormat(CodeGenerator codeGenerator, TypeDefinition type)
        {
            _compiler = codeGenerator.Compiler;
            _codeGenerator = codeGenerator;
            Type = type;
        }

        public LLVMTypeRef GetStructType()
        {
            if (_structType.HasValue)
                return _structType.Value;

            LLVMTypeRef type = LLVM.StructCreateNamed(_compiler.Context, Type.GetName());

            IList<LLVMTypeRef> types = new List<LLVMTypeRef>();
            if (Type.HasParent())
                types.Add(ParentFormat.GetStructType());
            else
            {
                //TODO: Replace with typeinfo
                types.Add(LLVM.Int32Type());
            }

            IList<FieldDefinition> locals = Type.GetLocals();
            foreach (FieldDefinition local in locals)
                types.Add(_codeGenerator.ConvertType(local.FieldType));

            LLVM.StructSetBody(type, types.ToArray(), false);
            _structType = type;

            return type;
        }

        public LLVMValueRef GetFieldAddress(LLVMValueRef address, FieldDefinition field)
        {
            if (field.DeclaringType != Type)
            {
                if (Type.HasParent())
                    return ParentFormat.GetFieldAddress(LLVM.BuildStructGEP(_compiler.Builder, address, 0, ""), field);
                throw new ArgumentException("Unable to find field owner " + field);
            }
            int index = Type.GetLocals().IndexOf(field);
            if (index == -1)
                throw new ArgumentException("Field not found in type " + field);
            return LLVM.BuildStructGEP(_compiler.Builder, address, (uint) (1 + index), "");
        }
        
        public LLVMValueRef UpcastTo(LLVMValueRef address, TypeDefinition type)
        {
            if (type.Is(Type))
                return address;
            if (!Type.HasParent())
                throw new ArgumentException("Invalid parent " + type);
            return ParentFormat.UpcastTo(LLVM.BuildStructGEP(_compiler.Builder, address, 0, ""), type);
        }
    }
}