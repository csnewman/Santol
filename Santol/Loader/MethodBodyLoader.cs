using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Santol.Generator;
using Santol.IR;
using Santol.Nodes;

namespace Santol.Loader
{
    public class MethodBodyLoader
    {
        private AssemblyLoader _assemblyLoader;
        private CodeGenerator _codeGenerator;
        private IMethod _method;
        private MethodBody _body;
        private ILProcessor _processor;
        private RegionMap _regionMap;
        private IList<Instruction> _noIncomings;
        private BlockRegion _baseRegion;
        private IDictionary<RegionMap, BlockRegion> _regionMappings;
        public IList<Block> Blocks { get; private set; }
        private IDictionary<Instruction, Block> _blockMap;
        private IDictionary<Block, IList<Instruction>> _blockInstructions;
        private Node _firstNode, _lastNode;
        private Stack<Node> _nodeStack;

        public MethodBodyLoader(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator)
        {
            _assemblyLoader = assemblyLoader;
            _codeGenerator = codeGenerator;
        }

        private void PrintInstructions()
        {
            //Find all jump destinations
            IList<Instruction> jumpDestinations = GetJumpDestinations();

            Console.WriteLine("  Instuctions:");
            foreach (Instruction instruction in _body.Instructions)
            {
                if (jumpDestinations.Contains(instruction))
                    Console.WriteLine($"    JUMP_POINT:");
                Console.WriteLine("      " + instruction);

                if (instruction.OpCode.FlowControl == FlowControl.Phi)
                    throw new Exception($"Phi: {instruction}");

                if (instruction.OpCode.FlowControl == FlowControl.Break)
                    throw new Exception($"Break: {instruction}");
            }
        }

        private void PrintRegions()
        {
            Console.WriteLine("  Regions:");
            _regionMap.PrintTree("    ");
        }

        public BlockRegion LoadBody(IMethod method, MethodBody body)
        {
            _method = method;
            _body = body;
            _processor = _body.GetILProcessor();

            // Seperate instructions into blocks
            _body.SimplifyMacros();
            ReplaceRuntimeArrayInitalizers();
            FixFallthroughs();
            FixMidBranches();
            DetectNoIncomings();
            PrintInstructions();

            // Map out exception regions and convert them
            ParseRegions();
            PrintRegions();
            GenerateRegions();

            // Creates blocks
            GenerateBlocks();
            ParseBlocks(new List<Block>());

            return _baseRegion;
        }

        private void ReplaceRuntimeArrayInitalizers()
        {
            Instruction current = _body.Instructions.First();
            while (current != null)
            {
                Instruction loadConstant = current;
                Instruction newArray = current.Next;
                Instruction dupeArray = newArray?.Next;
                Instruction loadToken = dupeArray?.Next;
                Instruction call = loadToken?.Next;
                Instruction next = call?.Next;
                if (OpEquals(loadConstant, OpCodes.Ldc_I4) && OpEquals(newArray, OpCodes.Newarr) &&
                    OpEquals(dupeArray, OpCodes.Dup) && OpEquals(loadToken, OpCodes.Ldtoken) &&
                    OpEquals(call, OpCodes.Call))
                {
                    int arraySize = (int) loadConstant.Operand;
                    IType arrayType = _assemblyLoader.ResolveType((TypeReference) newArray.Operand);
                    byte[] initialBytes = ((FieldDefinition) loadToken.Operand).InitialValue;

                    PrimitiveType primitiveType;
                    if (arrayType is PrimitiveType)
                        primitiveType = (PrimitiveType) arrayType;
                    else if (arrayType is EnumType)
                        primitiveType = (PrimitiveType) ((EnumType) arrayType).UnderlyingType;
                    else
                        throw new NotSupportedException();

                    int elementSize = primitiveType.CilElementSize;
                    Func<byte[], int, object> elementExtractor = primitiveType.CilElementExtractor;

                    if (initialBytes.Length != arraySize * elementSize)
                        throw new ArgumentException("Initial Bytes of incorrect size!");

                    _processor.Remove(dupeArray);
                    _processor.Remove(loadToken);
                    _processor.Remove(call);

                    Instruction last = newArray;
                    for (int i = 0; i < arraySize; i++)
                    {
                        Instruction dupe = _processor.Create(OpCodes.Dup);
                        Instruction arrayIndex = _processor.Create(OpCodes.Ldc_I4, i);
                        Instruction value =
                            CreateTypeConstant(arrayType, elementExtractor(initialBytes, i * elementSize));
                        Instruction store = _processor.Create(OpCodes.Stelem_Any, (TypeReference) newArray.Operand);

                        _processor.InsertAfter(last, dupe);
                        _processor.InsertAfter(dupe, arrayIndex);
                        _processor.InsertAfter(arrayIndex, value);
                        _processor.InsertAfter(value, store);
                        last = store;
                    }


                    PrintInstructions();
                    current = next;
                }
                else
                    current = current.Next;
            }
        }

