using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Santol.Generator;
using MMethodDefinition = Mono.Cecil.MethodDefinition;

namespace Santol.CIL
{
    public class AssemblyLoader
    {
        public IDictionary<string, ITypeDefinition> Load(string file)
        {
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(file);
            TypeSystem typeSystem = assembly.MainModule.TypeSystem;

            IDictionary<string, ITypeDefinition> types = new Dictionary<string, ITypeDefinition>();

            foreach (TypeDefinition type in assembly.MainModule.Types)
            {
                if (type.Name.Equals("<Module>")) continue;

                Console.WriteLine("Type " + type + " (" + type.GetType() + ")");
                Console.WriteLine($"{type.MetadataType}  {type.Attributes}   {type.ClassSize}  {type.IsEnum} ");

                Console.WriteLine(
                    $"Class: {type.IsClass}  Enum: {type.IsEnum}  Value Type: {type.IsValueType}  Interface: {type.IsInterface}  ");

                if (type.IsEnum)
                {
                    TypeReference underType = type.GetEnumUnderlyingType();
                    IDictionary<string, object> values = new Dictionary<string, object>();
                    foreach (FieldDefinition field in type.Fields)
                    {
                        if (field.Name.Equals("value__"))
                            continue;
                        if (!field.IsStatic || !field.HasConstant)
                            throw new NotSupportedException("Unknown field purpose " + field);
                        if (field.FieldType != type)
                            throw new NotSupportedException("Unknown field purpose, different type! " + field);
                        values[field.Name] = field.Constant;
                    }
                    types.Add(type.FullName, new EnumDefinition(type.Name, type.Namespace, type.FullName,
                        assembly.MainModule, underType, values));
                }
                else if (type.IsValueType)
                {
                    throw new NotImplementedException("Structs are not supported");
                }
                else
                {
                    ClassDefinition @class = new ClassDefinition(type.Name, type.Namespace, type.FullName,
                        assembly.MainModule);

                    foreach (MMethodDefinition method in type.Methods)
                    {
                        LoadMethod(@class, method, typeSystem);
                    }

                    types.Add(type.FullName, @class);
                }
            }

            return types;
        }

        private void LoadMethod(ClassDefinition @class, MMethodDefinition methodD, TypeSystem typeSystem)
        {
            MethodDefinition method = new MethodDefinition(methodD);
            MethodBody body = methodD.Body;
            Console.WriteLine($"Method {method.Name}");

            if (body == null)
                return;

            Console.WriteLine("  Parameters");
            foreach (ParameterDefinition parameter in methodD.Parameters)
            {
                Console.WriteLine($"    {parameter.Index}: {parameter.ParameterType.ToNiceString()} {parameter.Name}");
            }

            Console.WriteLine("  Locals");
            foreach (VariableDefinition variable in methodD.Body.Variables)
            {
                string name = "L_" +
                              (string.IsNullOrEmpty(variable.Name) ? variable.Index.ToString() : variable.Name);
                Console.WriteLine($"    {variable.Index}: {variable.VariableType.ToNiceString()} {name}");
            }

            body.SimplifyMacros();

            Console.WriteLine($"  Fixed {method.FixFallthroughs()} fallthroughs");
            Console.WriteLine($"  Fixed {method.FixMidBranches()} insegment jumps");

            method.PrintInstructions();
            method.GenerateSegments();
            method.DetectNoIncomings();

            foreach (CodeSegment segment in method.Segments)
                segment.ParseInstructions(typeSystem);


            method.PrintSegments();

            if (body.Scope != null)
                throw new NotImplementedException("Scopes are not supported yet");

            @class.AddMethod(method);
        }
    }
}