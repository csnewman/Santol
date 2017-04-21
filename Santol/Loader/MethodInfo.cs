using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Santol.Nodes;
using MMethodDefinition = Mono.Cecil.MethodDefinition;

namespace Santol.Loader
{
    public class MethodInfo
    {
        public string Name => Definition.Name;
        public MMethodDefinition Definition { get; }
        public MethodBody Body { get; }
        public ILProcessor Processor { get; }
        public bool DoesReturn => Definition.ReturnType.MetadataType != MetadataType.Void;
        public IList<CodeSegment> Segments { get; set; }
        private CodeRegion _baseRegion;

        public MethodInfo(MMethodDefinition definition)
        {
            Definition = definition;
            Body = definition.Body;
            Processor = Body.GetILProcessor();
        }

        private IList<Instruction> GetJumpDestinations()
        {
            IList<Instruction> jumpDestinations = new List<Instruction>();

            //Find all branches
            foreach (Instruction instruction in Body.Instructions)
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
            foreach (ExceptionHandler exceptionHandler in Body.ExceptionHandlers.Reverse())
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
            Instruction firstInst = Body.Instructions.Count > 0 ? Body.Instructions[0] : null;
            if (firstInst != null && !jumpDestinations.Contains(firstInst))
                jumpDestinations.Add(firstInst);

            return jumpDestinations;
        }

        public void FixMidBranches()
        {
            //Find all mid jumps
            IList<Instruction> midJumps = new List<Instruction>();
            foreach (Instruction instruction in Body.Instructions)
            {
                OpCode code = instruction.OpCode;
                if (code.FlowControl == FlowControl.Cond_Branch && instruction.Next != null &&
                    instruction.Next.OpCode.FlowControl != FlowControl.Branch)
                    midJumps.Add(instruction);
            }

            //Insert fixed jump
            foreach (Instruction instruction in midJumps)
                Processor.InsertAfter(instruction, Processor.Create(OpCodes.Br, instruction.Next));
        }

        public void FixFallthroughs()
        {
            //Find all jump destinations
            IList<Instruction> jumpDestinations = GetJumpDestinations();

            //Find all end points
            List<Instruction> endPoints = new List<Instruction>();
            foreach (Instruction instruction in Body.Instructions)
                if (jumpDestinations.Contains(instruction) && instruction.Previous != null)
                    endPoints.Add(instruction.Previous);


            //Checks for fallthroughs
            foreach (Instruction instruction in endPoints)
            {
                OpCode code = instruction.OpCode;
                if (code.FlowControl == FlowControl.Break)
                    throw new NotImplementedException("Breaks have not been checked");

                if (code.FlowControl != FlowControl.Branch && code.FlowControl != FlowControl.Return)
                    Processor.InsertAfter(instruction, Processor.Create(OpCodes.Br, instruction.Next));
            }
        }

        public void PrintInstructions()
        {
            //Find all jump destinations
            IList<Instruction> jumpDestinations = GetJumpDestinations();

            Console.WriteLine("  Instuctions:");
            foreach (Instruction instruction in Body.Instructions)
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

        public void ParseRegions()
        {
            _baseRegion = new CodeRegion(CodeRegion.RegionType.Root,
                new InstructionRange(Body.Instructions.First(), Body.Instructions.Last()));

            foreach (ExceptionHandler exceptionHandler in Body.ExceptionHandlers.Reverse())
            {
                CodeRegion tryRegion = _baseRegion.AddRegion(CodeRegion.RegionType.Try,
                    new InstructionRange(exceptionHandler.TryStart,
                        exceptionHandler.TryEnd.Previous));

                switch (exceptionHandler.HandlerType)
                {
                    case ExceptionHandlerType.Catch:
                    {
                        CodeRegion region = _baseRegion.AddRegion(CodeRegion.RegionType.Catch,
                            new InstructionRange(exceptionHandler.HandlerStart,
                                exceptionHandler.HandlerEnd.Previous));
                        region.AddAssociatedRegion(tryRegion);
                        tryRegion.AddAssociatedRegion(region);
                        break;
                    }
                    case ExceptionHandlerType.Finally:
                    {
                        CodeRegion region = _baseRegion.AddRegion(CodeRegion.RegionType.Finally,
                            new InstructionRange(exceptionHandler.HandlerStart,
                                exceptionHandler.HandlerEnd.Previous));
                        region.AddAssociatedRegion(tryRegion);
                        tryRegion.AddAssociatedRegion(region);
                        break;
                    }
                    default:
                        throw new NotSupportedException();
                }
            }

            _baseRegion.EnsureEdges(GetJumpDestinations());
        }

        public void PrintRegions()
        {
            Console.WriteLine("  Regions:");
            _baseRegion.PrintTree("    ");
        }

        public void GenerateSegments()
        {
            IList<Instruction> jumpDestinations = GetJumpDestinations();
            Segments = new List<CodeSegment>();

            int segmentLId = 0;
            CodeSegment currentSegment = null;
            foreach (Instruction instruction in Body.Instructions)
            {
                //Start new segment on first instruction
                if (jumpDestinations.Contains(instruction))
                {
                    currentSegment = new CodeSegment(this, "seg_" + segmentLId++, _baseRegion.GetRegion(instruction));
                    Segments.Add(currentSegment);
                }
                if (currentSegment == null)
                    throw new NotSupportedException(
                        "Method body is invalid! Unsure how to handle instructions outside segment");
                currentSegment.AddInstruction(instruction);
            }
        }

        public CodeSegment GetSegment(Instruction instruction)
        {
            return Segments.FirstOrDefault(segment => segment.Instructions.Contains(instruction));
        }

        public void DetectNoIncomings()
        {
            IList<Instruction> jumpDestinations = new List<Instruction>();
            foreach (Instruction instruction in Body.Instructions)
            {
                //Mark segments with only backward control flows
                OpCode code = instruction.OpCode;
                if (code.FlowControl == FlowControl.Branch || code.FlowControl == FlowControl.Cond_Branch)
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
                else if (instruction.Previous != null && instruction.Previous.OpCode.FlowControl == FlowControl.Branch &&
                         !jumpDestinations.Contains(instruction))
                    GetSegment(instruction).ForceNoIncomings = true;
            }

            //Mark exception handlers
            foreach (ExceptionHandler exceptionHandler in Body.ExceptionHandlers.Reverse())
            {
                GetSegment(exceptionHandler.TryStart).ForceNoIncomings = true;

                switch (exceptionHandler.HandlerType)
                {
                    case ExceptionHandlerType.Finally:
                    case ExceptionHandlerType.Catch:
                        GetSegment(exceptionHandler.HandlerStart).ForceNoIncomings = true;
                        break;
                    case ExceptionHandlerType.Filter:
                    case ExceptionHandlerType.Fault:
                        throw new NotSupportedException();
                }
            }
        }

        public void PrintSegments()
        {
            Console.WriteLine("  Segments:");
            foreach (CodeSegment segment in Segments)
            {
                Console.WriteLine($"    {segment.Name}:");
                Console.WriteLine(
                    $"      Incoming: {(segment.ForceNoIncomings ? "forced none" : !segment.HasIncoming ? "none" : string.Join<TypeReference>(",", segment.Incoming))}");
                Console.WriteLine(
                    $"      Region: {segment.Region.Type} ({segment.Region.Range.Start.Offset}-{segment.Region.Range.End.Offset})");
                Console.WriteLine($"      Calls: {segment.Callers.Count}");
                foreach (NodeReference node in segment.Nodes)
                    Console.WriteLine("        " + node.Node.ToFullString());
            }
        }
    }
}