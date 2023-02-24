using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class Build
{
    [MenuItem("Build/Development (Mono)")]
    public static void BuildPlayer_Android_Mono_Debug() => RunBuild(ScriptingImplementation.Mono2x, true);

    [MenuItem("Build/Release (il2cpp)")]
    public static void BuildPlayer_Android_il2cpp_Release() => RunBuild(ScriptingImplementation.IL2CPP, false);

    static void RunBuild(ScriptingImplementation backend, bool isDevelopment) => TaskExtensions.RunIgnoreAsync(async () =>
    {
        var backendIdentifier =
            backend == ScriptingImplementation.IL2CPP ? "il2cpp" :
            backend == ScriptingImplementation.Mono2x ? "mono" :
            throw new Exception("Unknown backend");

        var version = UpdateVersion();

        UpdateVersionResource(version);

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, backend);
        PlayerSettings.SetArchitecture(BuildTargetGroup.Android, (int)(backend == ScriptingImplementation.IL2CPP ? AndroidArchitecture.All : AndroidArchitecture.ARMv7));
        AssetDatabase.SaveAssets();

        UpdateFirebaseResources(isDevelopment);

        AssetDatabase.Refresh();

        Debug.Log("Resolving android dependencies");

        var tcs = new TaskCompletionSource<object>();
        GooglePlayServices.PlayServicesResolver.Resolve(() => tcs.TrySetResult(null), false, _ => tcs.TrySetResult(null));
        await tcs.Task;

        Debug.Log("Dependency resolution completed");

        var buildPath = "Builds/Android/" + PlayerSettings.applicationIdentifier + "-" + version.ToString() + "-" + backendIdentifier + ".apk";

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Startup.unity", "Assets/Scenes/Menu.unity" },
            locationPathName = buildPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = isDevelopment ? BuildOptions.Development : BuildOptions.None
        };

        var buildResult = BuildPipeline.BuildPlayer(opts);

        if (buildResult.summary.totalErrors == 0)
            EditorUtility.RevealInFinder(buildPath);
        else
            Debug.LogError("Build failed");
    });

    static void UpdateFirebaseResources(bool isDevelopment)
    {
        var identifier = isDevelopment ? "dev" : "prod";
        File.Copy($"FirebaseConfig/google-services.{identifier}.xml", "Assets/Plugins/Android/Firebase/res/values/google-services.xml", true);
        File.Copy($"FirebaseConfig/google-services-desktop.{identifier}.json", "Assets/StreamingAssets/google-services-desktop.json", true);
    }

    static void UpdateVersionResource(int version) => File.WriteAllText("Assets/Resources/ClientVersion.txt", version.ToString(), Encoding.ASCII);

    static int UpdateVersion()
    {
        var (version, major, minor) = ParseVersion(PlayerSettings.bundleVersion);

        var versionCode = PlayerSettings.Android.bundleVersionCode;
        var buildNumber = versionCode % 1000;

        var versionNameCode = version * 1_00_00 + major * 1_00 + minor;
        if (versionCode / 1000 != versionNameCode)
            buildNumber = 0;

        if (buildNumber == 999)
            throw new Exception("At build number 999, increase minor to enable build");

        var updatedVersionCode = versionNameCode * 1000 + (++buildNumber);
        Debug.Log("New version is " + updatedVersionCode);
        PlayerSettings.Android.bundleVersionCode = updatedVersionCode;

        return updatedVersionCode;
    }

    private static (int version, int major, int minor) ParseVersion(string bundleVersion)
    {
        var split = bundleVersion.Split('.');
        if (split.Length != 3)
            throw new Exception("Invalid version, should be of form num.num.num");

        return (int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]));
    }
}
