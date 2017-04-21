﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Santol.Loader;

namespace Santol.Patchers
{
    public interface ISegmentPatcher
    {
        void Patch(Compiler compiler, MethodInfo method, CodeSegment segment);
    }
}
