using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Santol
{
    public class CodeSegment
    {
        public MethodDefinition Method { get; }
        public string Name { get; }
        public IList<Instruction> Instructions { get; }
        public bool IsEndPoint => Instructions.Last().OpCode.FlowControl == FlowControl.Return;
        public bool ForceNoIncomings { get; set; }
        public bool HasIncoming => !ForceNoIncomings && Incoming != null && Incoming.Length > 0;
        public TypeReference[] Incoming { get; set; }
        public IList<CodeSegment> Callers { get; }
        public IList<IOperation> Operations { get; }

        public CodeSegment(MethodDefinition method, string name)
        {
            Method = method;
            Name = name;
            Instructions = new List<Instruction>();
            Operations = new List<IOperation>();
            Callers = new List<CodeSegment>();
        }

        public void AddInstruction(Instruction instruction)
        {
            Instructions.AddOpt(instruction);
        }

        public void AddCaller(CodeSegment segment, TypeReference[] types)
        {
            Callers.Add(segment);

            if (Incoming == null)
            {
                Incoming = types;
            }
            else if (!types.SequenceEqual(Incoming) || types.Length != Incoming.Length)
            {
                throw new NotSupportedException("Unable to handle different incoming types!");
            }
        }

        public void ParseInstructions(TypeSystem typeSystem)
        {
            Operations.Clear();

            Stack<TypeReference> typeStack = HasIncoming
                ? new Stack<TypeReference>(Incoming)
                : new Stack<TypeReference>();

            foreach (Instruction instruction in Instructions)
            {
                switch (instruction.OpCode.Code)
                {
                    case Code.Nop:
                        break;
                    case Code.Ldnull:
                    {
                        IOperation opp = new LoadNullConstant();
                        typeStack.Push(opp.ResultType);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Ldc_I4:
                    {
                        IOperation opp = new LoadPrimitiveConstant(typeSystem.Int32, instruction.Operand);
                        typeStack.Push(opp.ResultType);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Ldc_I8:
                    {
                        IOperation opp = new LoadPrimitiveConstant(typeSystem.Int64, instruction.Operand);
                        typeStack.Push(opp.ResultType);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Ldc_R4:
                    {
                        IOperation opp = new LoadPrimitiveConstant(typeSystem.Single, instruction.Operand);
                        typeStack.Push(opp.ResultType);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Ldc_R8:
                    {
                        IOperation opp = new LoadPrimitiveConstant(typeSystem.Double, instruction.Operand);
                        typeStack.Push(opp.ResultType);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Ldloc:
                    {
                        IOperation opp = new LoadLocal((Mono.Cecil.Cil.VariableDefinition) instruction.Operand);
                        typeStack.Push(opp.ResultType);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Ldarg:
                    {
                        IOperation opp = new LoadArg((ParameterDefinition) instruction.Operand);
                        typeStack.Push(opp.ResultType);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Stloc:
                    {
                        TypeReference v1 = typeStack.Pop();
                        Operations.Add(new StoreLocal((Mono.Cecil.Cil.VariableDefinition) instruction.Operand, v1));
                        break;
                    }
                    case Code.Stind_I1:
                    {
                        TypeReference v2 = typeStack.Pop();
                        TypeReference v1 = typeStack.Pop();
                        Operations.Add(new StoreDirect(typeSystem.Byte, v2, v1));
                        break;
                    }
                    case Code.Conv_I:
                    {
                        TypeReference v1 = typeStack.Pop();
                        IOperation opp = new Convert(v1, typeSystem.IntPtr);
                        typeStack.Push(opp.ResultType);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Conv_U1:
                    {
                        TypeReference v1 = typeStack.Pop();
                        IOperation opp = new Convert(v1, typeSystem.Byte);
                        typeStack.Push(opp.ResultType);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Add:
                    {
                        TypeReference v2 = typeStack.Pop();
                        TypeReference v1 = typeStack.Pop();
                        if (v1 != v2)
                            throw new NotSupportedException("Can not add two different types!");

                        IOperation opp = new Numeric(Numeric.Operations.Add, v1, v2, v1);
                        typeStack.Push(opp.ResultType);
                        Operations.Add(opp);
                        break;
                    }

                    case Code.Clt:
                    {
                        TypeReference v2 = typeStack.Pop();
                        TypeReference v1 = typeStack.Pop();
                        IOperation opp = new Comparison(typeSystem, Comparison.Operations.LessThan, v1, v2);
                        typeStack.Push(opp.ResultType);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Call:
                    {
                        Mono.Cecil.MethodDefinition method = (Mono.Cecil.MethodDefinition) instruction.Operand;

                        int typeCount = method.Parameters.Count + (method.HasThis && !method.ExplicitThis ? 1 : 0);
                        TypeReference[] types = new TypeReference[typeCount];

                        for (int i = 0; i < types.Length; i++)
                            types[types.Length - 1 - i] = typeStack.Pop();

                        IOperation opp = new Call(method, types);
                        typeStack.Push(opp.ResultType);
                        Operations.Add(opp);
                        break;
                    }
                    case Code.Br:
                    {
                        CodeSegment segment = Method.GetSegment((Instruction) instruction.Operand);
                        segment.AddCaller(this, typeStack.ToArray());
                        Operations.Add(new Branch(segment));
                        return;
                    }
                    case Code.Brtrue:
                    {
                        TypeReference v1 = typeStack.Pop();
                        CodeSegment segment = Method.GetSegment((Instruction) instruction.Operand);
                        CodeSegment elseSegment = Method.GetSegment((Instruction) instruction.Next.Operand);
                        TypeReference[] stack = typeStack.ToArray();
                        segment.AddCaller(this, stack);
                        elseSegment.AddCaller(this, stack);
                        Operations.Add(new ConditionalBranch(segment, elseSegment, ConditionalBranch.Types.True, v1));
                        return;
                    }
                    case Code.Ret:
                    {
                        TypeReference type = Method.DoesReturn ? typeStack.Pop() : null;
                        Operations.Add(new Return(type));
                        return;
                    }
                    default:
                        throw new NotImplementedException("Unknown opcode " + instruction);
                }
            }
        }
    }
}