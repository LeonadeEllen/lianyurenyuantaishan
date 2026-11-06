using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HearTheTide.EditorTools
{
    /// <summary>
    /// 打包脚本。编辑器菜单或命令行（CI）均可调用：
    ///   Unity -batchmode -quit -projectPath . -executeMethod HearTheTide.EditorTools.BuildScript.BuildAndroidApk -logFile -
    ///   Unity -batchmode -quit -projectPath . -executeMethod HearTheTide.EditorTools.BuildScript.BuildIos -logFile -
    /// iOS 会导出 Xcode 工程，需在 Mac 上用 Xcode 签名后安装或上传。
    /// </summary>
    public static class BuildScript
    {
        private const string OutputRoot = "Builds";

        [MenuItem("与你听潮/打包/Android APK（测试安装用）", priority = 40)]
        public static void BuildAndroidApk() { BuildAndroid(false); }

        [MenuItem("与你听潮/打包/Android AAB（上架 Google Play 用）", priority = 41)]
        public static void BuildAndroidAab() { BuildAndroid(true); }

        [MenuItem("与你听潮/打包/iOS（导出 Xcode 工程）", priority = 42)]
        public static void BuildIos()
        {
            ProjectSetup.ConfigurePlayer();
            Build(BuildTarget.iOS, Path.Combine(OutputRoot, "iOS"));
        }

        private static void BuildAndroid(bool appBundle)
        {
            ProjectSetup.ConfigurePlayer();
            EditorUserBuildSettings.buildAppBundle = appBundle;
            string file = "HearTheTide_" + PlayerSettings.bundleVersion + (appBundle ? ".aab" : ".apk");
            Build(BuildTarget.Android, Path.Combine(OutputRoot, "Android", file));
        }

        private static void Build(BuildTarget target, string location)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                ProjectSetup.CreateMainScene();
                scenes = new[] { ProjectSetup.ScenePath };
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(location)));

            bool dev = Environment.GetCommandLineArgs().Contains("-development");
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = location,
                target = target,
                options = dev ? BuildOptions.Development : BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[与你听潮] 打包成功：" + location + "  大小 " + (summary.totalSize / 1024 / 1024) + " MB");
            }
            else
            {
                Debug.LogError("[与你听潮] 打包失败：" + summary.result + "，错误数 " + summary.totalErrors);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
