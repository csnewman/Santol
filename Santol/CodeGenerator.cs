using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MMethodDefinition = Mono.Cecil.MethodDefinition;


namespace Santol
{
    public class CodeGenerator
    {
        private LLVMModuleRef _module;
        private LLVMContextRef _context;
        private LLVMBuilderRef _builder;

        public void GenerateClass(ClassDefinition @class)
        {
            Console.WriteLine($"Generating {@class.FullName}");

            _module = LLVM.ModuleCreateWithName("Module_" + @class.FullName.Replace('.', '_'));
            _context = LLVM.GetModuleContext(_module);
            _builder = LLVM.CreateBuilder();


            foreach (MethodDefinition methodDefinition in @class.Methods)
            {
                GenerateMethod(methodDefinition);
            }

            Console.WriteLine("\n\nDump:");
            LLVM.DumpModule(_module);
        }

        public void GenerateMethod(MethodDefinition method)
        {
            MMethodDefinition definition = method.Definition;

            //Define function
            LLVMTypeRef functionType = GetFunctionType(definition);
            LLVMValueRef functionRef = LLVM.AddFunction(_module, method.Definition.GetName(), functionType);
            LLVM.SetLinkage(functionRef, LLVMLinkage.LLVMExternalLinkage);


            LLVMBasicBlockRef entryBlock = CreateBlock(functionRef, "entry");
            //Allocate locals
            LLVMValueRef[] localRefs;
            {
                ICollection<VariableDefinition> variables = definition.Body.Variables;
                localRefs = new LLVMValueRef[variables.Count];
                foreach (VariableDefinition variable in variables)
                {
                    string name = "local_" +
                                  (string.IsNullOrEmpty(variable.Name) ? variable.Index.ToString() : variable.Name);
                    LLVMTypeRef type = ConvertType(variable.VariableType);
                    localRefs[variable.Index] = LLVM.BuildAlloca(_builder, type, name);
                }
            }

            IList<CodeSegment> segments = method.Segments;
            IDictionary<CodeSegment, LLVMBasicBlockRef> segmentBlocks = new Dictionary<CodeSegment, LLVMBasicBlockRef>();
            IDictionary<CodeSegment, LLVMValueRef[]> segmentPhis = new Dictionary<CodeSegment, LLVMValueRef[]>();

            foreach (CodeSegment segment in segments)
            {
                //Creare block
                LLVMBasicBlockRef segmentRef = CreateBlock(functionRef, segment.Name);
                segmentBlocks.Add(segment, segmentRef);

                //Build incomings
                if (segment.HasIncoming)
                {
                    TypeReference[] types = segment.Incoming;
                    LLVMValueRef[] incomings = new LLVMValueRef[types.Length];
                    for (int i = 0; i < types.Length; i++)
                        incomings[i] = LLVM.BuildPhi(_builder, ConvertType(types[i]), "in_" + i);
                    segmentPhis.Add(segment, incomings);
                }
            }

            //Enter first segment
            {
                LLVM.PositionBuilderAtEnd(_builder, entryBlock);
                LLVM.BuildBr(_builder, segmentBlocks[segments[0]]);
            }

            foreach (CodeSegment segment in segments)
            {
                Stack<LLVMValueRef> stack = new Stack<LLVMValueRef>();
                int lastTempId = 0;

                foreach (IOperation operation in segment.Operations)
                {
                    Console.WriteLine("> " + operation.ToFullString());

                    if (operation is LoadPrimitiveConstant)
                    {
                        LoadPrimitiveConstant loadPrimitive = (LoadPrimitiveConstant) operation;
                        stack.Push(GeneratePrimitiveConstant(loadPrimitive.ResultType, loadPrimitive.Value));
                    }
                    else if (operation is LoadLocal)
                    {
                        LoadLocal load = (LoadLocal) operation;
                        stack.Push(LLVM.BuildLoad(_builder, localRefs[load.Variable.Index], "temp_" + lastTempId++));
                    }
                    else if (operation is StoreLocal)
                    {
                        StoreLocal store = (StoreLocal) operation;
                        LLVM.BuildStore(_builder, stack.Pop(), localRefs[store.Destination.Index]);
                    }
                    else if (operation is Numeric)
                    {
                        Numeric numeric = (Numeric)operation;
                        LLVMValueRef v2 = stack.Pop();
                        LLVMValueRef v1 = stack.Pop();

                        switch (numeric.Operation)
                        {
                            case Numeric.Operations.Add:
                                stack.Push(LLVM.BuildAdd(_builder, v1, v2, "temp_" + lastTempId++));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();

                        }
                    }
                    else if (operation is Branch)
                    {
                        Branch branch = (Branch) operation;
                        LLVM.BuildBr(_builder, segmentBlocks[branch.Segment]);

                        if(branch.Segment.HasIncoming)
                            throw new NotImplementedException("Incoming passing not supported yet");
                    }
                    else
                    {
                        throw new NotImplementedException("Operation generation is not implemented yet for " +
                                                          operation.ToFullString());
                    }
                }
            }

            Console.WriteLine("> " + method.Definition.GetName());
        }

