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
        private MethodBody _body;
        private ILProcessor _processor;
        private RegionMap _regionMap;
        private IList<Instruction> _noIncomings;
        private BlockRegion _baseRegion;
        private IDictionary<RegionMap, BlockRegion> _regionMappings;
        private IList<Block> _blocks;
        private IDictionary<Instruction, Block> _blockMap;
        private IDictionary<Block, IList<Instruction>> _blockInstructions;
        private Stack<Node> _nodeStack;

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

        public void PrintRegions()
        {
            Console.WriteLine("  Regions:");
            _regionMap.PrintTree("    ");
        }

        public void LoadBody(MethodBody body)
        {
            _body = body;
            _processor = body.GetILProcessor();

            // Seperate instructions into blocks
            body.SimplifyMacros();
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
            IList<Instruction> jumpDestinations = new List<Instruction>();
            foreach (Instruction instruction in _body.Instructions)
            {
                //Mark instructions with only backward control flows
                OpCode code = instruction.OpCode;
                if (code.Code == Code.Leave)
                    _noIncomings.AddOpt((Instruction) instruction.Operand);
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
                else if (instruction.Previous != null &&
                         instruction.Previous.OpCode.FlowControl == FlowControl.Branch &&
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
            _blocks = new List<Block>();
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
                    _blocks.Add(currentBlock);
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

        private void ParseBlocks(IList<Block> parsedBlocks)
        {
            int parsed = 0;
            foreach (Block block in _blocks)
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
            else if (parsedBlocks.Count != _blocks.Count)
                throw new ArgumentException($"Failed to parse all blocks, parsed {parsedBlocks.Count}/{_blocks.Count}");
        }

        private void ParseBlock(Block block)
        {
            _nodeStack = new Stack<Node>();
            IList<Instruction> instructions = _blockInstructions[block];

            if (!block.ForcedNoIncomings)
            {
                IType[] incoming = block.IncomingTypes;
                if (incoming == null)
                    throw new ArgumentException("Incoming types are yet to be resolved");

                for (int i = 0; i < incoming.Length; i++)
                    PushNode(new IncomingValue(incoming[i], i));
            }

            foreach (Instruction instruction in instructions)
            {
                switch (instruction.OpCode.Code)
                {
                    case Code.Nop:
                        break;

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
                        PushNode(new LoadLocal(_assemblyLoader.ResolveType(definition.VariableType), definition.Index));
                        break;
                    }
                    case Code.Ldarg:
                    {
                        ParameterDefinition definition = (ParameterDefinition) instruction.Operand;
                        PushNode(new LoadArg(_assemblyLoader.ResolveType(definition.ParameterType), definition.Index));
                        break;
                    }
                    case Code.Ldsfld:
                        PushNode(new LoadStatic(
                            (StaticField) _assemblyLoader.ResolveField((FieldReference) instruction.Operand)));
                        break;
                    case Code.Ldfld:
                        PushNode(new LoadField(_codeGenerator, PopNode(), (FieldReference) instruction.Operand));
                        break;

                    default:
                    {
                        Node[] stack = _nodeStack.ToArray();
                        for (int i = 0; i < stack.Length; i++)
                            Console.WriteLine($"{i}: {stack[i]} ({stack[i].ResultType})");
                        throw new NotImplementedException("Unknown opcode " + instruction);
                    }
                }
            }
        }

        private void PushNode(Node node)
        {
            if (!node.HasResult)
                throw new ArgumentException("Nodes without results can not be pushed on the stack");
            _nodeStack.Push(node);
        }

        private NodeReference PopNode()
        {
            return _nodeStack.Pop().TakeReference();
        }
    }
}