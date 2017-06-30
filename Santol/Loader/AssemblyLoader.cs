using System;
using System.Collections.Generic;
using Mono.Cecil;
using Santol.IR;

namespace Santol.Loader
{
    public class AssemblyLoader
    {
        private IDictionary<TypeDefinition, IType> _resolvedTypes;

        public IList<IType> Load(string file)
        {
            _resolvedTypes = new Dictionary<TypeDefinition, IType>();

            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(file);

            IList<IType> types = new List<IType>();

            foreach (TypeDefinition type in assembly.MainModule.Types)
            {
                if (type.Name.Equals("<Module>")) continue;
                types.Add(ResolveType(type));
            }

            return types;
        }

        public IType ResolveType(TypeReference reference)
        {
            TypeDefinition definition = reference.Resolve();

            if (_resolvedTypes.ContainsKey(definition))
                return _resolvedTypes[definition];

            switch (definition.MetadataType)
            {
                case MetadataType.Void:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.Boolean:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.Char:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.SByte:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.Byte:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.Int16:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.UInt16:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.Int32:
                    return PrimitiveType.Int32;
                case MetadataType.UInt32:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.Int64:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.UInt64:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.Single:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.Double:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.IntPtr:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.UIntPtr:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.Pointer:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.String:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.ValueType:
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.Class:
                {
                    Console.WriteLine("Loading " + definition.FullName + " (class)");
                    ClassType type = new ClassType(definition.FullName);
                    _resolvedTypes.Add(definition, type);

                    foreach (FieldDefinition field in definition.Fields)
                    {
                        Console.WriteLine(" " + field);
                        if (field.HasConstant)
                            type.AddField(new ConstantField(type, ResolveType(field.FieldType), field.Name,
                                field.Constant));
                        else if (field.IsStatic)
                            throw new NotImplementedException("Static fields are not implemented");
                        else
                            throw new NotImplementedException("Local fields are not implemented");
                    }

                    return type;
                }
                default:
                    throw new NotImplementedException("Type not implemented! " + definition);
            }
        }
    }
}