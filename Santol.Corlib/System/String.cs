using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public sealed unsafe class String
    {
        public int Length { get; }
        private char* _firstChar;
    }
}