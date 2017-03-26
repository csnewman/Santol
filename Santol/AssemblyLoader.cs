using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MMethodDefinition = Mono.Cecil.MethodDefinition;
using MVariableDefinition = Mono.Cecil.Cil.VariableDefinition;

namespace Santol
{
    public class AssemblyLoader
    {
        public void Load(string file)
        {
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(file);

//            TypeDefinition type = assembly.MainModule.GetType("TestOS.Program");

            foreach (TypeDefinition type in assembly.MainModule.Types)
            {
                ClassDefinition @class = new ClassDefinition(type.Name, type.Namespace, type.FullName);

                foreach (MMethodDefinition method in type.Methods)
                {
                    LoadMethod(@class, method);
                }
            }
        }

        private void LoadMethod(ClassDefinition @class, MMethodDefinition methodD)
        {
            MethodDefinition method = new MethodDefinition(methodD);
            MethodBody body = methodD.Body;
            Console.WriteLine($"Method {method.Name}");

            if (body == null || !body.HasVariables)
                return;

            Console.WriteLine("  Locals");
            foreach (MVariableDefinition variableD in methodD.Body.Variables)
            {
                string name = "L_" +
                              (string.IsNullOrEmpty(variableD.Name) ? variableD.Index.ToString() : variableD.Name);

                VariableDefinition variable = new VariableDefinition(variableD.Index, name, variableD.VariableType);
                Console.WriteLine($"    {variable}");

                method.AddLocal(variable);
            }

            ILProcessor processor = body.GetILProcessor();
            int @fixed = FixFallthroughs(body, processor);
            Console.WriteLine($"  Fixed {@fixed} fallthroughs");
            @fixed = FixFallthroughs(body, processor);
            if (@fixed != 0)
                throw new Exception($"{@fixed} fallthroughs were fixed on a second pass, something went wrong");

            @fixed = BreakUpJumps(body, processor);
            Console.WriteLine($"  Fixed {@fixed} insegment jumps");
            @fixed = BreakUpJumps(body, processor);
            if (@fixed != 0)
                throw new Exception($"{@fixed} insegment jumps were fixed on a second pass, something went wrong");

            PrintInstructions(body);

            IList<CodeSegment> segments = GenerateSegments(body);

            CheckNoIncomings(body, segments);

            IList<CodeSegment> filledSegments = new List<CodeSegment>();
            segments[0].ForceNoIncomings = true;
            FillSegmentInfo(segments, segments[0], methodD.ReturnType.MetadataType != MetadataType.Void, filledSegments);


            FindCalls(body, segments);


            PrintSegments(segments, methodD.ReturnType.MetadataType != MetadataType.Void);


            //            body.Scope.

            PrintScope(body.Scope, "> ");
        }

        private IList<Instruction> GetJumpDestinations(MethodBody body)
        {
            IList<Instruction> jumpDestinations = new List<Instruction>();
            foreach (Instruction instruction in body.Instructions)
            {
                OpCode code = instruction.OpCode;
                if (code.FlowControl == FlowControl.Branch || code.FlowControl == FlowControl.Cond_Branch)
                {
                    if (code.OperandType == OperandType.ShortInlineBrTarget ||
                        code.OperandType == OperandType.InlineBrTarget)
                        jumpDestinations.AddOpt((Instruction) instruction.Operand);
                    else if (code.OperandType == OperandType.InlineSwitch)
                        jumpDestinations.AddOpt((Instruction[]) instruction.Operand);
                    else
                        throw new NotImplementedException("Unknown branch instruction " + instruction + "  " +
                                                          instruction.Operand.GetType());
                }
            }

            Instruction firstInst = body.Instructions.Count > 0 ? body.Instructions[0] : null;
            if (firstInst != null && !jumpDestinations.Contains(firstInst))
                jumpDestinations.Add(firstInst);

            return jumpDestinations;
        }

        private void PrintInstructions(MethodBody body)
        {
            //Find all jump destinations
            IList<Instruction> jumpDestinations = GetJumpDestinations(body);

            Console.WriteLine("  Instuctions:");
            foreach (Instruction instruction in body.Instructions)
            {
                if (jumpDestinations.Contains(instruction))
                {
                    Console.WriteLine($"    JUMP_POINT:");
                }
                Console.WriteLine("      " + instruction);

                if (instruction.OpCode.FlowControl == FlowControl.Phi)
                    throw new Exception($"Phi: {instruction}");

                if (instruction.OpCode.FlowControl == FlowControl.Break)
                    throw new Exception($"Break: {instruction}");
            }
        }