        private Instruction CreateTypeConstant(IType type, object value)
        {
            if (type == PrimitiveType.Boolean || type == PrimitiveType.Byte || type == PrimitiveType.SByte ||
                type == PrimitiveType.Char || type == PrimitiveType.Int16 || type == PrimitiveType.UInt16 ||
                type == PrimitiveType.Int32 || type == PrimitiveType.UInt32)
                return _processor.Create(OpCodes.Ldc_I4, System.Convert.ToInt32(value));
            else if (type == PrimitiveType.Int64 || type == PrimitiveType.UInt64)
                return _processor.Create(OpCodes.Ldc_I8, System.Convert.ToInt64(value));
            else if (type == PrimitiveType.Single)
                return _processor.Create(OpCodes.Ldc_R4, (float) value);
            else if (type == PrimitiveType.Double)
                return _processor.Create(OpCodes.Ldc_R8, (double) value);
            throw new ArgumentException();
        }

        private bool OpEquals(Instruction instruction, OpCode code)
        {
            return instruction != null && instruction.OpCode == code;
        }

        private IList<Instruction> GetJumpDestinations()
        {
            IList<Instruction> jumpDestinations = new List<Instruction>();

            //Find all branches
            foreach (Instruction instruction in _body.Instructions)
            {
                OpCode code = instruction.OpCode;
                if (code.FlowControl != FlowControl.Branch && code.FlowControl != FlowControl.Cond_Branch) continue;
                switch (code.OperandType)
                {
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.InlineBrTarget:
                        jumpDestinations.AddOpt((Instruction) instruction.Operand);
                        break;
                    case OperandType.InlineSwitch:
                        jumpDestinations.AddOpt((Instruction[]) instruction.Operand);
                        break;
                    default:
                        throw new NotImplementedException("Unknown branch instruction " + instruction + "  " +
                                                          instruction.Operand.GetType());
                }
            }

            //Add exception handlers
            foreach (ExceptionHandler exceptionHandler in _body.ExceptionHandlers.Reverse())
            {
                jumpDestinations.AddOpt(exceptionHandler.TryStart);

                switch (exceptionHandler.HandlerType)
                {
                    case ExceptionHandlerType.Finally:
                    case ExceptionHandlerType.Catch:
                        jumpDestinations.AddOpt(exceptionHandler.HandlerStart);
                        break;
                    case ExceptionHandlerType.Filter:
                    case ExceptionHandlerType.Fault:
                        throw new NotSupportedException();
                }
            }

            //AddInts first instruction if missing
            Instruction firstInst = _body.Instructions.Count > 0 ? _body.Instructions[0] : null;
            if (firstInst != null && !jumpDestinations.Contains(firstInst))
                jumpDestinations.Add(firstInst);

            return jumpDestinations;
        }

        private void FixFallthroughs()
        {
            //Find all jump destinations
            IList<Instruction> jumpDestinations = GetJumpDestinations();

            //Find all end points
            IList<Instruction> endPoints = new List<Instruction>();
            foreach (Instruction instruction in _body.Instructions)
                if (jumpDestinations.Contains(instruction) && instruction.Previous != null)
                    endPoints.Add(instruction.Previous);


            //Checks for fallthroughs
            foreach (Instruction instruction in endPoints)
            {
                OpCode code = instruction.OpCode;
                if (code.FlowControl == FlowControl.Break)
                    throw new NotImplementedException("Breaks have not been checked");

                if (code.FlowControl != FlowControl.Branch && code.FlowControl != FlowControl.Return)
                    _processor.InsertAfter(instruction, _processor.Create(OpCodes.Br, instruction.Next));
            }
        }

