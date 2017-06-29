using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
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

            Console.WriteLine("Loading " + definition.FullName);


            if (definition.IsEnum)
                throw new NotImplementedException("Enums are not implemented yet");
            else if (definition.IsValueType)
                throw new NotImplementedException("Structs are not implemented yet");
            else
            {
                ClassType type = new ClassType(definition.FullName);
                _resolvedTypes.Add(definition, type);

                foreach (FieldDefinition field in definition.Fields)
                {
                    if (field.HasConstant)
                        constantFields.Add(field);
                    else if (field.IsStatic)
                        staticFields.Add(field);
                    else
                        localFields.Add(field);
                }


                Console.WriteLine(" >> " + type.MangledName);
            }

            return _resolvedTypes[definition];
        }
    }
}