        private LLVMValueRef GeneratePrimitiveConstant(TypeReference typeReference, object value)
        {
            LLVMTypeRef type = ConvertType(typeReference);

            switch (typeReference.MetadataType)
            {
                case MetadataType.Boolean:
                    return LLVM.ConstInt(type, (ulong) (((bool) value) ? 1 : 0), false);

                case MetadataType.Byte:
                    return LLVM.ConstInt(type, (byte) value, false);
                case MetadataType.SByte:
                    return LLVM.ConstInt(type, (ulong) (sbyte) value, true);

                case MetadataType.Char:
                    throw new NotImplementedException("Char data " + value.GetType() + "=" + value);

                case MetadataType.UInt16:
                    return LLVM.ConstInt(type, (ushort) value, false);
                case MetadataType.Int16:
                    return LLVM.ConstInt(type, (ulong) (short) value, true);

                case MetadataType.UInt32:
                    return LLVM.ConstInt(type, (uint) value, false);
                case MetadataType.Int32:
                    return LLVM.ConstInt(type, (ulong) (int) value, true);

                case MetadataType.UInt64:
                    return LLVM.ConstInt(type, (ulong) value, false);
                case MetadataType.Int64:
                    return LLVM.ConstInt(type, (ulong) (long) value, true);

                case MetadataType.Single:
                    return LLVM.ConstReal(type, (float) value);
                case MetadataType.Double:
                    return LLVM.ConstReal(type, (double) value);

                case MetadataType.IntPtr:
                    throw new NotImplementedException("IntPtr data " + value.GetType() + "=" + value);
                case MetadataType.UIntPtr:
                    throw new NotImplementedException("UIntPtr data " + value.GetType() + "=" + value);
                default:
                    throw new NotImplementedException("Unknown type! " + typeReference);
            }
        }
        
        private LLVMBasicBlockRef CreateBlock(LLVMValueRef functionRef, string name)
        {
            LLVMBasicBlockRef block = LLVM.AppendBasicBlock(functionRef, name);
            LLVM.PositionBuilderAtEnd(_builder, block);
            return block;
        }

        public LLVMTypeRef GetFunctionType(MMethodDefinition definition)
        {
            LLVMTypeRef returnType = ConvertType(definition.ReturnType);
            LLVMTypeRef[] paramTypes = definition.Parameters.Select(p => ConvertType(p.ParameterType)).ToArray();

            return LLVM.FunctionType(returnType, paramTypes, false);
        }

        public LLVMTypeRef ConvertType(TypeReference reference)
        {
            switch (reference.MetadataType)
            {
                case MetadataType.Void:
                    return LLVM.VoidTypeInContext(_context);
                case MetadataType.Boolean:
                    return LLVM.Int1TypeInContext(_context);

                case MetadataType.Byte:
                case MetadataType.SByte:
                    return LLVM.Int8TypeInContext(_context);

                case MetadataType.Char:
                case MetadataType.UInt16:
                case MetadataType.Int16:
                    return LLVM.Int16TypeInContext(_context);

                case MetadataType.UInt32:
                case MetadataType.Int32:
                    return LLVM.Int32TypeInContext(_context);

                case MetadataType.UInt64:
                case MetadataType.Int64:
                    return LLVM.Int64TypeInContext(_context);

                case MetadataType.Single:
                    return LLVM.FloatTypeInContext(_context);
                case MetadataType.Double:
                    return LLVM.DoubleTypeInContext(_context);

                case MetadataType.IntPtr:
                case MetadataType.UIntPtr:
                    return LLVM.PointerType(LLVM.Int8TypeInContext(_context), 0);
                default:
                    Console.WriteLine("reference " + reference);
                    Console.WriteLine("  MetadataType " + reference.MetadataType);
                    Console.WriteLine("  DeclaringType " + reference.DeclaringType);
                    Console.WriteLine("  FullName " + reference.FullName);
                    Console.WriteLine("  Name " + reference.Name);
                    Console.WriteLine("  MetadataToken " + reference.MetadataToken);
                    throw new NotImplementedException("Unknown type! " + reference);
            }
        }
    }
}