        private void FixMidBranches()
        {
            //Find all mid jumps
            IList<Instruction> midJumps = new List<Instruction>();
            foreach (Instruction instruction in _body.Instructions)
            {
                OpCode code = instruction.OpCode;
                if (code.FlowControl == FlowControl.Cond_Branch && instruction.Next != null &&
                    instruction.Next.OpCode.FlowControl != FlowControl.Branch)
                    midJumps.Add(instruction);
            }

            //Insert fixed jump
            foreach (Instruction instruction in midJumps)
                _processor.InsertAfter(instruction, _processor.Create(OpCodes.Br, instruction.Next));
        }

        private void ParseRegions()
        {
            _regionMap = new RegionMap(RegionMap.RegionType.Root,
                new InstructionRange(_body.Instructions.First(), _body.Instructions.Last()));

            foreach (ExceptionHandler exceptionHandler in _body.ExceptionHandlers.Reverse())
            {
                RegionMap tryRegionMap = _regionMap.AddRegion(RegionMap.RegionType.Try,
                    new InstructionRange(exceptionHandler.TryStart,
                        exceptionHandler.TryEnd.Previous));

                switch (exceptionHandler.HandlerType)
                {
                    case ExceptionHandlerType.Catch:
                    {
                        RegionMap regionMap = _regionMap.AddRegion(RegionMap.RegionType.Catch,
                            new InstructionRange(exceptionHandler.HandlerStart,
                                exceptionHandler.HandlerEnd.Previous));
                        regionMap.CatchReference = exceptionHandler.CatchType;
                        regionMap.AddAssociatedRegion(tryRegionMap);
                        tryRegionMap.AddAssociatedRegion(regionMap);
                        break;
                    }
                    case ExceptionHandlerType.Finally:
                    {
                        RegionMap regionMap = _regionMap.AddRegion(RegionMap.RegionType.Finally,
                            new InstructionRange(exceptionHandler.HandlerStart,
                                exceptionHandler.HandlerEnd.Previous));
                        regionMap.AddAssociatedRegion(tryRegionMap);
                        tryRegionMap.AddAssociatedRegion(regionMap);
                        break;
                    }
                    default:
                        throw new NotSupportedException();
                }
            }

            _regionMap.EnsureEdges(GetJumpDestinations());
        }

        public void DetectNoIncomings()
        {
            _noIncomings = new List<Instruction>();
            IList<Instruction> jumpDestinations = new List<Instruction>();
            foreach (Instruction instruction in _body.Instructions)
            {
                //Mark instructions with only backward control flows
                OpCode code = instruction.OpCode;
                if (code.Code == Code.Leave)
                    _noIncomings.AddOpt((Instruction) instruction.Operand);
                else if (instruction.Previous == null)
                    _noIncomings.AddOpt(instruction);
                else if (code.FlowControl == FlowControl.Branch || code.FlowControl == FlowControl.Cond_Branch)
                    switch (code.OperandType)
                    {
                        case OperandType.ShortInlineBrTarget:
                        case OperandType.InlineBrTarget:
                            jumpDestinations.AddOpt((Instruction) instruction.Operand);
                            break;
                        case OperandType.InlineSwitch:
                            jumpDestinations.AddOpt((Instruction[]) instruction.Operand);
                            break;
                        default:
                            throw new NotImplementedException("Unknown branch instruction " + instruction + "  " +
                                                              instruction.Operand.GetType());
                    }
                else if (instruction.Previous.OpCode.FlowControl == FlowControl.Branch &&
                         !jumpDestinations.Contains(instruction))
                    _noIncomings.AddOpt(instruction);
            }

            //Mark exception handlers
            foreach (ExceptionHandler exceptionHandler in _body.ExceptionHandlers)
            {
                _noIncomings.AddOpt(exceptionHandler.TryStart);

                switch (exceptionHandler.HandlerType)
                {
                    case ExceptionHandlerType.Finally:
                    case ExceptionHandlerType.Catch:
                        _noIncomings.AddOpt(exceptionHandler.HandlerStart);
                        break;
                    case ExceptionHandlerType.Filter:
                    case ExceptionHandlerType.Fault:
                        throw new NotSupportedException();
                }
            }
        }

        private void GenerateRegions()
        {
            _regionMappings = new Dictionary<RegionMap, BlockRegion>();
            _baseRegion = new BlockRegion(BlockRegion.RegionType.Primary, null);
            GenerateRegion(_baseRegion, _regionMap);
        }

