using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HearTheTide.EditorTools
{
    /// <summary>
    /// 一键初始化工程：安卓 / iOS 双平台设置、URP 管线、主场景。
    /// 菜单：与你听潮 → 初始化工程
    /// </summary>
    public static class ProjectSetup
    {
        public const string CompanyName = "YourStudio";          // TODO: 替换为公司名
        public const string ProductName = "与你听潮";
        public const string BundleId = "com.yourstudio.hearthetide"; // TODO: 替换为正式包名（上架后不可改）
        public const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string UrpFolder = "Assets/_Project/Settings";

        [MenuItem("与你听潮/1. 初始化工程（双平台 + URP + 主场景）", priority = 1)]
        public static void SetupAll()
        {
            ConfigurePlayer();
            ConfigureUrp();
            CreateMainScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[与你听潮] 工程初始化完成。按 Play 即可运行原型；打包见菜单 与你听潮 → 打包。");
        }

        [MenuItem("与你听潮/工具/仅设置双平台参数", priority = 20)]
        public static void ConfigurePlayer()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = "0.1.0";

            // 横屏
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Android
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = 1;

            // iOS
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.requiresFullScreen = true;

            // 两个平台共用
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.iOS, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Debug.Log("[与你听潮] 已设置 Android(IL2CPP/ARM64/API24+) 与 iOS(IL2CPP/13.0+)，包名 " + BundleId);
        }

        [MenuItem("与你听潮/工具/仅创建 URP 管线资源", priority = 21)]
        public static void ConfigureUrp()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
            {
                Debug.Log("[与你听潮] 已在使用 URP，跳过。");
                return;
            }
            Directory.CreateDirectory(UrpFolder);
            string rendererPath = UrpFolder + "/URP_Renderer.asset";
            string assetPath = UrpFolder + "/URP_Mobile.asset";

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (urp == null)
            {
                urp = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(urp, assetPath);
            }
            // 移动端起步参数：中端机 30fps / 高端机 60fps 再按性能测试调整
            urp.renderScale = 1f;
            urp.msaaSampleCount = 2;
            urp.shadowDistance = 30f;

            GraphicsSettings.defaultRenderPipeline = urp;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }
            EditorUtility.SetDirty(urp);
            Debug.Log("[与你听潮] 已创建并启用 URP：" + assetPath);
        }

        [MenuItem("与你听潮/工具/仅创建主场景", priority = 22)]
        public static void CreateMainScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                // 空场景即可：GameRoot 会在运行时自动创建（见 GameRoot.Bootstrap）
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
