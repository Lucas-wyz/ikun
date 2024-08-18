﻿/*
  Copyright (C) 2002-2015 Jeroen Frijters

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

using IKVM.ByteCode;
using IKVM.ByteCode.Decoding;

namespace IKVM.Runtime
{

    sealed partial class ClassFile
    {

        internal sealed class ConstantPoolItemFieldref : ConstantPoolItemFMI
        {

            RuntimeJavaField field;
            RuntimeJavaType fieldTypeWrapper;

            /// <summary>
            /// Initializes a new instance.
            /// </summary>
            /// <param name="context"></param>
            /// <param name="data"></param>
            internal ConstantPoolItemFieldref(RuntimeContext context, FieldrefConstantData data) :
                base(context, data.Class, data.NameAndType)
            {

            }

            protected override void Validate(string name, string descriptor, int majorVersion)
            {
                if (!IsValidFieldSig(descriptor))
                    throw new ClassFormatError("Invalid field signature \"{0}\"", descriptor);
                if (!IsValidFieldName(name, new ClassFormatVersion((ushort)majorVersion, 0)))
                    throw new ClassFormatError("Invalid field name \"{0}\"", name);
            }

            internal RuntimeJavaType GetFieldType()
            {
                return fieldTypeWrapper;
            }

            internal override void Link(RuntimeJavaType thisType, LoadMode mode)
            {
                base.Link(thisType, mode);
                lock (this)
                {
                    if (fieldTypeWrapper != null)
                    {
                        return;
                    }
                }
                RuntimeJavaField fw = null;
                RuntimeJavaType wrapper = GetClassType();
                if (wrapper == null)
                {
                    return;
                }
                if (!wrapper.IsUnloadable)
                {
                    fw = wrapper.GetFieldWrapper(Name, Signature);
                    if (fw != null)
                    {
                        fw.Link(mode);
                    }
                }
                RuntimeClassLoader classLoader = thisType.GetClassLoader();
                RuntimeJavaType fld = classLoader.FieldTypeWrapperFromSig(this.Signature, mode);
                lock (this)
                {
                    if (fieldTypeWrapper == null)
                    {
                        fieldTypeWrapper = fld;
                        field = fw;
                    }
                }
            }

            internal RuntimeJavaField GetField()
            {
                return field;
            }

            internal override RuntimeJavaMember GetMember()
            {
                return field;
            }

        }

    }

}
