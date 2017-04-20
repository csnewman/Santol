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
        public bool ForceNoIncomings { get; set; }
        public bool HasIncoming => !ForceNoIncomings && Incoming != null && Incoming.Length > 0;
        public TypeReference[] Incoming { get; set; }
        public CodeRegion Region { get; }
        public IList<CodeSegment> Callers { get; }
        public IList<Node> Nodes { get; }
        private Stack<Node> _nodeStack;

        public CodeSegment(MethodInfo method, string name, CodeRegion region)
        {
            Method = method;
            Name = name;
            Instructions = new List<Instruction>();
            Nodes = new List<Node>();
            Region = region;
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

        public void ParseInstructions(Compiler compiler)
        {
            Nodes.Clear();
            _nodeStack = new Stack<Node>();

            if (HasIncoming)
                for (int i = 0; i < Incoming.Length; i++)
                    PushNode(new IncomingValue(compiler, Incoming[i], i));

            foreach (Instruction instruction in Instructions)
            {
                switch (instruction.OpCode.Code)
                {
                    case Code.Nop:
                        break;

                    case Code.Ldc_I4:
                        PushNode(new LoadPrimitiveConstant(compiler, compiler.TypeSystem.Int32, instruction.Operand));
                        break;
                    case Code.Ldc_I8:
                        PushNode(new LoadPrimitiveConstant(compiler, compiler.TypeSystem.Int64, instruction.Operand));
                        break;
                    case Code.Ldc_R4:
                        PushNode(new LoadPrimitiveConstant(compiler, compiler.TypeSystem.Single, instruction.Operand));
                        break;
                    case Code.Ldc_R8:
                        PushNode(new LoadPrimitiveConstant(compiler, compiler.TypeSystem.Double, instruction.Operand));
                        break;


                    case Code.Ldloc:
                        PushNode(new LoadLocal(compiler, (VariableDefinition) instruction.Operand));
                        break;
                    case Code.Ldarg:
                        PushNode(new LoadArg(compiler, (ParameterDefinition) instruction.Operand));
                        break;
                    case Code.Ldsfld:
                        PushNode(new LoadStatic(compiler, (FieldReference) instruction.Operand));
                        break;
                    case Code.Ldfld:
                        PushNode(new LoadField(compiler, PopNode(), (FieldReference) instruction.Operand));
                        break;

                    case Code.Ldind_U1:
                        PushNode(new LoadDirect(compiler, compiler.TypeSystem.Byte, PopNode()));
                        break;

                    case Code.Stloc:
                        PushNode(new StoreLocal(compiler, (VariableDefinition) instruction.Operand, PopNode()));
                        break;
                    case Code.Stsfld:
                        PushNode(new StoreStatic(compiler, (FieldReference) instruction.Operand, PopNode()));
                        break;

                    case Code.Stind_I1:
                        PushNode(new StoreDirect(compiler, compiler.TypeSystem.Byte, PopNode(), PopNode()));
                        break;

                    case Code.Conv_I:
                        PushNode(new Nodes.Convert(compiler, compiler.TypeSystem.IntPtr, PopNode()));
                        break;
                    case Code.Conv_I8:
                        PushNode(new Nodes.Convert(compiler, compiler.TypeSystem.Int64, PopNode()));
                        break;
                    case Code.Conv_U:
                        PushNode(new Nodes.Convert(compiler, compiler.TypeSystem.UIntPtr, PopNode()));
                        break;
                    case Code.Conv_U1:
                        PushNode(new Nodes.Convert(compiler, compiler.TypeSystem.Byte, PopNode()));
                        break;
                    case Code.Conv_U2:
                        PushNode(new Nodes.Convert(compiler, compiler.TypeSystem.UInt16, PopNode()));
                        break;

                    case Code.Add:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (lhs.ResultType != rhs.ResultType)
                            throw new NotSupportedException("Can not add two different types!");
                        PushNode(new Numeric(compiler, Numeric.OperationType.Add, lhs, rhs));
                        break;
                    }
                    case Code.Sub:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (lhs.ResultType != rhs.ResultType)
                            throw new NotSupportedException("Can not subtract two different types!");
                        PushNode(new Numeric(compiler, Numeric.OperationType.Subtract, lhs, rhs));
                        break;
                    }
                    case Code.Mul:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (lhs.ResultType != rhs.ResultType)
                            throw new NotSupportedException("Can not multiply two different types!");
                        PushNode(new Numeric(compiler, Numeric.OperationType.Multiply, lhs, rhs));
                        break;
                    }
                    case Code.Div:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (lhs.ResultType != rhs.ResultType)
                            throw new NotSupportedException("Can not divide two different types!");
                        PushNode(new Numeric(compiler, Numeric.OperationType.Divide, lhs, rhs));
                        break;
                    }
                    case Code.Rem:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (lhs.ResultType != rhs.ResultType)
                            throw new NotSupportedException("Can not find remainder of two different types!");
                        PushNode(new Numeric(compiler, Numeric.OperationType.Remainder, lhs, rhs));
                        break;
                    }
                    case Code.Shl:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        PushNode(new Numeric(compiler, Numeric.OperationType.ShiftLeft, lhs, rhs));
                        break;
                    }
                    case Code.Or:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (lhs.ResultType != rhs.ResultType)
                            throw new NotSupportedException("Can not or two different types!");
                        PushNode(new Numeric(compiler, Numeric.OperationType.Or, lhs, rhs));
                        break;
                    }
                    case Code.Xor:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        if (lhs.ResultType != rhs.ResultType)
                            throw new NotSupportedException("Can not xor two different types!");
                        PushNode(new Numeric(compiler, Numeric.OperationType.XOr, lhs, rhs));
                        break;
                    }

                    case Code.Clt:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        PushNode(new Comparison(compiler, Comparison.OperationType.LessThan, lhs, rhs));
                        break;
                    }
                    case Code.Cgt:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        PushNode(new Comparison(compiler, Comparison.OperationType.GreaterThan, lhs, rhs));
                        break;
                    }
                    case Code.Ceq:
                    {
                        NodeReference rhs = PopNode();
                        NodeReference lhs = PopNode();
                        PushNode(new Comparison(compiler, Comparison.OperationType.Equal, lhs, rhs));
                        break;
                    }

                    case Code.Call:
                    {
                        MethodReference method = (MethodReference) instruction.Operand;

                        int argCount = method.Parameters.Count + (method.HasThis && !method.ExplicitThis ? 1 : 0);
                        NodeReference[] args = new NodeReference[argCount];

                        for (int i = 0; i < args.Length; i++)
                            args[args.Length - 1 - i] = PopNode();

                        PushNode(new Call(compiler, method, args));
                        break;
                    }
                    case Code.Callvirt:
                    {
                        MethodReference method = (MethodReference) instruction.Operand;

                        if (!method.HasThis)
                            throw new NotImplementedException("Expected this parameter");

                        int argCount = method.Parameters.Count - (method.ExplicitThis ? 1 : 0);
                        NodeReference[] args = new NodeReference[argCount];

                        for (int i = 0; i < args.Length; i++)
                            args[args.Length - 1 - i] = PopNode();


                        PushNode(new CallVirtual(compiler, method, PopNode(), args));
                        break;
                    }

                    case Code.Br:
                    {
                        CodeSegment segment = Method.GetSegment((Instruction) instruction.Operand);
                        Tuple<TypeReference[], NodeReference[]> stack = GetStackInfo();
                        segment.AddCaller(this, stack.Item1);
                        PushNode(new Branch(compiler, segment, stack.Item2));
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
                        PushNode(new ConditionalBranch(compiler, segment, elseSegment, cond, stack.Item2));
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
                        PushNode(new ConditionalBranch(compiler, elseSegment, segment, cond, stack.Item2));
                        return;
                    }
                    case Code.Blt:
                    {
                        NodeReference v2 = PopNode();
                        NodeReference v1 = PopNode();
                        NodeReference cond = AddNode(new Comparison(compiler, Comparison.OperationType.LessThan, v1, v2));
                        CodeSegment segment = Method.GetSegment((Instruction) instruction.Operand);
                        CodeSegment elseSegment = Method.GetSegment((Instruction) instruction.Next.Operand);
                        Tuple<TypeReference[], NodeReference[]> stack = GetStackInfo();
                        segment.AddCaller(this, stack.Item1);
                        elseSegment.AddCaller(this, stack.Item1);
                        PushNode(new ConditionalBranch(compiler, segment, elseSegment, cond, stack.Item2));
                        return;
                    }
                    case Code.Bge:
                    {
                        NodeReference v2 = PopNode();
                        NodeReference v1 = PopNode();
                        NodeReference cond =
                            AddNode(new Comparison(compiler, Comparison.OperationType.GreaterThanOrEqual, v1, v2));
                        CodeSegment segment = Method.GetSegment((Instruction) instruction.Operand);
                        CodeSegment elseSegment = Method.GetSegment((Instruction) instruction.Next.Operand);
                        Tuple<TypeReference[], NodeReference[]> stack = GetStackInfo();
                        segment.AddCaller(this, stack.Item1);
                        elseSegment.AddCaller(this, stack.Item1);
                        PushNode(new ConditionalBranch(compiler, segment, elseSegment, cond, stack.Item2));
                        return;
                    }
                    case Code.Ret:
                        PushNode(new Return(compiler, Method.DoesReturn ? PopNode() : null));
                        return;
                    default:
                    {
                        Tuple<TypeReference[], NodeReference[]> stack = GetStackInfo();
                        for (int i = 0; i < stack.Item1.Length; i++)
                        {
                            Console.WriteLine($"{i}: {stack.Item1[i]} {stack.Item2[i]}");
                        }
                        throw new NotImplementedException("Unknown opcode " + instruction);
                    }
                }
            }
        }
    }
}