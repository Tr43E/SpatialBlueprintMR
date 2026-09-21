using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpatialBlueprintMR.Editor
{
    /// <summary>Builds a self-contained Windows demo and packages it for sharing.</summary>
    public static class WindowsReleaseBuilder
    {
        private const string DemoScenePath = "Assets/2.unity";

        [MenuItem("Tools/Spatial Blueprint MR/Build Windows Release")]
        public static void BuildWindowsRelease()
        {
            EnsureDemoSceneExists();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(DemoScenePath, true) };
            PlayerSettings.productName = "SpatialBlueprintMR";

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var releaseName = $"SpatialBlueprintMR-Windows-{timestamp}";
            var buildDirectory = Path.Combine(projectRoot, "Builds", releaseName);
            var executablePath = Path.Combine(buildDirectory, "SpatialBlueprintMR.exe");
            Directory.CreateDirectory(buildDirectory);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { DemoScenePath },
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"Windows build failed. See the Unity Console for details. Output folder: {buildDirectory}");
                return;
            }

            File.WriteAllText(
                Path.Combine(buildDirectory, "README.txt"),
                "Spatial Blueprint MR\r\n\r\n"
                + "To run: unzip this folder if needed, then double-click SpatialBlueprintMR.exe.\r\n"
                + "Controls: W/A/S/D move, hold right mouse button and drag to look, B toggles bird's-eye view, Esc exits.\r\n"
                + "The included Studio, Apartment, and Office blueprints are available from the in-app import panel.\r\n");

            var zipPath = Path.Combine(projectRoot, "Builds", releaseName + ".zip");
            ZipFile.CreateFromDirectory(buildDirectory, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);
            Debug.Log($"Windows release created: {zipPath}");
            EditorUtility.RevealInFinder(zipPath);
        }

        private static void EnsureDemoSceneExists()
        {
            if (File.Exists(DemoScenePath))
            {
                return;
            }

            SpatialBlueprintMenu.CreateStarterScene();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), DemoScenePath);
        }
    }
}