        private int FixFallthroughs(MethodBody body, ILProcessor processor)
        {
            //Find all jump destinations
            IList<Instruction> jumpDestinations = GetJumpDestinations(body);

            //Find all end points
            List<Instruction> endPoints = new List<Instruction>();
            foreach (Instruction instruction in body.Instructions)
            {
                if (jumpDestinations.Contains(instruction) && instruction.Previous != null)
                {
                    endPoints.Add(instruction.Previous);
                }
            }

            //Checks for fallthroughs
            int fixCount = 0;
            foreach (Instruction instruction in endPoints)
            {
                OpCode code = instruction.OpCode;
                if (code.FlowControl == FlowControl.Break)
                    throw new NotImplementedException("Breaks have not been checked");

                if (code.FlowControl != FlowControl.Branch && code.FlowControl != FlowControl.Return)
                {
                    //Fixes fallthrough
                    processor.InsertAfter(instruction, processor.Create(OpCodes.Br, instruction.Next));
                    fixCount++;
                }
            }

            return fixCount;
        }

        private int BreakUpJumps(MethodBody body, ILProcessor processor)
        {
            //Find all mid jumps
            IList<Instruction> midJumps = new List<Instruction>();
            foreach (Instruction instruction in body.Instructions)
            {
                OpCode code = instruction.OpCode;
                if (code.FlowControl == FlowControl.Cond_Branch && instruction.Next != null &&
                    instruction.Next.OpCode.FlowControl != FlowControl.Branch)
                {
                    midJumps.Add(instruction);
                }
            }

            //End segment
            foreach (Instruction instruction in midJumps)
                processor.InsertAfter(instruction, processor.Create(OpCodes.Br, instruction.Next));

            return midJumps.Count;
        }

        private IList<CodeSegment> GenerateSegments(MethodBody body)
        {
            IList<Instruction> jumpDestinations = GetJumpDestinations(body);
            IList<CodeSegment> segments = new List<CodeSegment>();

            int segmentLId = 0;
            CodeSegment currentSegment = null;
            foreach (Instruction instruction in body.Instructions)
            {
                if (jumpDestinations.Contains(instruction))
                {
                    currentSegment = new CodeSegment("SEG_" + (segmentLId++));
                    segments.Add(currentSegment);
                }
                if (currentSegment == null)
                    throw new ArgumentException("Method body is invalid!");
                currentSegment.AddInstruction(instruction);
            }

            return segments;
        }

        private CodeSegment GetSegment(IList<CodeSegment> segments, Instruction instruction)
        {
            foreach (CodeSegment segment in segments)
            {
                if (segment.Instructions.Contains(instruction))
                    return segment;
            }
            return null;
        }

        private void FindCalls(MethodBody body, IList<CodeSegment> segments)
        {
            foreach (Instruction instruction in body.Instructions)
            {
                OpCode code = instruction.OpCode;
                if (code.FlowControl == FlowControl.Branch || code.FlowControl == FlowControl.Cond_Branch)
                {
                    Instruction[] dsts;
                    switch (code.OperandType)
                    {
                        case OperandType.ShortInlineBrTarget:
                        case OperandType.InlineBrTarget:
                            dsts = new[] {(Instruction) instruction.Operand};
                            break;
                        case OperandType.InlineSwitch:
                            dsts = (Instruction[]) instruction.Operand;
                            break;
                        default:
                            throw new NotImplementedException("Unknown branch instruction " + instruction + "  " +
                                                              instruction.Operand.GetType());
                    }

                    CodeSegment ourSeg = dsts.Length > 0 ? GetSegment(segments, instruction) : null;
                    foreach (Instruction dst in dsts)
                        GetSegment(segments, dst).AddCall(ourSeg, instruction);
                }
            }
        }