        private void GenerateRegion(BlockRegion region, RegionMap map)
        {
            _regionMappings[map] = region;

            foreach (RegionMap childRegion in map.ChildRegions)
            {
                if (childRegion.Type != RegionMap.RegionType.Try) continue;
                Zone newZone = new Zone(region);
                region.AddChildZone(newZone);
                newZone.TryRegion = new BlockRegion(BlockRegion.RegionType.Primary, newZone);
                GenerateRegion(newZone.TryRegion, childRegion);

                foreach (RegionMap associatedRegion in childRegion.AssociatedRegions)
                {
                    switch (associatedRegion.Type)
                    {
                        case RegionMap.RegionType.Catch:
                            BlockRegion catchRegion = new BlockRegion(BlockRegion.RegionType.Catch, newZone);
                            GenerateRegion(catchRegion, associatedRegion);

                            CatchCase catchCase =
                                new CatchCase(_assemblyLoader.ResolveType(associatedRegion.CatchReference),
                                    catchRegion);
                            newZone.AddCatchRegion(catchCase);
                            break;
                        case RegionMap.RegionType.Finally:
                            if (newZone.FinalRegion != null)
                                throw new ArgumentException("Two final regions should not occur!");
                            newZone.FinalRegion = new BlockRegion(BlockRegion.RegionType.Final, newZone);
                            GenerateRegion(newZone.FinalRegion, associatedRegion);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        private BlockRegion GetRegion(Instruction instruction)
        {
            return _regionMappings[_regionMap.GetRegion(instruction)];
        }

        private void GenerateBlocks()
        {
            Blocks = new List<Block>();
            _blockMap = new Dictionary<Instruction, Block>();
            _blockInstructions = new Dictionary<Block, IList<Instruction>>();

            IList<Instruction> jumpDestinations = GetJumpDestinations();

            int lastId = 0;
            Block currentBlock = null;
            IList<Instruction> instructions = null;
            foreach (Instruction instruction in _body.Instructions)
            {
                if (jumpDestinations.Contains(instruction))
                {
                    BlockRegion region = GetRegion(instruction);
                    currentBlock = new Block("block_" + lastId++, region);
                    currentBlock.ForcedNoIncomings = _noIncomings.Contains(instruction);
                    Blocks.Add(currentBlock);
                    region.AddBlock(currentBlock);
                    instructions = new List<Instruction>();
                    _blockInstructions[currentBlock] = instructions;
                }

                if (currentBlock == null)
                    throw new ArgumentException("Current block should never be null");

                _blockMap[instruction] = currentBlock;
                instructions.Add(instruction);
            }
        }

        private Block GetBlock(Instruction instruction)
        {
            return _blockMap[instruction];
        }

        public Block GetFirstBlock()
        {
            return _blockMap[_body.Instructions.First()];
        }

        private void ParseBlocks(IList<Block> parsedBlocks)
        {
            int parsed = 0;
            foreach (Block block in Blocks)
            {
                if (parsedBlocks.Contains(block))
                    continue;
                if (!block.ForcedNoIncomings && block.IncomingTypes == null) continue;
                parsedBlocks.Add(block);
                ParseBlock(block);
                parsed++;
            }
            if (parsed > 0)
                ParseBlocks(parsedBlocks);
            else if (parsedBlocks.Count != Blocks.Count)
                throw new ArgumentException($"Failed to parse all blocks, parsed {parsedBlocks.Count}/{Blocks.Count}");
        }

        private Tuple<IType[], NodeReference[]> CollapseStack()
        {
            Node[] nodes = _nodeStack.ToArray();
            IType[] types = new IType[nodes.Length];
            NodeReference[] references = new NodeReference[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                Node node = nodes[i];
                types[i] = node.ResultType;
                references[i] = node.TakeReference();
            }
            _nodeStack.Clear();
            return new Tuple<IType[], NodeReference[]>(types, references);
        }

        private void ParseBlock(Block block)
        {
            _nodeStack = new Stack<Node>();
            _firstNode = null;
            _lastNode = null;

            if (!block.ForcedNoIncomings)
            {
                IType[] incoming = block.IncomingTypes;
                if (incoming == null)
                    throw new ArgumentException("Incoming types are yet to be resolved");
                for (int i = 0; i < incoming.Length; i++)
                    PushNode(new IncomingValue(incoming[i], i));
            }

            ParseInstructions(block, _blockInstructions[block]);
            block.FirstNode = _firstNode;
        }

        private void ParseInstructions(Block block, IList<Instruction> instructions)
        {
            foreach (Instruction instruction in instructions)
            {
                switch (instruction.OpCode.Code)
                {
                    case Code.Nop:
                        break;

                    case Code.Dup:
                    {
                        Node node = _nodeStack.Pop();
                        _nodeStack.Push(node);
                        _nodeStack.Push(node);
                        break;
                    }

                    case Code.Ldc_I4:
                        PushNode(new LoadPrimitiveConstant(PrimitiveType.Int32, instruction.Operand));
                        break;
                    case Code.Ldc_I8:
                        PushNode(new LoadPrimitiveConstant(PrimitiveType.Int64, instruction.Operand));
                        break;
                    case Code.Ldc_R4:
                        PushNode(new LoadPrimitiveConstant(PrimitiveType.Single, instruction.Operand));
                        break;
                    case Code.Ldc_R8:
                        PushNode(new LoadPrimitiveConstant(PrimitiveType.Double, instruction.Operand));
                        break;

                    case Code.Ldloc:
                    {
                        VariableDefinition definition = (VariableDefinition) instruction.Operand;
                        PushNode(new LoadLocal(_assemblyLoader.ResolveType(definition.VariableType).GetStackType(),
                            definition.Index));
                        break;
                    }
                    case Code.Ldloca:
                    {
                        VariableDefinition definition = (VariableDefinition) instruction.Operand;
                        PushNode(new LoadLocalAddress(
                            new IR.PointerType(_assemblyLoader.ResolveType(definition.VariableType)),
                            definition.Index));
                        break;
                    }
                    case Code.Ldarg:
                    {
                        int index = ((ParameterDefinition) instruction.Operand).Index + _method.ArgumentOffset;
                        PushNode(new LoadArg(_method.Arguments[index], index));
                        break;
                    }
                    case Code.Ldsfld:
                        PushNode(new LoadStatic(
                            (StaticField) _assemblyLoader.ResolveField((FieldReference) instruction.Operand)));
                        break;
                    case Code.Ldfld:
                        PushNode(new LoadField(PopNode(),
                            _assemblyLoader.ResolveField((FieldReference) instruction.Operand)));
                        break;
                    case Code.Ldflda:
                        PushNode(new LoadFieldAddress(PopNode(),
                            _assemblyLoader.ResolveField((FieldReference) instruction.Operand)));
                        break;

                    case Code.Ldind_U1:
                        PushNode(new LoadDirect(PrimitiveType.Byte, PopNode()));
                        break;

                    case Code.Stloc:
                    {
                        VariableDefinition definition = (VariableDefinition) instruction.Operand;
                        PushNode(new StoreLocal(_assemblyLoader.ResolveType(definition.VariableType).GetStackType(),
                            definition.Index,
                            PopNode()));
                        break;
                    }
                    case Code.Stsfld:
                        PushNode(new StoreStatic(
                            (StaticField) _assemblyLoader.ResolveField((FieldReference) instruction.Operand),
                            PopNode()));
                        break;
                    case Code.Stfld:
                    {
                        NodeReference val = PopNode();
                        NodeReference obj = PopNode();
                        PushNode(new StoreField(obj, _assemblyLoader.ResolveField((FieldReference) instruction.Operand),
                            val));
                        break;
                    }
                    case Code.Stind_I1:
                        PushNode(new StoreDirect(PrimitiveType.Byte, PopNode(), PopNode()));
                        break;

                    case Code.Conv_I:
                        PushNode(new Nodes.Convert(PrimitiveType.IntPtr, PopNode()));
                        break;
                    case Code.Conv_I4:
                        PushNode(new Nodes.Convert(PrimitiveType.Int32, PopNode()));
                        break;
                    case Code.Conv_I8:
                        PushNode(new Nodes.Convert(PrimitiveType.Int64, PopNode()));
                        break;
                    case Code.Conv_U:
                        PushNode(new Nodes.Convert(PrimitiveType.UIntPtr, PopNode()));
                        break;
                    case Code.Conv_U1:
                        PushNode(new Nodes.Convert(PrimitiveType.Byte, PopNode()));
                        break;
                    case Code.Conv_U2:
                        PushNode(new Nodes.Convert(PrimitiveType.UInt16, PopNode()));
                        break;

                    case Code.Add:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (!lhs.ResultType.IsStackCompatible(rhs.ResultType))
                            throw new NotSupportedException("Can not add two different types!");
                        PushNode(new Numeric(Numeric.OperationType.Add, lhs, rhs));
                        break;
                    }
                    case Code.Sub:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (!lhs.ResultType.IsStackCompatible(rhs.ResultType))
                            throw new NotSupportedException("Can not subtract two different types!");
                        PushNode(new Numeric(Numeric.OperationType.Subtract, lhs, rhs));
                        break;
                    }
                    case Code.Mul:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (!lhs.ResultType.IsStackCompatible(rhs.ResultType))
                            throw new NotSupportedException("Can not multiply two different types!");
                        PushNode(new Numeric(Numeric.OperationType.Multiply, lhs, rhs));
                        break;
                    }
                    case Code.Div:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (!lhs.ResultType.IsStackCompatible(rhs.ResultType))
                            throw new NotSupportedException("Can not divide two different types!");
                        PushNode(new Numeric(Numeric.OperationType.Divide, lhs, rhs));
                        break;
                    }
                    case Code.Rem:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (!lhs.ResultType.IsStackCompatible(rhs.ResultType))
                            throw new NotSupportedException("Can not find remainder of two different types!");
                        PushNode(new Numeric(Numeric.OperationType.Remainder, lhs, rhs));
                        break;
                    }
                    case Code.Shl:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        PushNode(new Numeric(Numeric.OperationType.ShiftLeft, lhs, rhs));
                        break;
                    }
                    case Code.Or:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (!lhs.ResultType.IsStackCompatible(rhs.ResultType))
                            throw new NotSupportedException("Can not or two different types!");
                        PushNode(new Numeric(Numeric.OperationType.Or, lhs, rhs));
                        break;
                    }
                    case Code.Xor:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (!lhs.ResultType.IsStackCompatible(rhs.ResultType))
                            throw new NotSupportedException("Can not xor two different types!");
                        PushNode(new Numeric(Numeric.OperationType.XOr, lhs, rhs));
                        break;
                    }

