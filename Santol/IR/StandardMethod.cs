using System.Linq;
using LLVMSharp;
using Mono.Cecil.Cil;
using Santol.Generator;
using Santol.Loader;

namespace Santol.IR
{
    public class StandardMethod : IMethod
    {
        public IType Parent { get; }
        public string Name { get; }
        public string MangledName { get; }
        public bool IsStatic { get; }
        public bool IsLocal { get; }
        public IType ReturnType { get; }
        public IType[] Arguments { get; }
        private MethodBody _body;

        public StandardMethod(IType parent, string name, bool isStatic, bool isLocal, bool implicitThis,
            IType returnType,
            IType[] arguments, MethodBody body)
        {
            Parent = parent;
            Name = name;
            MangledName =
                $"{parent.MangledName}_SM_{returnType.MangledName}_{name}_{string.Join("_", arguments.Select(f => f.MangledName))}";
            IsStatic = isStatic;
            IsLocal = isLocal;
            ReturnType = returnType;
            _body = body;

            if (implicitThis)
            {
                Arguments = new IType[arguments.Length + 1];
                Arguments[0] = parent.GetLocalReferenceType();
                arguments.CopyTo(Arguments, 1);
            }
            else
                Arguments = arguments;
        }

        public void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator)
        {
            MethodBodyLoader bodyLoader = new MethodBodyLoader(assemblyLoader, codeGenerator);
            BlockRegion baseRegion = bodyLoader.LoadBody(this, _body);

            LLVMTypeRef type = GetMethodType(codeGenerator);
            LLVMValueRef function = codeGenerator.GetFunction(MangledName, type);
            LLVM.SetLinkage(function, LLVMLinkage.LLVMExternalLinkage);

            FunctionGenerator functionGenerator = new FunctionGenerator(codeGenerator, this, function);
            functionGenerator.CreateBlock("entry", null);
            functionGenerator.Locals = new LLVMValueRef[_body.Variables.Count];
            foreach (VariableDefinition variable in _body.Variables)
            {
                string name = "local_" +
                              (string.IsNullOrEmpty(variable.Name) ? variable.Index.ToString() : variable.Name);
                LLVMTypeRef localType = assemblyLoader.ResolveType(variable.VariableType).GetType(codeGenerator);
                functionGenerator.Locals[variable.Index] = LLVM.BuildAlloca(codeGenerator.Builder, localType, name);
            }

            foreach (Block block in bodyLoader.Blocks)
                functionGenerator.CreateBlock(block, codeGenerator);

            baseRegion.Generate(codeGenerator, functionGenerator);

            functionGenerator.SelectBlock("entry");
            functionGenerator.Branch(bodyLoader.GetFirstBlock(), null);
        }

        public LLVMTypeRef GetMethodType(CodeGenerator codeGenerator)
        {
            LLVMTypeRef returnType = ReturnType.GetType(codeGenerator);
            LLVMTypeRef[] argTypes = new LLVMTypeRef[Arguments.Length];

            for (int i = 0; i < Arguments.Length; i++)
                argTypes[i] = Arguments[i].GetType(codeGenerator);

            return LLVM.FunctionType(returnType, argTypes, false);
        }

        public LLVMValueRef? GenerateCall(CodeGenerator codeGenerator, LLVMValueRef[] arguments)
        {
            LLVMTypeRef type = GetMethodType(codeGenerator);
            LLVMValueRef function = codeGenerator.GetFunction(MangledName, type);

            if (ReturnType != PrimitiveType.Void)
                return LLVM.BuildCall(codeGenerator.Builder, function, arguments, "");
            LLVM.BuildCall(codeGenerator.Builder, function, arguments, "");
            return null;
        }
    }
}