        private void CheckNoIncomings(MethodBody body, IList<CodeSegment> segments)
        {
            IList<Instruction> jumpDestinations = new List<Instruction>();
            foreach (Instruction instruction in body.Instructions)
            {
                OpCode code = instruction.OpCode;
                if (code.FlowControl == FlowControl.Branch || code.FlowControl == FlowControl.Cond_Branch)
                {
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
                else if (instruction.Previous != null && instruction.Previous.OpCode.FlowControl == FlowControl.Branch &&
                         !jumpDestinations.Contains(instruction))
                    GetSegment(segments, instruction).ForceNoIncomings = true;
            }
        }


        private void FillSegmentInfo(IList<CodeSegment> segments, CodeSegment segment, bool methodReturns,
            IList<CodeSegment> filledSegments)
        {
            if (filledSegments.Contains(segment))
                return;
            filledSegments.Add(segment);

            int stackSize = segment.ForceNoIncomings ? 0 : segment.IncomingSize;
            if (stackSize == -1)
                throw new NotSupportedException("Unable to fill in information for segment with to incoming info");
            bool foundBranch = false;
            foreach (Instruction instruction in segment.Instructions)
            {
                int popSize = 0, pushSize = 0;
                ComputeStackDelta(instruction, ref popSize, ref pushSize);

                stackSize += popSize;
                if (stackSize < 0)
                    throw new NotSupportedException("Stack size is negative! " + instruction);
                stackSize += pushSize;

                if (foundBranch && popSize != 0 && pushSize != 0)
                    throw new NotSupportedException("Instructions can not change stack after a branch! " + instruction);

                OpCode code = instruction.OpCode;
                if (code.FlowControl == FlowControl.Branch || code.FlowControl == FlowControl.Cond_Branch)
                {
                    foundBranch = true;
                    Instruction[] dsts;
                    switch (code.OperandType)
                    {
                        case OperandType.ShortInlineBrTarget:
                        case OperandType.InlineBrTarget:
                            dsts = new[] {(Instruction) instruction.Operand};
                            break;
                        case OperandType.InlineSwitch:
                            dsts = (Instruction[]) instruction.Operand;
                            break;
                        default:
                            throw new NotImplementedException("Unknown branch instruction " + instruction + "  " +
                                                              instruction.Operand.GetType());
                    }

//                    CodeSegment ourSeg = dsts.Length > 0 ? GetSegment(segments, instruction) : null;
                    foreach (Instruction dst in dsts)
                    {
                        CodeSegment other = GetSegment(segments, dst);
                        other.UpdateIncomingSize(stackSize);
                        FillSegmentInfo(segments, other, methodReturns, filledSegments);
                    }
                }
            }
        }

        static void ComputeStackDelta(Instruction instruction, ref int popSize, ref int pushSize)
        {
            switch (instruction.OpCode.FlowControl)
            {
                case FlowControl.Call:
                {
                    var method = (IMethodSignature) instruction.Operand;
                    // pop 'this' argument
                    if (method.HasThis && !method.ExplicitThis && instruction.OpCode.Code != Code.Newobj)
                        popSize--;
                    // pop normal arguments
                    if (method.HasParameters)
                        popSize -= method.Parameters.Count;
                    // pop function pointer
                    if (instruction.OpCode.Code == Code.Calli)
                        popSize--;
                    // push return value
                    if (method.ReturnType.MetadataType != MetadataType.Void || instruction.OpCode.Code == Code.Newobj)
                        pushSize++;
                    break;
                }
                default:
                    ComputePopDelta(instruction.OpCode.StackBehaviourPop, ref popSize);
                    ComputePushDelta(instruction.OpCode.StackBehaviourPush, ref pushSize);
                    break;
            }
        }

        static void ComputePopDelta(StackBehaviour pop_behavior, ref int stack_size)
        {
            switch (pop_behavior)
            {
                case StackBehaviour.Popi:
                case StackBehaviour.Popref:
                case StackBehaviour.Pop1:
                    stack_size--;
                    break;
                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popi_pop1:
                case StackBehaviour.Popi_popi:
                case StackBehaviour.Popi_popi8:
                case StackBehaviour.Popi_popr4:
                case StackBehaviour.Popi_popr8:
                case StackBehaviour.Popref_pop1:
                case StackBehaviour.Popref_popi:
                    stack_size -= 2;
                    break;
                case StackBehaviour.Popi_popi_popi:
                case StackBehaviour.Popref_popi_popi:
                case StackBehaviour.Popref_popi_popi8:
                case StackBehaviour.Popref_popi_popr4:
                case StackBehaviour.Popref_popi_popr8:
                case StackBehaviour.Popref_popi_popref:
                    stack_size -= 3;
                    break;
                case StackBehaviour.PopAll:
                    stack_size = 0;
                    break;
            }
        }

        static void ComputePushDelta(StackBehaviour push_behaviour, ref int stack_size)
        {
            switch (push_behaviour)
            {
                case StackBehaviour.Push1:
                case StackBehaviour.Pushi:
                case StackBehaviour.Pushi8:
                case StackBehaviour.Pushr4:
                case StackBehaviour.Pushr8:
                case StackBehaviour.Pushref:
                    stack_size++;
                    break;
                case StackBehaviour.Push1_push1:
                    stack_size += 2;
                    break;
            }
        }


        private void PrintSegments(IList<CodeSegment> segments, bool methodReturns)
        {
            Console.WriteLine("  Segments:");
            foreach (CodeSegment segment in segments)
            {
                Console.WriteLine($"    {segment.Name}:");
                Console.WriteLine($"      Force No Incomings: {segment.ForceNoIncomings}");
                Console.WriteLine($"      Incoming Size: {segment.IncomingSize}");
                Console.WriteLine($"      Calls: {segment.Calls.Count}");
                Console.WriteLine($"      End Point: {segment.IsEndPoint}");
                foreach (Instruction instruction in segment.Instructions)
                {
                    Console.WriteLine("        " + instruction);
                    //                    Console.WriteLine("        " + instruction + " Pops " + CalculatePopSize(instruction, methodReturns));
                }
            }
        }

        public static void PrintScope(Scope scope, string front)
        {
            if (scope == null)
            {
                Console.WriteLine($"{front}null scope");
                return;
            }
            Console.WriteLine($"{front}Start {scope.Start}    Stop {scope.End}");
            foreach (Scope scopeScope in scope.Scopes)
            {
                Console.WriteLine($"{front}Child");
                PrintScope(scopeScope, front + "\t");
            }

            throw new Exception("Not null?");
        }
    }
}