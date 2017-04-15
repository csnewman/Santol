using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Santol.Loader
{
    public class LoadedType
    {
        public TypeDefinition Definition { get; }
        public bool IsEnum => Definition.IsEnum;
        public bool IsStruct => Definition.IsValueType && !Definition.IsEnum;
        public bool IsClass => Definition.IsClass && !Definition.IsEnum && !Definition.IsValueType;
        public bool IsInterface => Definition.IsInterface;
        public IList<FieldDefinition> StaticFields { get; }
        public IList<FieldDefinition> ConstantFields { get; }

        public IList<FieldDefinition> LocalFields { get; }
        public IList<MethodInfo> StaticMethods { get; }
        public TypeReference EnumType => Definition.GetEnumUnderlyingType();

        public LoadedType(TypeDefinition definition, IList<FieldDefinition> staticFields,
            IList<FieldDefinition> constantFields, IList<FieldDefinition> localFields, IList<MethodInfo> staticMethods)
        {
            Definition = definition;
            StaticFields = staticFields;
            ConstantFields = constantFields;
            LocalFields = localFields;
            StaticMethods = staticMethods;
        }
    }
}