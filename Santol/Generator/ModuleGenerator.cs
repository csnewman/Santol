using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using LLVMSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Santol.Loader;
using Santol.Nodes;
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

       

       
    }
}