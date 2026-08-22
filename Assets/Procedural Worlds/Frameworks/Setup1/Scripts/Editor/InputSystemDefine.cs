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
    /// Injects GAIA_INPUT_SYSTEM define into project after assets have been installed
    /// </summary>

    public class InputSystemDefineEditor : AssetPostprocessor
    {
        const string isDefine = "GAIA_INPUT_SYSTEM";
        static int maxRetries = 10;
        static int delayMilliseconds = 1000; // 1 second delay

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {

            bool updateScripting = false;
            string symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));

            if (InputSystemPackageCheck())
            {
                if (!symbols.Contains(isDefine))
                {
                    updateScripting = true;
                    symbols += ";" + isDefine;
                }
            }
            else
            {
                if (symbols.Contains(isDefine))
                {
                    updateScripting = true;
                    symbols = symbols.Replace(";" + isDefine, "");
                    symbols = symbols.Replace(isDefine, "");
                }
            }

            if (updateScripting)
            {
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), symbols);
            }
        }


        /// <summary>
        /// Checks if the Input System package is installed via reflection
        /// </summary>
        /// <returns></returns>
        public static bool InputSystemPackageCheck()
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
                        if (assembly.name.Contains("InputSystem"))
                        {
                            //was found -> we are done
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