using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        private TypeSystem _typeSystem;
        private int _lastTempId;
        private IDictionary<MMethodDefinition, LLVMValueRef> _functionRefs;
        private string _target;
        private LLVMPassManagerRef _passManagerRef;

        public CodeGenerator(string target, LLVMPassManagerRef passManagerRef)
        {
            _target = target;
            _passManagerRef = passManagerRef;
        }

        public void GenerateClass(ClassDefinition @class)
        {
            Console.WriteLine($"Generating {@class.FullName}");

            _module = LLVM.ModuleCreateWithName("Module_" + @class.FullName.Replace('.', '_'));
            _context = LLVM.GetModuleContext(_module);
            _builder = LLVM.CreateBuilder();

            _functionRefs = new Dictionary<MMethodDefinition, LLVMValueRef>();

            foreach (MethodDefinition methodDefinition in @class.Methods)
            {
                //Define function
                MMethodDefinition definition = methodDefinition.Definition;
                LLVMTypeRef functionType = GetFunctionType(definition);
                LLVMValueRef functionRef = LLVM.AddFunction(_module, definition.GetName(), functionType);
                LLVM.SetLinkage(functionRef, LLVMLinkage.LLVMExternalLinkage);

                _functionRefs.Add(definition, functionRef);
            }

            foreach (MethodDefinition methodDefinition in @class.Methods)
            {
                GenerateMethod(methodDefinition);
            }

            Console.WriteLine("\n\nDump:");
            LLVM.DumpModule(_module);
            
            //Optimise
            LLVM.SetTarget(_module, _target);
            LLVM.RunPassManager(_passManagerRef, _module);

            Console.WriteLine("\n\nDump:");
            LLVM.DumpModule(_module);

            //Compile
            LLVMTargetRef tref;
            IntPtr error;
            LLVM.GetTargetFromTriple(_target, out tref, out error);

            LLVMTargetMachineRef machineRef = LLVM.CreateTargetMachine(tref, _target,
                "generic", "",
                LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocDefault,
                LLVMCodeModel.LLVMCodeModelDefault);

            LLVM.TargetMachineEmitToFile(machineRef, _module,
                Marshal.StringToHGlobalAnsi(@class.FullName.Replace('.', '_') + ".o"),
                LLVMCodeGenFileType.LLVMObjectFile,
                out error);
        }

        public void GenerateMethod(MethodDefinition method)
        {
            MMethodDefinition definition = method.Definition;
            _typeSystem = definition.Module.TypeSystem;

            LLVMValueRef functionRef = _functionRefs[definition];
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

            _lastTempId = 0;
            foreach (CodeSegment segment in segments)
            {
                LLVM.PositionBuilderAtEnd(_builder, segmentBlocks[segment]);
                Stack<LLVMValueRef> stack = new Stack<LLVMValueRef>();

                if (segment.HasIncoming)
                    foreach (LLVMValueRef incoming in segmentPhis[segment])
                        stack.Push(incoming);

                foreach (IOperation operation in segment.Operations)
                {
                    //Console.WriteLine("> " + operation.ToFullString());

                    if (operation is LoadPrimitiveConstant)
                    {
                        LoadPrimitiveConstant loadPrimitive = (LoadPrimitiveConstant) operation;
                        stack.Push(GeneratePrimitiveConstant(loadPrimitive.ResultType, loadPrimitive.Value));
                    }
                    else if (operation is LoadLocal)
                    {
                        LoadLocal load = (LoadLocal) operation;
                        stack.Push(LLVM.BuildLoad(_builder, localRefs[load.Variable.Index], GetTempName()));
                    }
                    else if (operation is LoadArg)
                    {
                        LoadArg load = (LoadArg) operation;
                        stack.Push(GenerateConversion(load.Parameter.ParameterType, load.ResultType,
                            LLVM.GetParam(functionRef, (uint) load.Slot)));
                    }
                    else if (operation is StoreLocal)
                    {
                        StoreLocal store = (StoreLocal) operation;
                        LLVMValueRef val = GenerateConversion(store.SourceType, store.Destination.VariableType,
                            stack.Pop());
                        LLVM.BuildStore(_builder, val, localRefs[store.Destination.Index]);
                    }
                    else if (operation is StoreDirect)
                    {
                        StoreDirect store = (StoreDirect) operation;

                        LLVMValueRef val = GenerateConversion(store.SourceType, store.Type,
                            stack.Pop());
                        LLVMValueRef addr = GenerateConversion(store.AddressType, _typeSystem.UIntPtr,
                            stack.Pop());
                        LLVM.BuildStore(_builder, val, addr);
                    }
                    else if (operation is Convert)
                    {
                        Convert convert = (Convert) operation;
                        stack.Push(GenerateConversion(convert.SourceType, convert.ResultType, stack.Pop()));
                    }
                    else if (operation is Numeric)
                    {
                        Numeric numeric = (Numeric) operation;
                        LLVMValueRef v2 = stack.Pop();
                        LLVMValueRef v1 = stack.Pop();
                        stack.Push(GenerateNumeric(numeric.Operation, numeric.Lhs, v1, numeric.Rhs, v2,
                            numeric.ResultType));
                    }
                    else if (operation is Comparison)
                    {
                        Comparison comparison = (Comparison) operation;
                        LLVMValueRef v2 = stack.Pop();
                        LLVMValueRef v1 = stack.Pop();
                        stack.Push(GenerateComparison(comparison.Operation, comparison.Lhs, v1, comparison.Rhs, v2,
                            comparison.ResultType));
                    }
                    else if (operation is Call)
                    {
                        Call call = (Call) operation;

                        LLVMValueRef[] args = new LLVMValueRef[call.ArgTypes.Length];
                        for (int i = 0; i < args.Length; i++)
                            args[args.Length - 1 - i] = stack.Pop();

                        LLVMValueRef? val = GenerateCall(call.Definition, call.ArgTypes, args);
                        if (val.HasValue)
                            stack.Push(val.Value);
                    }
                    else if (operation is Branch)
                    {
                        Branch branch = (Branch) operation;
                        CodeSegment otherSegment = branch.Segment;

                        if (otherSegment.HasIncoming)
                        {
                            LLVMValueRef[] phis = segmentPhis[otherSegment];

                            TypeReference[] sourceTypes = branch.Types;
                            TypeReference[] targetTypes = otherSegment.Incoming;
                            for (int i = 0; i < sourceTypes.Length; i++)
                            {
                                LLVMValueRef raw = stack.Pop();
                                Console.WriteLine(i + ": " + sourceTypes[i] + "  " + raw);
                                LLVMValueRef val = GenerateConversion(sourceTypes[i], targetTypes[i], raw);
                                LLVMValueRef phi = phis[sourceTypes.Length - 1 - i];
                                LLVM.AddIncoming(phi, new[] {val}, new[] {segmentBlocks[segment]}, 1);
                            }
                        }

                        LLVM.BuildBr(_builder, segmentBlocks[otherSegment]);
                    }
                    else if (operation is ConditionalBranch)
                    {
                        ConditionalBranch branch = (ConditionalBranch) operation;
                        LLVMValueRef v1 = GenerateConversion(branch.SourceType, _typeSystem.Boolean, stack.Pop());
                        LLVM.BuildCondBr(_builder, v1, segmentBlocks[branch.Segment], segmentBlocks[branch.ElseSegment]);

                        if (branch.Segment.HasIncoming || branch.ElseSegment.HasIncoming)
                            throw new NotImplementedException("Incoming passing not supported yet");
                    }
                    else if (operation is Return)
                    {
                        Return @return = (Return) operation;

                        if (@return.HasValue)
                            LLVM.BuildRet(_builder,
                                GenerateConversion(@return.ValueType, definition.ReturnType, stack.Pop()));
                        else
                            LLVM.BuildRetVoid(_builder);
                    }
                    else
                    {
                        Console.WriteLine("\n\nDump:");
                        LLVM.DumpModule(_module);

                        throw new NotImplementedException("Operation generation is not implemented yet for " +
                                                          operation.ToFullString());
                    }
                }
            }

            Console.WriteLine("> " + method.Definition.GetName());
        }

        private string GetTempName()
        {
//            return "temp_" + _lastTempId++;
            return "";
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

        public LLVMValueRef GenerateConversion(TypeReference sourceType, TypeReference destType, LLVMValueRef value)
        {
            if (sourceType == destType)
                return value;

            switch (sourceType.MetadataType)
            {
                case MetadataType.Boolean:
                    switch (destType.MetadataType)
                    {
                        case MetadataType.Int32:
                            return LLVM.BuildZExt(_builder, value, ConvertType(destType), GetTempName());
                        default:
                            throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
                    }

                case MetadataType.Char:
                    switch (destType.MetadataType)
                    {
                        case MetadataType.Byte:
                            return LLVM.BuildTrunc(_builder, value, ConvertType(destType), GetTempName());
                        default:
                            throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
                    }

                case MetadataType.Int32:
                    switch (destType.MetadataType)
                    {
                        case MetadataType.Byte:
                        case MetadataType.Boolean:
                        case MetadataType.Char:
                            return LLVM.BuildTrunc(_builder, value, ConvertType(destType), GetTempName());
                        case MetadataType.IntPtr:
                            return LLVM.BuildIntToPtr(_builder, value, ConvertType(destType), GetTempName());
                        default:
                            throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
                    }

                case MetadataType.IntPtr:
                    switch (destType.MetadataType)
                    {
                        case MetadataType.UIntPtr:
                            //TODO: Check
                            return value;
                        default:
                            throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
                    }


                default:
                    throw new NotImplementedException("Unable to convert " + sourceType + " to " + destType);
            }
        }

        public LLVMValueRef GenerateNumeric(Numeric.Operations op, TypeReference lhsType, LLVMValueRef lhs,
            TypeReference rhsType, LLVMValueRef rhs, TypeReference resultType)
        {
            if (lhsType != rhsType)
                throw new NotImplementedException("Numeric ops on different types not implemented yet");

            switch (op)
            {
                case Numeric.Operations.Add:
                    LLVMValueRef val = LLVM.BuildAdd(_builder, lhs, rhs, GetTempName());
                    return GenerateConversion(lhsType, resultType, val);
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        public LLVMValueRef GenerateComparison(Comparison.Operations op, TypeReference lhsType, LLVMValueRef lhs,
            TypeReference rhsType, LLVMValueRef rhs, TypeReference resultType)
        {
            if (lhsType != rhsType)
                throw new NotImplementedException("Comparison ops on different types not implemented yet");

            switch (op)
            {
                case Comparison.Operations.LessThan:
                    //TODO: Ensure ints
                    LLVMValueRef val = LLVM.BuildICmp(_builder, LLVMIntPredicate.LLVMIntSLT, lhs, rhs,
                        GetTempName());
                    return GenerateConversion(_typeSystem.Boolean, resultType, val);
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        public LLVMValueRef GetFunction(MMethodDefinition method)
        {
            if (_functionRefs.ContainsKey(method))
                return _functionRefs[method];

            throw new NotImplementedException("External functions not added yet!");
        }

        public LLVMValueRef? GenerateCall(MMethodDefinition method, TypeReference[] argTypes, LLVMValueRef[] args)
        {
            if (method.HasThis)
                throw new NotImplementedException("Instance methods not supported");

            LLVMValueRef func = GetFunction(method);

            LLVMValueRef[] convArgs = new LLVMValueRef[args.Length];
            for (int i = 0; i < args.Length; i++)
                convArgs[i] = GenerateConversion(argTypes[i], method.Parameters[i].ParameterType, args[i]);

            if (method.ReturnType.MetadataType != MetadataType.Void)
                return LLVM.BuildCall(_builder, func, convArgs, GetTempName());

            LLVM.BuildCall(_builder, func, convArgs, "");
            return null;
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