using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;
using Santol.Loader;

namespace Santol.Nodes
{
    public class LoadField : Node
    {
        public NodeReference Object { get; }
        public FieldReference Field { get; }
        public override bool HasResult => true;
        public override TypeReference ResultType => Field.FieldType;

        public LoadField(Compiler compiler, NodeReference @object, FieldReference field) : base(compiler)
        {
            Object = @object;
            Field = field;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            TypeReference objType = Object.ResultType;

            Console.WriteLine("reference " + Object.ResultType);
            Console.WriteLine(" Element type " + Object.ResultType.GetElementType());
            Console.WriteLine("  MetadataType " + Object.ResultType.MetadataType);
            Console.WriteLine("  DeclaringType " + Object.ResultType.DeclaringType);
            Console.WriteLine("  FullName " + Object.ResultType.FullName);
            Console.WriteLine("  Name " + Object.ResultType.Name);
            Console.WriteLine("  MetadataToken " + Object.ResultType.MetadataToken);

            LLVMValueRef address;
            switch (objType.MetadataType)
            {
                case MetadataType.Pointer:
                    address = Object.GetLlvmRef();
                    objType = objType.GetElementType();
                    break;
                default:
                    throw new NotImplementedException("Unable to get field on type " + objType);
            }

            switch (objType.MetadataType)
            {
                case MetadataType.ValueType:
                    LoadedType def = CodeGenerator.Resolve(objType);
                    if (def.IsEnum)
                        throw new NotSupportedException("Fields should not be accessed on enum!");

                    SetLlvmRef(fgen.LoadDirect(fgen.GetStructElement(address, def.GetIndexOfLocal(Field))));
                    break;
                default:
                    throw new NotImplementedException("Unable to get field on type " + objType);
            }
        }

        public override string ToFullString() => $"LoadField [Object: {Object}, Field: {Field}]";
    }
}