#if UNITY_EDITOR
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

        const string OutputDir   = "Assets/_Generated/Scenes";
        const string OutputPath  = OutputDir + "/GameScenes.cs";
        const string AsmdefPath  = OutputDir + "/SackranyScenes.Generated.asmdef";

        [MenuItem("Sackrany/Scenes/Generate Scene Names")]
        public static void Generate()
        {
            Directory.CreateDirectory(OutputDir);
            EnsureAsmdef();

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => Path.GetFileNameWithoutExtension(s.path))
                .Distinct();

            var sb = new StringBuilder();
            sb.AppendLine("// AUTO-GENERATED — не редактировать вручную");
            sb.AppendLine("// Имена сцен из Build Settings. Используй вместе с SackranyScenes.SceneLoader.Load(...).");
            sb.AppendLine("public static class GameScenes");
            sb.AppendLine("{");

            foreach (var name in scenes)
            {
                var constName = ToConstName(name);
                sb.AppendLine($"    public const string {constName} = \"{name}\";");
            }

            sb.AppendLine("}");

            File.WriteAllText(OutputPath, sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log($"[Scenes] Generated GameScenes.cs");
        }

        static string ToConstName(string name)
            => name.ToUpper().Replace(" ", "_").Replace("-", "_");

        /// <summary>
        /// Сгенерированный код живёт в отдельной сборке, чтобы не ломать asmdef
        /// руками написанных фич. Класс констант ни от чего не зависит.
        /// </summary>
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
