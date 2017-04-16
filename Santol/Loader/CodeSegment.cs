using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Santol.Nodes;

namespace Santol.Loader
{
    public class CodeSegment
    {
        public MethodInfo Method { get; }
        public string Name { get; }
        public IList<Instruction> Instructions { get; }
        public bool IsEndPoint => Instructions.Last().OpCode.FlowControl == FlowControl.Return;
        public bool ForceNoIncomings { get; set; }
        public bool HasIncoming => !ForceNoIncomings && Incoming != null && Incoming.Length > 0;
        public TypeReference[] Incoming { get; set; }
        public IList<CodeSegment> Callers { get; }
        public IList<Node> Nodes { get; }
        private Stack<Node> _nodeStack;

        public CodeSegment(MethodInfo method, string name)
        {
            Method = method;
            Name = name;
            Instructions = new List<Instruction>();
            Nodes = new List<Node>();
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
            else if (types.Length != Incoming.Length)
            {
                throw new NotSupportedException("Unable to handle a different number of incoming types!");
            }
            else if (!types.SequenceEqual(Incoming))
            {
                for (int i = 0; i < Incoming.Length; i++)
                {
                    if (Incoming[i].Equals(types[i]))
                        continue;

                    Incoming[i] = TypeHelper.GetSimplestType(Incoming[i], types[i]);
                }
            }
        }

        private void PushNode(Node node)
        {
            Nodes.Add(node);
            if (node.HasResult)
                _nodeStack.Push(node);
        }

        private NodeReference AddNode(Node node)
        {
            Nodes.Add(node);
            return node.TakeReference();
        }

        private NodeReference PopNode()
        {
            return _nodeStack.Pop().TakeReference();
        }

        private Tuple<TypeReference[], NodeReference[]> GetStackInfo()
        {
            Node[] nodes = _nodeStack.ToArray();
            TypeReference[] types = new TypeReference[nodes.Length];
            NodeReference[] refs = new NodeReference[nodes.Length];
            for (int i = 0; i < types.Length; i++)
            {
                Node node = nodes[i];
                types[i] = node.ResultType;
                refs[i] = node.TakeReference();
            }
            return new Tuple<TypeReference[], NodeReference[]>(types, refs);
        }

