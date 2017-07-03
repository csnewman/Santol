﻿using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Rocks;
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
                    return PrimitiveType.UInt16;
                case MetadataType.Int32:
                    return PrimitiveType.Int32;
                case MetadataType.UInt32:
                    return PrimitiveType.UInt32;
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
                    if (definition.IsEnum)
                    {
                        Console.WriteLine("Loading " + definition.FullName + " (enum)");
                        EnumType type = new EnumType(definition.FullName,
                            ResolveType(definition.GetEnumUnderlyingType()));
                        _resolvedTypes.Add(definition, type);

                        foreach (FieldDefinition field in definition.Fields)
                        {
                            if (field.InitialValue.Length != 0)
                                throw new NotSupportedException("Initial values not supported!");

                            if (field.Name.Equals("value__"))
                                continue;
                            else if (field.HasConstant)
                                type.AddField(new ConstantField(type, ResolveType(field.FieldType), field.Name,
                                    field.Constant));
                            else
                                throw new NotImplementedException("Only constants are supported in enuns " + field);
                        }

                        if (definition.HasMethods)
                            throw new NotSupportedException("Methods inside enums are not supported");

                        return type;
                    }
                    else if (definition.PackingSize > 1)
                        throw new NotSupportedException("Packing sizes of 0 and 1 are only supported");
                    else if (definition.IsSequentialLayout)
                    {
                        Console.WriteLine("Loading " + definition.FullName + " (sequential struct)");
                        SequentialStructType type =
                            new SequentialStructType(definition.FullName, definition.PackingSize == 1);
                        _resolvedTypes.Add(definition, type);

                        foreach (FieldDefinition field in definition.Fields)
                        {
                            if (field.InitialValue.Length != 0)
                                throw new NotSupportedException("Initial values not supported!");

                            if (field.HasConstant)
                                type.AddField(new ConstantField(type, ResolveType(field.FieldType), field.Name,
                                    field.Constant));
                            else if (field.IsStatic)
                                type.AddField(new StaticField(type, ResolveType(field.FieldType), field.Name));
                            else
                                type.AddField(new LocalField(type, ResolveType(field.FieldType), field.Name));
                        }

                        if (definition.HasMethods)
                            throw new NotSupportedException("Methods inside structs are not supported");

                        return type;
                    }
                    throw new NotImplementedException("Type not implemented! " + definition);
                case MetadataType.Class:
                {
                    Console.WriteLine("Loading " + definition.FullName + " (class)");
                    ClassType type = new ClassType(definition.FullName);
                    _resolvedTypes.Add(definition, type);

                    foreach (FieldDefinition field in definition.Fields)
                    {
                        if (field.InitialValue.Length != 0)
                            throw new NotSupportedException("Initial values not supported!");

                        if (field.HasConstant)
                            type.AddField(new ConstantField(type, ResolveType(field.FieldType), field.Name,
                                field.Constant));
                        else if (field.IsStatic)
                            type.AddField(new StaticField(type, ResolveType(field.FieldType), field.Name));
                        else
                            throw new NotImplementedException("Local fields are not implemented");
                    }

                    if (definition.HasMethods)
                        throw new NotSupportedException("Methods inside classes are not supported");

                        return type;
                }
                default:
                    throw new NotImplementedException("Type not implemented! " + definition);
            }
        }
    }
}