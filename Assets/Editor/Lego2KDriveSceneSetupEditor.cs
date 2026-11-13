#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lego2KDrive.EditorTools
{
    [InitializeOnLoad]
    public static class Lego2KDriveSceneSetupEditor
    {
        private const string SCENE_FOLDER = "Assets/Scenes";
        private const string SCENE_PATH = "Assets/Scenes/Lego2KDriveMain.unity";

        static Lego2KDriveSceneSetupEditor()
        {
            EditorApplication.delayCall += EnsureMainSceneExists;
        }

        private static void EnsureMainSceneExists()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!File.Exists(SCENE_PATH))
            {
                CreateAndSaveMainScene();
            }
        }

        [MenuItem("LEGO 2K Drive/Setup & Open Main Game Scene", false, 10)]
        public static void CreateAndSaveMainScene()
        {
            if (!Directory.Exists(SCENE_FOLDER))
            {
                Directory.CreateDirectory(SCENE_FOLDER);
            }

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Directional Light
            GameObject lightObj = new GameObject("Directional Sun Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.97f, 0.90f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(48f, -38f, 0f);

            // 2. Main Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.74f, 0.98f);
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<LegoArcadeCamera>();
            camObj.transform.position = new Vector3(0f, 8f, -15f);

            // 3. LEGO 2K Drive Master Game Manager
            GameObject managerObj = new GameObject("Lego2KDrive_GameManager");
            managerObj.AddComponent<LegoRaceManagerAndHUD>();

            EditorSceneManager.SaveScene(newScene, SCENE_PATH);
            AssetDatabase.Refresh();

            // Register in Build Settings
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(SCENE_PATH, true)
            };

            Debug.Log("[LEGO 2K Drive] Main scene created and configured at " + SCENE_PATH + ". Press PLAY to drive!");
        }

        [MenuItem("LEGO 2K Drive/Play Game Now!", false, 20)]
        public static void PlayGameNow()
        {
            if (!File.Exists(SCENE_PATH))
            {
                CreateAndSaveMainScene();
            }
            else if (SceneManager.GetActiveScene().path != SCENE_PATH)
            {
                EditorSceneManager.OpenScene(SCENE_PATH);
            }

            EditorApplication.isPlaying = true;
        }
    }
}
#endif

