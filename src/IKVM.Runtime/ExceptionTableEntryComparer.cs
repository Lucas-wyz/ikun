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
using System.Collections.Generic;

using ExceptionTableEntry = IKVM.Runtime.ClassFile.Method.ExceptionTableEntry;

namespace IKVM.Runtime
{

    /// <summary>
    /// Implements the <see cref="IComparer{T}"/> interface for <see cref="ExceptionTableEntry"/>.
    /// </summary>
    sealed class ExceptionTableEntryComparer : IComparer<ExceptionTableEntry>
    {

        /// <inheritdoc />
        public int Compare(ExceptionTableEntry e1, ExceptionTableEntry e2)
        {
            if (e1.startIndex < e2.startIndex)
                return -1;

            if (e1.startIndex == e2.startIndex)
            {
                if (e1.endIndex == e2.endIndex)
                {
                    if (e1.ordinal > e2.ordinal)
                        return -1;

                    if (e1.ordinal == e2.ordinal)
                        return 0;

                    return 1;
                }

                if (e1.endIndex > e2.endIndex)
                    return -1;
            }

            return 1;
        }

    }

}
