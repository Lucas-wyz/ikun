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
using System.Text;

using IKVM.ByteCode.Text;
using IKVM.Runtime.Extensions;

namespace IKVM.Runtime.JNI
{

#if FIRST_PASS == false && IMPORTER == false && EXPORTER == false

    using jsize = System.Int32;

    sealed unsafe partial class JNIVM
    {

        static readonly MUTF8Encoding MUTF8 = MUTF8Encoding.GetMUTF8(52);

        internal static volatile bool jvmCreated;
        internal static volatile bool jvmDestroyed;

#if NETFRAMEWORK
        static readonly Encoding platformEncoding = Encoding.Default;
#else
        static readonly Encoding platformEncoding = CodePagesEncodingProvider.Instance.GetEncoding(0);
#endif

        internal static bool IsSupportedJNIVersion(int version)
        {
            return version == JNIEnv.JNI_VERSION_1_1
                || version == JNIEnv.JNI_VERSION_1_2
                || version == JNIEnv.JNI_VERSION_1_4
                || version == JNIEnv.JNI_VERSION_1_6
                || version == JNIEnv.JNI_VERSION_1_8
                ;
        }

        /// <summary>
        /// Decodes a pointer to the specified NULL-terminated string in platform format.
        /// </summary>
        /// <param name="psz"></param>
        /// <returns></returns>
        static string DecodePlatformString(byte* psz)
        {
            var p = psz is not null ? MemoryMarshalExtensions.GetIndexOfNull(psz) : -1;
            return p < 0 ? null : platformEncoding.GetString(psz, p);
        }

        /// <summary>
        /// Implements the JNI_CreateJavaVM native method.
        /// </summary>
        /// <param name="p_vm"></param>
        /// <param name="p_env"></param>
        /// <param name="vm_args"></param>
        /// <returns></returns>
        public static int CreateJavaVM(JavaVM** p_vm, void** p_env, void* vm_args)
        {
            var pInitArgs = (JavaVMInitArgs*)vm_args;

            // we don't support the JDK 1.1 JavaVMInitArgs
            if (!IsSupportedJNIVersion(pInitArgs->version) || pInitArgs->version == JNIEnv.JNI_VERSION_1_1)
                return JNIEnv.JNI_EVERSION;

            // JVM is already created, only one allowed at a time
            if (jvmCreated)
                return JNIEnv.JNI_ERR;

            var properties = new Dictionary<string, string>();
            for (int i = 0; i < pInitArgs->nOptions; i++)
            {
                var option = DecodePlatformString(pInitArgs->options[i].optionString);
                if (option == null)
                    return JNIEnv.JNI_ERR;

                if (option.StartsWith("-D"))
                {
                    var idx = option.IndexOf('=', 2);
                    properties[option.Substring(2, idx - 2)] = option.Substring(idx + 1);
                }
                else if (option.StartsWith("-verbose"))
                {
                    // ignore
                }
                else if (option == "vfprintf" || option == "exit" || option == "abort")
                {
                    // not supported
                }
                else if (pInitArgs->ignoreUnrecognized == JNIEnv.JNI_FALSE)
                {
                    return JNIEnv.JNI_ERR;
                }
            }

            // initialize the JVM properties
            foreach (var kvp in properties)
                JVM.Properties.User[kvp.Key] = kvp.Value;

            java.lang.Thread.currentThread();
            *p_vm = JavaVM.pJavaVM;

            return JavaVM.AttachCurrentThread(JavaVM.pJavaVM, p_env, null);
        }

        /// <summary>
        /// Implements the JNI_GetDefaultJavaVMInitArgs native function.
        /// </summary>
        /// <param name="vm_args"></param>
        /// <returns></returns>
        public static int GetDefaultJavaVMInitArgs(void* vm_args)
        {
            // This is only used for JDK 1.1 JavaVMInitArgs, and we don't support those.
            return JNIEnv.JNI_ERR;
        }

        /// <summary>
        /// Implements the JNI_GetCreatedJavaVMs native function.
        /// </summary>
        /// <param name="ppvmBuf"></param>
        /// <param name="bufLen"></param>
        /// <param name="nVMs"></param>
        /// <returns></returns>
        public static int GetCreatedJavaVMs(JavaVM** ppvmBuf, jsize bufLen, jsize* nVMs)
        {
            if (jvmCreated)
            {
                if (bufLen >= 1)
                    *ppvmBuf = JavaVM.pJavaVM;

                if (nVMs != null)
                    *nVMs = 1;
            }
            else if (nVMs != null)
                *nVMs = 0;

            return JNIEnv.JNI_OK;
        }

    }

#endif

}