using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Santol
{
    public class CodeSegment
    {
        public MethodDefinition Method { get; }
        public string Name { get; }
//        public Instruction FirstInstruction => Instructions[0];
        public IList<Instruction> Instructions { get; }
        public bool IsEndPoint => Instructions.Last().OpCode.FlowControl == FlowControl.Return;

        public bool ForceNoIncomings { get; set; }
        public IList<IOperation> Operations { get; }


        //Unused
        public IList<Tuple<CodeSegment, Instruction>> Calls { get; }
        public int IncomingSize { get; set; } = -1;

        public CodeSegment(MethodDefinition method, string name)
        {
            Method = method;
            Name = name;
            Instructions = new List<Instruction>();
            Operations = new List<IOperation>();
            Calls = new List<Tuple<CodeSegment, Instruction>>();
        }

        public void AddInstruction(Instruction instruction)
        {
            Instructions.AddOpt(instruction);
        }

        public void AddCall(CodeSegment segment, Instruction instruction)
        {
            Calls.Add(new Tuple<CodeSegment, Instruction>(segment, instruction));
        }

        public void UpdateIncomingSize(int stackSize)
        {
            if (stackSize != IncomingSize && IncomingSize != -1)
                throw new NotSupportedException("Different incoming stack sizes?");
            IncomingSize = stackSize;
        }

        public void ParseInstructions(TypeSystem typeSystem)
        {
            Operations.Clear();

            bool reachedEnd = false;


            Stack<IOperation> operationStack = new Stack<IOperation>();

            foreach (Instruction instruction in Instructions)
            {
                if (reachedEnd)
                    throw new ArgumentException("End instruction has been reached, no more instructions were expected");

                switch (instruction.OpCode.Code)
                {
                    case Code.Nop:
                        break;
                    case Code.Ldnull:
                    {
                        IOperation opp = new LoadNullConstant();
                        operationStack.Push(opp);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Ldc_I4:
                    {
                        IOperation opp = new LoadPrimitiveConstant(typeSystem.Int32, instruction.Operand);
                        operationStack.Push(opp);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Ldc_I8:
                    {
                        IOperation opp = new LoadPrimitiveConstant(typeSystem.Int64, instruction.Operand);
                        operationStack.Push(opp);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Ldc_R4:
                    {
                        IOperation opp = new LoadPrimitiveConstant(typeSystem.Single, instruction.Operand);
                        operationStack.Push(opp);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Ldc_R8:
                    {
                        IOperation opp = new LoadPrimitiveConstant(typeSystem.Double, instruction.Operand);
                        operationStack.Push(opp);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Stloc:
                        Operations.Add(new StoreLocal((Mono.Cecil.Cil.VariableDefinition) instruction.Operand));
                        break;
                    case Code.Ldloc:
                    {
                        IOperation opp = new LoadLocal((Mono.Cecil.Cil.VariableDefinition) instruction.Operand);
                        operationStack.Push(opp);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Br:
                        Operations.Add(new Branch(Method.GetSegment((Instruction) instruction.Operand)));
                        reachedEnd = true;
                        break;
                    case Code.Add:
                    {
                        IOperation v2 = operationStack.Pop();
                        IOperation v1 = operationStack.Pop();
                        if (v1.ResultType != v2.ResultType)
                            throw new NotSupportedException("Can not add two different types!");

                        IOperation opp = new Numeric(Numeric.Actions.Add, v1.ResultType, v2.ResultType, v1.ResultType);
                        operationStack.Push(opp);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Conv_I:
                    {
                        IOperation v1 = operationStack.Pop();
                        IOperation opp = new Convert(v1.ResultType, typeSystem.IntPtr);
                        operationStack.Push(opp);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Stind_I1:
                    {
                        //(TypeReference) instruction.Operand
                        IOperation v2 = operationStack.Pop();
                        IOperation v1 = operationStack.Pop();
                        Operations.Add(new StoreDirect(typeSystem.Byte, v2.ResultType, v1.ResultType));
                        break;
                    }
                    case Code.Clt:
                    {
                        IOperation v2 = operationStack.Pop();
                        IOperation v1 = operationStack.Pop();
                        IOperation opp = new Comparison(typeSystem, Comparison.Actions.LessThan, v1.ResultType,
                            v2.ResultType);
                        operationStack.Push(opp);
                        Operations.Add(opp);
                        break;
                    }
                    default:
                        throw new NotImplementedException("Unknown opcode " + instruction);
                }
            }
        }
    }
}