        public void ParseInstructions(TypeSystem typeSystem)
        {
            Nodes.Clear();
            _nodeStack = new Stack<Node>();

            if (HasIncoming)
                for (int i = 0; i < Incoming.Length; i++)
                    PushNode(new IncomingValue(Incoming[i], i));

            foreach (Instruction instruction in Instructions)
            {
                switch (instruction.OpCode.Code)
                {
                    case Code.Nop:
                        break;

                    case Code.Ldc_I4:
                        PushNode(new LoadPrimitiveConstant(typeSystem.Int32, instruction.Operand));
                        break;
                    case Code.Ldc_I8:
                        PushNode(new LoadPrimitiveConstant(typeSystem.Int64, instruction.Operand));
                        break;
                    case Code.Ldc_R4:
                        PushNode(new LoadPrimitiveConstant(typeSystem.Single, instruction.Operand));
                        break;
                    case Code.Ldc_R8:
                        PushNode(new LoadPrimitiveConstant(typeSystem.Double, instruction.Operand));
                        break;


                    case Code.Ldloc:
                        PushNode(new LoadLocal((VariableDefinition) instruction.Operand));
                        break;
                    case Code.Ldarg:
                        PushNode(new LoadArg((ParameterDefinition) instruction.Operand));
                        break;
                    case Code.Ldsfld:
                        PushNode(new LoadStatic((FieldReference) instruction.Operand));
                        break;
                    case Code.Ldfld:
                        PushNode(new LoadField(PopNode(), (FieldReference) instruction.Operand));
                        break;

                    case Code.Ldind_U1:
                        PushNode(new LoadDirect(typeSystem.Byte, PopNode()));
                        break;

                    case Code.Stloc:
                        PushNode(new StoreLocal((VariableDefinition) instruction.Operand, PopNode()));
                        break;
                    case Code.Stsfld:
                        PushNode(new StoreStatic((FieldReference) instruction.Operand, PopNode()));
                        break;

                    case Code.Stind_I1:
                        PushNode(new StoreDirect(typeSystem.Byte, PopNode(), PopNode()));
                        break;

                    case Code.Conv_I:
                        PushNode(new Nodes.Convert(typeSystem.IntPtr, PopNode()));
                        break;
                    case Code.Conv_I8:
                        PushNode(new Nodes.Convert(typeSystem.Int64, PopNode()));
                        break;
                    case Code.Conv_U:
                        PushNode(new Nodes.Convert(typeSystem.UIntPtr, PopNode()));
                        break;
                    case Code.Conv_U1:
                        PushNode(new Nodes.Convert(typeSystem.Byte, PopNode()));
                        break;
                    case Code.Conv_U2:
                        PushNode(new Nodes.Convert(typeSystem.UInt16, PopNode()));
                        break;

                    case Code.Add:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (lhs.ResultType != rhs.ResultType)
                            throw new NotSupportedException("Can not add two different types!");
                        PushNode(new Numeric(Numeric.Operations.Add, lhs, rhs));
                        break;
                    }
                    case Code.Sub:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (lhs.ResultType != rhs.ResultType)
                            throw new NotSupportedException("Can not subtract two different types!");
                        PushNode(new Numeric(Numeric.Operations.Subtract, lhs, rhs));
                        break;
                    }
                    case Code.Mul:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (lhs.ResultType != rhs.ResultType)
                            throw new NotSupportedException("Can not multiply two different types!");
                        PushNode(new Numeric(Numeric.Operations.Multiply, lhs, rhs));
                        break;
                    }
                    case Code.Div:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (lhs.ResultType != rhs.ResultType)
                            throw new NotSupportedException("Can not divide two different types!");
                        PushNode(new Numeric(Numeric.Operations.Divide, lhs, rhs));
                        break;
                    }
                    case Code.Rem:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (lhs.ResultType != rhs.ResultType)
                            throw new NotSupportedException("Can not find remainder of two different types!");
                        PushNode(new Numeric(Numeric.Operations.Remainder, lhs, rhs));
                        break;
                    }
                    case Code.Shl:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        PushNode(new Numeric(Numeric.Operations.ShiftLeft, lhs, rhs));
                        break;
                    }
                    case Code.Or:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (lhs.ResultType != rhs.ResultType)
                            throw new NotSupportedException("Can not or two different types!");
                        PushNode(new Numeric(Numeric.Operations.Or, lhs, rhs));
                        break;
                    }
                    case Code.Xor:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (lhs.ResultType != rhs.ResultType)
                            throw new NotSupportedException("Can not xor two different types!");
                        PushNode(new Numeric(Numeric.Operations.XOr, lhs, rhs));
                        break;
                    }

                    case Code.Clt:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        PushNode(new Comparison(typeSystem, Comparison.Operations.LessThan, lhs, rhs));
                        break;
                    }
                    case Code.Cgt:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        PushNode(new Comparison(typeSystem, Comparison.Operations.GreaterThan, lhs, rhs));
                        break;
                    }
                    case Code.Ceq:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        PushNode(new Comparison(typeSystem, Comparison.Operations.Equal, lhs, rhs));
                        break;
                    }

                    case Code.Call:
                    {
                        MethodReference method = (MethodReference) instruction.Operand;

                        int argCount = method.Parameters.Count + (method.HasThis && !method.ExplicitThis ? 1 : 0);
                        NodeReference[] args = new NodeReference[argCount];

                        for (int i = 0; i < args.Length; i++)
                            args[args.Length - 1 - i] = PopNode();

                        PushNode(new Call(method, args));
                        break;
                    }
                    case Code.Br:
                    {
                        CodeSegment segment = Method.GetSegment((Instruction) instruction.Operand);
                        Tuple<TypeReference[], NodeReference[]> stack = GetStackInfo();
                        segment.AddCaller(this, stack.Item1);
                        PushNode(new Branch(segment, stack.Item2));
                        return;
                    }
                    case Code.Brtrue:
                    {
                        NodeReference cond = PopNode();
                        CodeSegment segment = Method.GetSegment((Instruction) instruction.Operand);
                        CodeSegment elseSegment = Method.GetSegment((Instruction) instruction.Next.Operand);
                        Tuple<TypeReference[], NodeReference[]> stack = GetStackInfo();
                        segment.AddCaller(this, stack.Item1);
                        elseSegment.AddCaller(this, stack.Item1);
                        PushNode(new ConditionalBranch(segment, elseSegment, cond, stack.Item2));
                        return;
                    }
                    case Code.Brfalse:
                    {
                        NodeReference cond = PopNode();
                        CodeSegment segment = Method.GetSegment((Instruction) instruction.Operand);
                        CodeSegment elseSegment = Method.GetSegment((Instruction) instruction.Next.Operand);
                        Tuple<TypeReference[], NodeReference[]> stack = GetStackInfo();
                        segment.AddCaller(this, stack.Item1);
                        elseSegment.AddCaller(this, stack.Item1);
                        //TODO: Check whether this is valid in all cases
                        PushNode(new ConditionalBranch(elseSegment, segment, cond, stack.Item2));
                        return;
                    }
                    case Code.Blt:
                    {
                        NodeReference v2 = PopNode();
                        NodeReference v1 = PopNode();
                        NodeReference cond = AddNode(new Comparison(typeSystem, Comparison.Operations.LessThan, v1, v2));
                        CodeSegment segment = Method.GetSegment((Instruction) instruction.Operand);
                        CodeSegment elseSegment = Method.GetSegment((Instruction) instruction.Next.Operand);
                        Tuple<TypeReference[], NodeReference[]> stack = GetStackInfo();
                        segment.AddCaller(this, stack.Item1);
                        elseSegment.AddCaller(this, stack.Item1);
                        PushNode(new ConditionalBranch(segment, elseSegment, cond, stack.Item2));
                        return;
                    }
                    case Code.Bge:
                    {
                        NodeReference v2 = PopNode();
                        NodeReference v1 = PopNode();
                        NodeReference cond =
                            AddNode(new Comparison(typeSystem, Comparison.Operations.GreaterThanOrEqual, v1, v2));
                        CodeSegment segment = Method.GetSegment((Instruction) instruction.Operand);
                        CodeSegment elseSegment = Method.GetSegment((Instruction) instruction.Next.Operand);
                        Tuple<TypeReference[], NodeReference[]> stack = GetStackInfo();
                        segment.AddCaller(this, stack.Item1);
                        elseSegment.AddCaller(this, stack.Item1);
                        PushNode(new ConditionalBranch(segment, elseSegment, cond, stack.Item2));
                        return;
                    }
                    case Code.Ret:
                        PushNode(new Return(Method.DoesReturn ? PopNode() : null));
                        return;
                    default:
                    {
                        Tuple<TypeReference[], NodeReference[]> stack = GetStackInfo();
                        for (int i = 0; i < stack.Item1.Length; i++)
                        {
                            Console.WriteLine($"{i}: {stack.Item1[i]}   {stack.Item2[i]}");
                        }
                        throw new NotImplementedException("Unknown opcode " + instruction);
                    }
                }
            }
        }
    }
}