// Keeps the Unity-MCP editor tooling (Assets/Plugins/NuGet DLLs + com.IvanMurzak.Unity.MCP.Runtime)
// out of player builds without touching ProjectSettings scripting defines.
//
// How it works:
//  * Every DLL under Assets/Plugins/NuGet gets the plugin define constraint
//    "!UNITY_MCP_EXCLUDE_FROM_COMPILATION". Platform flags (Any/Editor/...) are left alone on purpose:
//    the MCP DependencyResolver rewrites them on every domain reload, so changing them here would make
//    the two fight in an endless reimport loop. The resolver never touches define constraints.
//  * Builds started from the Build Settings window (Build / Build And Run) get
//    UNITY_MCP_EXCLUDE_FROM_COMPILATION in BuildPlayerOptions.extraScriptingDefines. That define removes
//    the NuGet DLLs (via the constraint above) and the MCP Runtime asmdef (its own constraint) from the build.
//  * In the Editor the define is never set, so the MCP plugin keeps working.
//
// NOTE: builds started by other code paths (custom build scripts, CI calling BuildPipeline.BuildPlayer
// directly) do not go through this handler; pass the same define in their extraScriptingDefines.

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PixelMatch.EditorTools
{
    [InitializeOnLoad]
    public static class McpBuildExclusion
    {
        public const string ExcludeDefine = "UNITY_MCP_EXCLUDE_FROM_COMPILATION";
        public const string PluginConstraint = "!" + ExcludeDefine;
        public const string NuGetFolder = "Assets/Plugins/NuGet/";
        const string Tag = "[McpBuildExclusion]";
        const string SessionKey = "PixelMatch.McpBuildExclusion.Checked";

        static McpBuildExclusion()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);

            // One-time (per editor session) self-heal: reimport any NuGet DLL that is still missing the
            // constraint, so the AssetPostprocessor below applies it. Deferred out of the static ctor.
            if (!SessionState.GetBool(SessionKey, false))
            {
                SessionState.SetBool(SessionKey, true);
                EditorApplication.delayCall += () => ReapplyToExistingDlls(false);
            }
        }

        // ------------------------------------------------------------------ build
        static void OnBuildPlayer(BuildPlayerOptions options)
        {
            var defines = options.extraScriptingDefines ?? new string[0];
            if (!defines.Contains(ExcludeDefine))
                options.extraScriptingDefines = defines.Concat(new[] { ExcludeDefine }).ToArray();

            Debug.Log($"{Tag} Building {options.target} with extra defines: {string.Join(", ", options.extraScriptingDefines)}");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;

            string size = s.totalSize > 0 ? $"{s.totalSize / (1024f * 1024f):0.0} MB" : "n/a";
            string msg = $"{Tag} Build {s.result} — target {s.platform}, size {size}, " +
                         $"time {s.totalTime:hh\\:mm\\:ss}, errors {s.totalErrors}, warnings {s.totalWarnings}, output: {s.outputPath}";

            if (s.result == BuildResult.Succeeded) Debug.Log(msg);
            else Debug.LogError(msg);
        }

        // ------------------------------------------------------------------ existing DLLs
        [MenuItem("Tools/Pixel Match/MCP: Reapply Build Exclusion To NuGet DLLs")]
        static void ReapplyFromMenu() => ReapplyToExistingDlls(true);

        /// <summary>Reimports only the NuGet DLLs whose importer is still missing the constraint.</summary>
        public static int ReapplyToExistingDlls(bool verbose)
        {
            if (!Directory.Exists(NuGetFolder)) return 0;

            var pending = Directory.GetFiles(NuGetFolder, "*.dll", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/'))
                .Where(p => AssetImporter.GetAtPath(p) is PluginImporter imp && !HasConstraint(imp))
                .ToList();

            if (pending.Count > 0)
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var p in pending)
                        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }

            if (verbose || pending.Count > 0)
                Debug.Log($"{Tag} Reimported {pending.Count} NuGet DLL(s) to apply '{PluginConstraint}'.");
            return pending.Count;
        }

        public static bool HasConstraint(PluginImporter importer) =>
            importer.DefineConstraints != null && importer.DefineConstraints.Contains(PluginConstraint);

        public static bool IsNuGetDll(string assetPath) =>
            assetPath.StartsWith(NuGetFolder, System.StringComparison.OrdinalIgnoreCase) &&
            assetPath.EndsWith(".dll", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Adds the build-exclusion define constraint to NuGet DLLs as they import. Writes only when the
    /// constraint is missing, so it never causes extra reimports; platform flags are not touched.
    /// </summary>
    public class McpNuGetDllPostprocessor : AssetPostprocessor
    {
        void OnPreprocessAsset()
        {
            if (!McpBuildExclusion.IsNuGetDll(assetPath)) return;
            if (!(assetImporter is PluginImporter importer)) return;
            if (McpBuildExclusion.HasConstraint(importer)) return;

            var current = importer.DefineConstraints ?? new string[0];
            importer.DefineConstraints = current.Concat(new[] { McpBuildExclusion.PluginConstraint }).ToArray();
        }
    }
}
