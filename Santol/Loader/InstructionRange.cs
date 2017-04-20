using System;
using Mono.Cecil.Cil;

namespace Santol.Loader
{
    public class InstructionRange : IEquatable<InstructionRange>
    {
        public Instruction Start { get; }
        public Instruction End { get; }

        public InstructionRange(Instruction start, Instruction end)
        {
            Start = start;
            End = end;
        }

        public bool IsContained(Instruction i)
        {
            if (Start == i || End == i)
                return true;

            Instruction current = Start.Next;
            while (current != End)
            {
                if (current == i)
                    return true;
                current = current.Next;
            }
            return false;
        }

        public bool IsContained(InstructionRange other)
        {
            return IsContained(other.Start, other.End);
        }

        public bool IsContained(Instruction start, Instruction end)
        {
            bool found = false;
            Instruction current = Start;
            do
            {
                if (current == start)
                    found = true;
                else if (current == End)
                    return false;
                current = current.Next;
            } while (!found);

            found = false;
            current = start;
            do
            {
                if (current == end)
                    found = true;
                else if (current == End)
                    return false;
                current = current.Next;
            } while (!found);

            return true;
        }

        public bool Equals(InstructionRange other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Start.Equals(other.Start) && End.Equals(other.End);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((InstructionRange) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Start.GetHashCode() * 397) ^ End.GetHashCode();
            }
        }

        public static bool operator ==(InstructionRange left, InstructionRange right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(InstructionRange left, InstructionRange right)
        {
            return !Equals(left, right);
        }
    }
}