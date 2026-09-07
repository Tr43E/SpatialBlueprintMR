using System.IO;
using SpatialBlueprintMR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpatialBlueprintMR.Editor
{
    public static class SpatialBlueprintMenu
    {
        private const string SamplePlanPath = "Assets/StreamingAssets/SampleStudio.dxf";

        [MenuItem("Tools/Spatial Blueprint MR/Create Unity 6 Starter Scene")]
        public static void CreateStarterScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SpatialBlueprintDemo";

            var directionalLight = new GameObject("Directional Light");
            var light = directionalLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            directionalLight.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

            var camera = new GameObject("Preview Camera");
            camera.tag = "MainCamera";
            var cameraComponent = camera.AddComponent<Camera>();
            cameraComponent.clearFlags = CameraClearFlags.Skybox;
            camera.transform.position = new Vector3(2.5f, 2.1f, -3.5f);
            camera.transform.LookAt(new Vector3(1.5f, 1f, 1.5f));

            var blueprintRoot = new GameObject("BlueprintRoot");
            blueprintRoot.AddComponent<BlueprintModel>();
            var importer = blueprintRoot.AddComponent<DxfBlueprintImporter>();
            blueprintRoot.AddComponent<PlanPlacementController>();
            blueprintRoot.AddComponent<BlueprintDemoBootstrapper>();

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Reference Floor (remove for headset build)";
            floor.transform.localScale = new Vector3(2f, 1f, 2f);
            var floorRenderer = floor.GetComponent<MeshRenderer>();
            floorRenderer.material.color = new Color(0.2f, 0.22f, 0.24f, 1f);

            Selection.activeGameObject = blueprintRoot;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Starter scene created. Save it, then press Play to preview the imported plan.");
        }

        [MenuItem("Tools/Spatial Blueprint MR/Copy DXF into Project…")]
        public static void CopyDxfIntoProject()
        {
            var source = EditorUtility.OpenFilePanel("Choose an ASCII DXF floor plan", string.Empty, "dxf");
            if (string.IsNullOrWhiteSpace(source))
            {
                return;
            }

            var destinationFolder = Path.Combine(Application.dataPath, "StreamingAssets", "Imported");
            Directory.CreateDirectory(destinationFolder);
            var destination = Path.Combine(destinationFolder, Path.GetFileName(source));
            File.Copy(source, destination, true);
            AssetDatabase.Refresh();
            Debug.Log($"Copied DXF to {destination}. Assign it to Default Plan on BlueprintRoot, then press Play.");
        }
    }
}