                    case Code.Clt:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        PushNode(new Comparison(Comparison.OperationType.LessThan, lhs, rhs));
                        break;
                    }
                    case Code.Cgt:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        PushNode(new Comparison(Comparison.OperationType.GreaterThan, lhs, rhs));
                        break;
                    }
                    case Code.Ceq:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        PushNode(new Comparison(Comparison.OperationType.Equal, lhs, rhs));
                        break;
                    }

                    case Code.Call:
                    {
                        IMethod method = _assemblyLoader.ResolveMethod((MethodReference) instruction.Operand);
                        NodeReference[] args = new NodeReference[method.Arguments.Length];

                        for (int i = 0; i < args.Length; i++)
                            args[args.Length - 1 - i] = PopNode();

                        PushNode(new Call(method, args));
                        break;
                    }
                    case Code.Callvirt:
                    {
                        IMethod method = _assemblyLoader.ResolveMethod((MethodReference) instruction.Operand);

                        if (method.IsStatic)
                            throw new NotImplementedException("Expected a local/virtual method");

                        NodeReference[] args = new NodeReference[method.Arguments.Length - 1];

                        for (int i = 0; i < args.Length; i++)
                            args[args.Length - 1 - i] = PopNode();

                        PushNode(new CallVirtual(method, PopNode(), args));
                        break;
                    }

                    case Code.Initobj:
                        PushNode(new InitObject(_assemblyLoader.ResolveType((TypeReference) instruction.Operand),
                            PopNode()));
                        break;
                    case Code.Newobj:
                    {
                        IMethod method = _assemblyLoader.ResolveMethod((MethodReference) instruction.Operand);
                        NodeReference[] args = new NodeReference[method.Arguments.Length - 1];

                        for (int i = 0; i < args.Length; i++)
                            args[args.Length - 1 - i] = PopNode();

                        PushNode(new NewObject(method, args));
                        break;
                    }

                    case Code.Br:
                    {
                        Block target = GetBlock((Instruction) instruction.Operand);

                        Tuple<IType[], NodeReference[]> stack = CollapseStack();
                        target.AddCaller(block, stack.Item1);
                        PushNode(new Branch(target, stack.Item2));
                        return;
                    }
                    case Code.Brtrue:
                    {
                        NodeReference cond = PopNode();
                        Block target = GetBlock((Instruction) instruction.Operand);
                        Block elseTarget = GetBlock((Instruction) instruction.Next.Operand);
                        Tuple<IType[], NodeReference[]> stack = CollapseStack();
                        target.AddCaller(block, stack.Item1);
                        elseTarget.AddCaller(block, stack.Item1);
                        PushNode(new ConditionalBranch(target, elseTarget, cond, stack.Item2));
                        return;
                    }
                    case Code.Brfalse:
                    {
                        NodeReference cond = PopNode();
                        Block target = GetBlock((Instruction) instruction.Operand);
                        Block elseTarget = GetBlock((Instruction) instruction.Next.Operand);
                        Tuple<IType[], NodeReference[]> stack = CollapseStack();
                        target.AddCaller(block, stack.Item1);
                        elseTarget.AddCaller(block, stack.Item1);
                        //TODO: Check whether this is valid in all cases
                        PushNode(new ConditionalBranch(elseTarget, target, cond, stack.Item2));
                        return;
                    }
                    case Code.Blt:
                    {
                        NodeReference v2 = PopNode();
                        NodeReference v1 = PopNode();
                        NodeReference cond =
                            AddNode(new Comparison(Comparison.OperationType.LessThan, v1, v2));
                        Block target = GetBlock((Instruction) instruction.Operand);
                        Block elseTarget = GetBlock((Instruction) instruction.Next.Operand);
                        Tuple<IType[], NodeReference[]> stack = CollapseStack();
                        target.AddCaller(block, stack.Item1);
                        elseTarget.AddCaller(block, stack.Item1);
                        PushNode(new ConditionalBranch(target, elseTarget, cond, stack.Item2));
                        return;
                    }
                    case Code.Bge:
                    {
                        NodeReference v2 = PopNode();
                        NodeReference v1 = PopNode();
                        NodeReference cond =
                            AddNode(new Comparison(Comparison.OperationType.GreaterThanOrEqual, v1, v2));
                        Block target = GetBlock((Instruction) instruction.Operand);
                        Block elseTarget = GetBlock((Instruction) instruction.Next.Operand);
                        Tuple<IType[], NodeReference[]> stack = CollapseStack();
                        target.AddCaller(block, stack.Item1);
                        elseTarget.AddCaller(block, stack.Item1);
                        PushNode(new ConditionalBranch(target, elseTarget, cond, stack.Item2));
                        return;
                    }
                    case Code.Ret:
                        PushNode(new Return(_method.ReturnType != PrimitiveType.Void ? PopNode() : null));
                        return;

                    default:
                    {
                        Node[] stack = _nodeStack.ToArray();
                        for (int i = 0; i < stack.Length; i++)
                            Console.WriteLine($"{i}: {stack[i]} ({stack[i].ResultType})");
                        throw new NotImplementedException("Unknown opcode " + instruction);
                    }
                }
            }
            throw new ArgumentException("Invalid set on instructions, with no ending branch");
        }

        private void PushNode(Node node)
        {
            if (_firstNode == null)
                _firstNode = node;
            else
            {
                _lastNode.NextNode = node.TakeReference();
                node.PreviousNode = _lastNode.TakeReference();
            }
            _lastNode = node;

            if (!node.HasResult) return;
            if (!node.ResultType.IsAllowedOnStack)
                throw new ArgumentException("Node returns type not allowed on stack");
            _nodeStack.Push(node);
        }

        private NodeReference AddNode(Node node)
        {
            if (_firstNode == null)
                _firstNode = node;
            else
            {
                _lastNode.NextNode = node.TakeReference();
                node.PreviousNode = _lastNode.TakeReference();
            }
            _lastNode = node;

            if (!node.HasResult)
                throw new ArgumentException("Nodes without results cannot use add");
            if (!node.ResultType.IsAllowedOnStack)
                throw new ArgumentException("Node returns type not allowed on stack");
            return node.TakeReference();
        }

        private NodeReference PopNode()
        {
            return _nodeStack.Pop().TakeReference();
        }
    }
}