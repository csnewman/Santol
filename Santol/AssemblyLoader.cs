using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MMethodDefinition = Mono.Cecil.MethodDefinition;

namespace Santol
{
    public class AssemblyLoader
    {
        public IList<ClassDefinition> Load(string file)
        {
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(file);
            TypeSystem typeSystem = assembly.MainModule.TypeSystem;

            IList<ClassDefinition> classes = new List<ClassDefinition>();

            foreach (TypeDefinition type in assembly.MainModule.Types)
            {
                if (type.Name.Equals("<Module>")) continue;

                ClassDefinition @class = new ClassDefinition(type.Name, type.Namespace, type.FullName);

                foreach (MMethodDefinition method in type.Methods)
                {
                    LoadMethod(@class, method, typeSystem);
                }

                classes.Add(@class);
            }

            return classes;
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