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
    public class ClassGenerator
    {
        private TypeSystem _typeSystem;
        private CodeGenerator _generator;
        private string _target;
        private LLVMPassManagerRef _passManagerRef;

        public ClassGenerator(string target, LLVMPassManagerRef passManagerRef)
        {
            _target = target;
            _passManagerRef = passManagerRef;
        }

        public void GenerateClass(ClassDefinition @class)
        {
            Console.WriteLine($"Generating {@class.FullName}");

            _generator = new CodeGenerator(@class.FullName.Replace('.', '_'), _target, @class.Module.TypeSystem);

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
            //            IDictionary<CodeSegment, LLVMBasicBlockRef> segmentBlocks = new Dictionary<CodeSegment, LLVMBasicBlockRef>();
            //            IDictionary<CodeSegment, LLVMValueRef[]> segmentPhis = new Dictionary<CodeSegment, LLVMValueRef[]>();


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