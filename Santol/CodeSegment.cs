using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Santol
{
    public class CodeSegment
    {
        public string Name { get; }
        public Instruction FirstInstruction => Instructions[0];
        public IList<Instruction> Instructions { get; }
        public bool IsEndPoint => Instructions.Last().OpCode.FlowControl == FlowControl.Return;
        public IList<Tuple<CodeSegment, Instruction>> Calls { get; }
        public bool ForceNoIncomings { get; set; }
        public int IncomingSize { get; set; } = -1;

        public CodeSegment(string name)
        {
            Name = name;
            Instructions = new List<Instruction>();
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
            if(stackSize != IncomingSize && IncomingSize != -1)
                throw new NotSupportedException("Different incoming stack sizes?");
            IncomingSize = stackSize;
        }
    }
}