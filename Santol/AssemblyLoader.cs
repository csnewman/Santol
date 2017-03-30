using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MMethodDefinition = Mono.Cecil.MethodDefinition;
using MVariableDefinition = Mono.Cecil.Cil.VariableDefinition;

namespace Santol
{
    public class AssemblyLoader
    {
        private TypeSystem typeSystem;

        public void Load(string file)
        {
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(file);

            typeSystem = assembly.MainModule.TypeSystem;
           

//            TypeDefinition type = assembly.MainModule.GetType("TestOS.Program");

            foreach (TypeDefinition type in assembly.MainModule.Types)
            {
                ClassDefinition @class = new ClassDefinition(type.Name, type.Namespace, type.FullName);

                foreach (MMethodDefinition method in type.Methods)
                {
                    LoadMethod(@class, method);
                }
            }
        }

        private void LoadMethod(ClassDefinition @class, MMethodDefinition methodD)
        {
            MethodDefinition method = new MethodDefinition(methodD);
            MethodBody body = methodD.Body;
            Console.WriteLine($"Method {method.Name}");

            if (body == null || !body.HasVariables)
                return;

            Console.WriteLine("  Locals");
            foreach (MVariableDefinition variableD in methodD.Body.Variables)
            {
                string name = "L_" +
                              (string.IsNullOrEmpty(variableD.Name) ? variableD.Index.ToString() : variableD.Name);

                VariableDefinition variable = new VariableDefinition(variableD.Index, name, variableD.VariableType);
                Console.WriteLine($"    {variable}");

                method.AddLocal(variable);
            }

            body.SimplifyMacros();

            Console.WriteLine($"  Fixed {method.FixFallthroughs()} fallthroughs");
            Console.WriteLine($"  Fixed {method.FixMidBranches()} insegment jumps");
            
            method.PrintInstructions();
            method.GenerateSegments();
            method.DetectNoIncomings();

            //            IList<CodeSegment> filledSegments = new List<CodeSegment>();
            //            segments[0].ForceNoIncomings = true;
            //            FillSegmentInfo(segments, segments[0], methodD.ReturnType.MetadataType != MetadataType.Void, filledSegments);


            //FindCalls(body, segments);

            foreach (CodeSegment segment in method.Segments)
            {
                segment.ParseInstructions(typeSystem);
            }


            method.PrintSegments();

            if (body.Scope != null)
                throw new NotImplementedException("Scopes are not supported yet");
        }

      

        


        
    }
}