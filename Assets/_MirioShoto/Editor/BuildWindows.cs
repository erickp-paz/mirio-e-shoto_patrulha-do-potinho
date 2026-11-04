using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MirioShoto.EditorTools
{
    /// <summary>
    /// One-click Windows build + zip (menu: Tools > MirioShoto > Build Windows + Zip).
    /// Output: Builds/itch/MirioShoto-Windows.zip (ready to upload to itch.io).
    /// </summary>
    public static class BuildWindows
    {
        private const string OutputFolder = "Builds/itch/MirioShoto-Windows";
        private const string ZipPath = "Builds/itch/MirioShoto-Windows.zip";

        [MenuItem("Tools/MirioShoto/Build Windows + Zip")]
        public static void Build()
        {
            var scenes = EditorBuildSettings.scenes;
            var scenePaths = new System.Collections.Generic.List<string>();
            foreach (var s in scenes)
                if (s.enabled) scenePaths.Add(s.path);

            if (Directory.Exists(OutputFolder)) Directory.Delete(OutputFolder, true);
            Directory.CreateDirectory(OutputFolder);

            var options = new BuildPlayerOptions
            {
                scenes = scenePaths.ToArray(),
                locationPathName = Path.Combine(OutputFolder, "MirioShoto.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("Build failed: " + report.summary.result);
                return;
            }

            // Remove debug-only folders that should not ship.
            foreach (var dir in Directory.GetDirectories(OutputFolder, "*_BurstDebugInformation_DoNotShip"))
                Directory.Delete(dir, true);

            if (File.Exists(ZipPath)) File.Delete(ZipPath);
            ZipFile.CreateFromDirectory(OutputFolder, ZipPath, System.IO.Compression.CompressionLevel.Optimal, false);

            Debug.Log("Build OK. Zip ready at: " + Path.GetFullPath(ZipPath));
            EditorUtility.RevealInFinder(ZipPath);
        }
    }
}
