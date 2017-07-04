using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Santol.IR;

namespace Santol.Loader
{
    public class MethodBodyLoader
    {
        private AssemblyLoader _assemblyLoader;
        private MethodBody _body;
        private ILProcessor _processor;
        private RegionMap _regionMap;
        private IList<Instruction> _noIncomings;
        private BlockRegion _baseRegion;

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
            PrintInstructions();

            // Map out exception regions
            ParseRegions();
            PrintRegions();

            // Generate regions and blocks
            DetectNoIncomings();
            _baseRegion = new BlockRegion(BlockRegion.RegionType.Primary, null);
            GenerateRegion(_baseRegion, _regionMap);
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
            List<Instruction> endPoints = new List<Instruction>();
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

        private void GenerateRegion(BlockRegion region, RegionMap map)
        {
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
    }
}