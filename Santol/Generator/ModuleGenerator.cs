using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using LLVMSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Santol.Loader;
using Santol.Operations;
using Convert = Santol.Operations.Convert;
using MMethodDefinition = Mono.Cecil.MethodDefinition;


namespace Santol.Generator
{
    public class ModuleGenerator
    {
        private TypeSystem _typeSystem;
        private CodeGenerator _generator;
        private string _target;
        private LLVMPassManagerRef _passManagerRef;
        private IDictionary<string, LoadedType> _types;

        public ModuleGenerator(string target, LLVMPassManagerRef passManagerRef,
            IDictionary<string, LoadedType> types)
        {
            _target = target;
            _passManagerRef = passManagerRef;
            _types = types;
        }

        public void GenerateType(LoadedType type)
        {
            Console.WriteLine($"Generating {type.Definition.FullName}");

            _generator = new CodeGenerator(type.Definition.GetName(), _target,
                type.Definition.Module.TypeSystem, _types);

            foreach (FieldDefinition field in type.ConstantFields)
            {
                _generator.SetConstant(field.GetName(), _generator.ConvertType(field.FieldType),
                    _generator.GeneratePrimitiveConstant(field.FieldType, field.Constant));
            }

            foreach (FieldDefinition field in type.StaticFields)
            {
                LLVMTypeRef ftype = _generator.ConvertType(field.FieldType);
                _generator.SetGlobal(field.GetName(), ftype, LLVM.ConstNull(ftype));
            }

            foreach (MethodInfo methodDefinition in type.StaticMethods)
                GenerateMethod(methodDefinition);
            
            _generator.Optimise(_passManagerRef);
            _generator.Compile();
        }

        public void GenerateMethod(MethodInfo method)
        {
            MMethodDefinition definition = method.Definition;
            FunctionGenerator fgen = _generator.DefineFunction(definition);

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
        }
    }
}