using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Santol.Generator;
using MMethodDefinition = Mono.Cecil.MethodDefinition;

namespace Santol.Loader
{
    public class AssemblyLoader
    {
        public IDictionary<string, LoadedType> Load(string file)
        {
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(file);
            TypeSystem typeSystem = assembly.MainModule.TypeSystem;

            IDictionary<string, LoadedType> types = new Dictionary<string, LoadedType>();

            foreach (TypeDefinition type in assembly.MainModule.Types)
            {
                if (type.Name.Equals("<Module>")) continue;
                Console.WriteLine("Loading " + type.FullName);

                IList<FieldDefinition> localFields = new List<FieldDefinition>();
                IList<FieldDefinition> staticFields = new List<FieldDefinition>();
                IList<FieldDefinition> constantFields = new List<FieldDefinition>();
                foreach (FieldDefinition field in type.Fields)
                {
                    if (field.HasConstant)
                        constantFields.Add(field);
                    else if (field.IsStatic)
                        staticFields.Add(field);
                    else
                        localFields.Add(field);
                }

                IList<MethodInfo> staticMethods = new List<MethodInfo>();
                foreach (MMethodDefinition methodD in type.Methods)
                {
                    if (!methodD.IsStatic)
                        throw new NotImplementedException("Support for local methods not implemented!");
                    MethodInfo method = new MethodInfo(methodD);

                    methodD.Body.SimplifyMacros();
                    method.FixFallthroughs();
                    method.FixMidBranches();
                    method.GenerateSegments();
                    method.DetectNoIncomings();

                    foreach (CodeSegment segment in method.Segments)
                        segment.ParseInstructions(typeSystem);

                    staticMethods.Add(method);
                }

                types.Add(type.FullName, new LoadedType(type, staticFields, constantFields, localFields, staticMethods));
            }

            return types;
        }
    }
}