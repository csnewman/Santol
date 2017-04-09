using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using LLVMSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Santol.CIL;
using Santol.Operations;
using Convert = Santol.Operations.Convert;
using MethodDefinition = Santol.CIL.MethodDefinition;
using MMethodDefinition = Mono.Cecil.MethodDefinition;


namespace Santol.Generator
{
    public class ModuleGenerator
    {
        private TypeSystem _typeSystem;
        private CodeGenerator _generator;
        private string _target;
        private LLVMPassManagerRef _passManagerRef;
        private IDictionary<string, ITypeDefinition> _types;

        public ModuleGenerator(string target, LLVMPassManagerRef passManagerRef, IDictionary<string, ITypeDefinition> types)
        {
            _target = target;
            _passManagerRef = passManagerRef;
            _types = types;
        }

        public void GenerateType(ITypeDefinition type)
        {
            if (type is ClassDefinition)
            {
                GenerateClass((ClassDefinition) type);
            }
            else if (type is EnumDefinition)
            {
                GenerateEnum((EnumDefinition) type);
            }
            else
            {
                throw new NotImplementedException("Unknown type " + type);
            }
        }

        public void GenerateEnum(EnumDefinition @enum)
        {
            Console.WriteLine($"Generating {@enum.FullName}");
            _generator = new CodeGenerator(@enum.FullName.Replace('.', '_'), _target, @enum.Module.TypeSystem, _types);

            LLVMTypeRef type = _generator.ConvertType(@enum.Type);
            foreach (KeyValuePair<string, object> pair in @enum.Values)
            {
                _generator.SetConstant(@enum.FullName.Replace('.', '_') + "____" + pair.Key, type,
                    _generator.GeneratePrimitiveConstant(@enum.Type, pair.Value));
            }

            Console.WriteLine("\n\nDump:");
            _generator.Dump();

            _generator.Optimise(_passManagerRef);

            Console.WriteLine("\n\nDump:");
            _generator.Dump();

            _generator.Compile();
        }

        public void GenerateClass(ClassDefinition @class)
        {
            Console.WriteLine($"Generating {@class.FullName}");

            _generator = new CodeGenerator(@class.FullName.Replace('.', '_'), _target, @class.Module.TypeSystem, _types);

            foreach (MethodDefinition methodDefinition in @class.Methods)
                _generator.DefineFunction(methodDefinition.Definition);


            foreach (MethodDefinition methodDefinition in @class.Methods)
            {
                GenerateMethod(methodDefinition);
            }

            Console.WriteLine("\n\nDump:");
            _generator.Dump();

            _generator.Optimise(_passManagerRef);

            Console.WriteLine("\n\nDump:");
            _generator.Dump();

            _generator.Compile();
        }

        public void GenerateMethod(MethodDefinition method)
        {
            MMethodDefinition definition = method.Definition;
            FunctionGenerator fgen = _generator.GetFunction(definition);

            //Allocate locals
            fgen.CreateBlock("entry", null);
            {
                ICollection<VariableDefinition> variables = definition.Body.Variables;
                fgen.Locals = new LLVMValueRef[variables.Count];
                foreach (VariableDefinition variable in variables)
                {
                    string name = "local_" +
                                  (string.IsNullOrEmpty(variable.Name) ? variable.Index.ToString() : variable.Name);
                    LLVMTypeRef type = _generator.ConvertType(variable.VariableType);
                    fgen.Locals[variable.Index] = LLVM.BuildAlloca(_generator.Builder, type, name);
                }
            }

            IList<CodeSegment> segments = method.Segments;
            foreach (CodeSegment segment in segments)
                fgen.CreateBlock(segment, _generator.ConvertTypes(segment.Incoming));

            //Enter first segment
            fgen.SelectBlock("entry");
            fgen.Branch(segments[0], null);

            foreach (CodeSegment segment in segments)
            {
                fgen.SelectBlock(segment);
                StackBuilder builder = fgen.CurrentStackBuilder;

                foreach (IOperation operation in segment.Operations)
                    operation.Generate(_generator, fgen, builder);
            }


            Console.WriteLine("> " + method.Definition.GetName());
        }
    }
}