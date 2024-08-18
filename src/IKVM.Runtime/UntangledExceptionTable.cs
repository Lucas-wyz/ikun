﻿/*
  Copyright (C) 2002-2014 Jeroen Frijters

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jeroen Frijters
  jeroen@frijters.net
  
*/
using System;

using ExceptionTableEntry = IKVM.Runtime.ClassFile.Method.ExceptionTableEntry;

namespace IKVM.Runtime
{

    struct UntangledExceptionTable
    {
        private readonly ExceptionTableEntry[] exceptions;

        internal UntangledExceptionTable(ExceptionTableEntry[] exceptions)
        {
#if DEBUG
            for (int i = 0; i < exceptions.Length; i++)
            {
                for (int j = i + 1; j < exceptions.Length; j++)
                {
                    // check for partially overlapping try blocks (which is legal for the JVM, but not the CLR)
                    if (exceptions[i].startIndex < exceptions[j].startIndex &&
                        exceptions[j].startIndex < exceptions[i].endIndex &&
                        exceptions[i].endIndex < exceptions[j].endIndex)
                    {
                        throw new InvalidOperationException("Partially overlapping try blocks is broken");
                    }
                    // check that we didn't destroy the ordering, when sorting
                    if (exceptions[i].startIndex <= exceptions[j].startIndex &&
                        exceptions[i].endIndex >= exceptions[j].endIndex &&
                        exceptions[i].ordinal < exceptions[j].ordinal)
                    {
                        throw new InvalidOperationException("Non recursive try blocks is broken");
                    }
                }
            }
#endif
            this.exceptions = exceptions;
        }

        internal ExceptionTableEntry this[int index]
        {
            get { return exceptions[index]; }
        }

        internal int Length
        {
            get { return exceptions.Length; }
        }

        internal void SetFinally(int index)
        {
            exceptions[index] = new ExceptionTableEntry(exceptions[index].startIndex, exceptions[index].endIndex, exceptions[index].handlerIndex, exceptions[index].catchType, exceptions[index].ordinal, true);
        }
    }

}
