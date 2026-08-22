using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralWorlds.Setup
{
    /// <summary>
    /// Injects GAIA_CINEMACHINE define into project after assets have been installed
    /// </summary>
    [InitializeOnLoad]
    public class CinemachineDefineEditor : AssetPostprocessor
    {
        static Dictionary<string, string> vmVersionDefines = new Dictionary<string, string>()
        {
            //putting the version of the cinemachine package first as it can cause troubles 
            //when one scripting define string is contained within another
            { "old", "GAIA_CINEMACHINE" },
            {"v3", "GAIA_V3_CINEMACHINE_OR_NEWER" }
        }
        ;
        static string currentVersion = "old";
        static int maxRetries = 10;
        static int delayMilliseconds = 1000; // 1 second delay

        static CinemachineDefineEditor()
        {

            bool updateScripting = false;
            string symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));

            if (AddressablesPackageCheck())
            {
                //We do have Cinemachine installed, add the matching define and remove all potential outdated defines from the dictionary
                foreach (KeyValuePair<string, string> kvp in vmVersionDefines)
                {
                    if (kvp.Key == currentVersion)
                    {
                        if (!symbols.Contains(kvp.Value))
                        {
                            updateScripting = true;
                            symbols += ";" + kvp.Value;
                        }
                    }
                    else
                    {
                        if (symbols.Contains(kvp.Value))
                        {
                            updateScripting = true;
                            symbols = symbols.Replace(";" + kvp.Value, "");
                            symbols = symbols.Replace(kvp.Value, "");
                        }
                    }
                }
            }
            else
            {
                //We do not have Cinemachine installed at all, remove all potential defines from the dictionary
                foreach (KeyValuePair<string, string> kvp in vmVersionDefines)
                {
                    if (symbols.Contains(kvp.Value))
                    {
                        updateScripting = true;
                        symbols = symbols.Replace(";" + kvp.Value, "");
                        symbols = symbols.Replace(kvp.Value, "");
                    }
                }
            }

            if (updateScripting)
            {
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), symbols);
            }
        }


        /// <summary>
        /// Checks if the addressables package is installed via reflection
        /// </summary>
        /// <returns></returns>
        public static bool AddressablesPackageCheck()
        {
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    //Look for assembly
                    var assemblies = CompilationPipeline.GetAssemblies();
                    foreach (UnityEditor.Compilation.Assembly assembly in assemblies)
                    {
                        if (assembly.name.Contains("Cinemachine"))
                        {
                            //was found, now we need to look for specific class name to find out which version we are running
                            if (assembly.sourceFiles.Any(t => t.Contains("CinemachineSplineDolly")))
                            {
                                //let's adjust the scripting define to the version then
                                currentVersion = "v3";
                                return true;
                            }
                            return true;
                        }
                    }
                    return false;
                }
                catch (IOException ex) when (IsFileLocked(ex))
                {
                    attempt++;
                    Thread.Sleep(delayMilliseconds);
                }
            }
            return false;
        }

        static bool IsFileLocked(IOException ex)
        {
            int errorCode = Marshal.GetHRForException(ex) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33;
        }

    }
}