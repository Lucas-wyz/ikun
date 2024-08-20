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
using System.Runtime.InteropServices;

using IKVM.ByteCode.Text;
using IKVM.Runtime.Extensions;

namespace IKVM.Runtime.JNI
{

#if FIRST_PASS == false && IMPORTER == false && EXPORTER == false

    using jint = System.Int32;

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct JavaVM
    {

        static readonly MUTF8Encoding MUTF8 = MUTF8Encoding.GetMUTF8(52);

        internal static JavaVM* pJavaVM;
        void** vtable;
        void* firstVtableEntry;
        delegate int pf_int(JavaVM* pJVM);
        delegate int pf_int_ppvoid_pvoid(JavaVM* pJVM, void** p1, void* p2);
        delegate int pf_int_ppvoid_int(JavaVM* pJVM, void** p1, int p2);

        static Delegate[] vtableDelegates =
        {
            null,
            null,
            null,
            new pf_int(DestroyJavaVM),
            new pf_int_ppvoid_pvoid(AttachCurrentThread),
            new pf_int(DetachCurrentThread),
            new pf_int_ppvoid_int(GetEnv),
            new pf_int_ppvoid_pvoid(AttachCurrentThreadAsDaemon)
        };

        static JavaVM()
        {
            JNIVM.jvmCreated = true;
            pJavaVM = (JavaVM*)(void*)JNIMemory.Alloc(IntPtr.Size * (1 + vtableDelegates.Length));
            pJavaVM->vtable = &pJavaVM->firstVtableEntry;
            for (int i = 0; i < vtableDelegates.Length; i++)
            {
                if (null != vtableDelegates[i])
                {
                    pJavaVM->vtable[i] = (void*)Marshal.GetFunctionPointerForDelegate(vtableDelegates[i]);
                }
            }
        }

        /// <summary>
        /// Unloads a Java VM and reclaims its resources.
        /// </summary>
        /// <param name="pJVM"></param>
        /// <returns></returns>
        internal static jint DestroyJavaVM(JavaVM* pJVM)
        {
            if (JNIVM.jvmDestroyed)
                return JNIEnv.JNI_ERR;

            JNIVM.jvmDestroyed = true;
            IKVM.Java.Externs.java.lang.Thread.WaitUntilLastJniThread();
            return JNIEnv.JNI_OK;
        }

        /// <summary>
        /// Attaches the current thread to a Java VM. Returns a JNI interface pointer in the JNIEnv argument.
        /// </summary>
        /// <param name="pJVM"></param>
        /// <param name="penv"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static jint AttachCurrentThread(JavaVM* pJVM, void** penv, void* args)
        {
            return AttachCurrentThreadImpl(pJVM, penv, (JavaVMAttachArgs*)args, false);
        }

        /// <summary>
        /// Attaches the current thread to a Java VM. Returns a JNI interface pointer in the JNIEnv argument.
        /// </summary>
        /// <param name="pJVM"></param>
        /// <param name="penv"></param>
        /// <param name="pAttachArgs"></param>
        /// <param name="asDaemon"></param>
        /// <returns></returns>
        internal static jint AttachCurrentThreadImpl(JavaVM* pJVM, void** penv, JavaVMAttachArgs* pAttachArgs, bool asDaemon)
        {
            if (pAttachArgs != null)
            {
                if (!JNIVM.IsSupportedJNIVersion(pAttachArgs->version) || pAttachArgs->version == JNIEnv.JNI_VERSION_1_1)
                {
                    *penv = null;
                    return JNIEnv.JNI_EVERSION;
                }
            }

            var env = JNIEnvContext.Current;
            if (env != null)
            {
                *penv = env.pJNIEnv;
                return JNIEnv.JNI_OK;
            }

            // NOTE if we're here, it is *very* likely that the thread was created by native code and not by managed code,
            // but it's not impossible that the thread started life as a managed thread and if it did the changes to the
            // thread we're making are somewhat dubious.
            System.Threading.Thread.CurrentThread.IsBackground = asDaemon;
            if (pAttachArgs != null)
            {
                if (pAttachArgs->name != null && System.Threading.Thread.CurrentThread.Name == null)
                {
                    try
                    {
                        var l = MemoryMarshalExtensions.GetIndexOfNull(pAttachArgs->name);
                        if (l < 0)
                            return JNIEnv.JNI_ERR;

                        System.Threading.Thread.CurrentThread.Name = MUTF8.GetString(pAttachArgs->name, l);
                    }
                    catch (InvalidOperationException)
                    {
                        // someone beat us to it...
                    }
                }
                var threadGroup = JNIGlobalRefTable.Unwrap(pAttachArgs->group);
                if (threadGroup != null)
                    IKVM.Java.Externs.java.lang.Thread.AttachThreadFromJni(threadGroup);
            }

            *penv = JNIEnv.CreateJNIEnv(JVM.Context);
            return JNIEnv.JNI_OK;
        }

        /// <summary>
        /// Detaches the current thread from a Java VM. All Java monitors held by this thread are released. All Java threads waiting for this thread to die are notified.
        /// </summary>
        /// <param name="pJVM"></param>
        /// <returns></returns>
        internal static jint DetachCurrentThread(JavaVM* pJVM)
        {
            if (JNIEnvContext.Current == null)
            {
                // the JDK allows detaching from an already detached thread
                return JNIEnv.JNI_OK;
            }

            // TODO if we set Thread.IsBackground to false when we attached, now might be a good time to set it back to true.
            JNIEnv.FreeJNIEnv();
            java.lang.Thread.currentThread().die();
            return JNIEnv.JNI_OK;
        }

        internal static jint GetEnv(JavaVM* pJVM, void** penv, jint version)
        {
            if (JNIVM.IsSupportedJNIVersion(version))
            {
                var env = JNIEnvContext.Current;
                if (env != null)
                {
                    *penv = env.pJNIEnv;
                    return JNIEnv.JNI_OK;
                }

                *penv = null;
                return JNIEnv.JNI_EDETACHED;
            }

            *penv = null;
            return JNIEnv.JNI_EVERSION;
        }

        internal static jint AttachCurrentThreadAsDaemon(JavaVM* pJVM, void** penv, void* args)
        {
            return AttachCurrentThreadImpl(pJVM, penv, (JavaVMAttachArgs*)args, true);
        }

    }

#endif

}
