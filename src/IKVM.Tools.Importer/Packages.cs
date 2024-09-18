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

namespace IKVM.Tools.Importer
{
    sealed class Packages
    {

        readonly List<string> packages = new List<string>();
        readonly Dictionary<string, string> packagesSet = new Dictionary<string, string>();

        internal void DefinePackage(string packageName, string jar)
        {
            if (!packagesSet.ContainsKey(packageName))
            {
                packages.Add(packageName);
                packagesSet.Add(packageName, jar);
            }
        }

        // returns an array of PackageListAttribute constructor argument arrays
        internal object[][] ToArray()
        {
            List<object[]> list = new List<object[]>();
            // we use an empty string to indicate we don't yet have a jar,
            // because null is used for packages that were defined from
            // the file system (i.e. don't have a jar to load a manifest from)
            string currentJar = "";
            List<string> currentList = new List<string>();
            foreach (string package in packages)
            {
                string jar = packagesSet[package];
                if (jar != currentJar)
                {
                    if (currentList.Count != 0)
                    {
                        list.Add(new object[] { currentJar, currentList.ToArray() });
                        currentList.Clear();
                    }
                    currentJar = jar;
                }
                currentList.Add(package);
            }

            if (currentList.Count != 0)
            {
                list.Add(new object[] { currentJar, currentList.ToArray() });
            }

            return list.ToArray();
        }

    }

}
