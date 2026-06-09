#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using UnityEngine;

namespace SackranyScenes.Editor
{
    public class SceneEnumGenerator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        const string OutputDir        = "Assets/_Generated/Scenes";
        const string ScenesPath       = OutputDir + "/GameScenes.cs";
        const string TransitionsPath  = OutputDir + "/GameTransitions.cs";
        const string AsmdefPath       = OutputDir + "/SackranyScenes.Generated.asmdef";

        const string LibraryAssetName = "SceneTransitionLibrary.asset";

        [MenuItem("Sackrany/Scenes/Generate Scene Names")]
        public static void Generate()
        {
            Directory.CreateDirectory(OutputDir);
            EnsureAsmdef();

            GenerateScenes();
            GenerateTransitions();

            AssetDatabase.Refresh();
        }

        [MenuItem("Sackrany/Scenes/Create Transition Library")]
        public static SceneTransitionLibrary CreateLibrary()
        {
            var existing = FindLibrary();
            if (existing != null)
            {
                Selection.activeObject = existing;
                return existing;
            }

            var resourcesDir = PackageRoot() + "/Resources";
            Directory.CreateDirectory(resourcesDir);

            var assetPath = resourcesDir + "/" + LibraryAssetName;
            var library = ScriptableObject.CreateInstance<SceneTransitionLibrary>();
            AssetDatabase.CreateAsset(library, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = library;
            return library;
        }

        static void GenerateScenes()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => Path.GetFileNameWithoutExtension(s.path));

            var sb = new StringBuilder();
            sb.AppendLine("// AUTO-GENERATED — do not edit manually");
            sb.AppendLine("// Scene names from Build Settings. Use with SackranyScenes.SceneLoader.Load(...).");
            sb.AppendLine("public static class GameScenes");
            sb.AppendLine("{");

            var seen = new HashSet<string>();
            foreach (var name in scenes)
            {
                var constName = ToConstName(name);
                if (!seen.Add(constName))
                {
                    Debug.LogWarning($"[Scenes] Skipping scene '{name}': constant name '{constName}' collides with another scene.");
                    continue;
                }

                sb.AppendLine($"    public const string {constName} = \"{name}\";");
            }

            sb.AppendLine("}");

            File.WriteAllText(ScenesPath, sb.ToString());
        }

        static void GenerateTransitions()
        {
            var library = FindLibrary();

            var sb = new StringBuilder();
            sb.AppendLine("// AUTO-GENERATED — do not edit manually");
            sb.AppendLine("// Transition names from the SceneTransitionLibrary. Pass to");
            sb.AppendLine("// SackranyScenes.SceneLoader.Load/Reload/SetTransition. null is always valid (= no transition).");
            sb.AppendLine("public static class GameTransitions");
            sb.AppendLine("{");

            if (library != null)
            {
                var seen = new HashSet<string>();
                foreach (var entry in library.Transitions)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.Name)) continue;

                    var constName = ToConstName(entry.Name);
                    if (!seen.Add(constName))
                    {
                        Debug.LogWarning($"[Scenes] Skipping transition '{entry.Name}': constant name '{constName}' collides with another transition.");
                        continue;
                    }

                    sb.AppendLine($"    public const string {constName} = \"{entry.Name}\";");
                }
            }

            sb.AppendLine("}");

            File.WriteAllText(TransitionsPath, sb.ToString());
        }

        // Builds a valid C# identifier: letters/digits kept (upper-cased), everything
        // else becomes '_', and a leading digit is prefixed so the result is legal.
        static string ToConstName(string name)
        {
            var sb = new StringBuilder(name.Length);
            foreach (var ch in name)
                sb.Append(char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : '_');

            var result = sb.ToString();
            if (result.Length == 0) result = "_";
            if (char.IsDigit(result[0])) result = "_" + result;

            return result;
        }

        static SceneTransitionLibrary FindLibrary()
        {
            var guids = AssetDatabase.FindAssets("t:SceneTransitionLibrary");
            if (guids.Length == 0) return null;

            if (guids.Length > 1)
                Debug.LogWarning("[Scenes] More than one SceneTransitionLibrary found; using the first. There should be exactly one.");

            return AssetDatabase.LoadAssetAtPath<SceneTransitionLibrary>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        // Folder that holds SceneLoader.cs — the package root.
        static string PackageRoot()
        {
            var guids = AssetDatabase.FindAssets("SceneLoader t:MonoScript");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path) == "SceneLoader.cs")
                    return Path.GetDirectoryName(path).Replace('\\', '/');
            }

            return "Assets";
        }

        static void EnsureAsmdef()
        {
            if (File.Exists(AsmdefPath)) return;

            const string json =
                @"{
    ""name"": ""SackranyScenes.Generated"",
    ""rootNamespace"": """",
    ""references"": [],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": true
}";
            File.WriteAllText(AsmdefPath, json);
        }

        public void OnPreprocessBuild(BuildReport report) => Generate();
    }
}
#endif
