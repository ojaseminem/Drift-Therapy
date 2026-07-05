#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DriftTherapy.EditorTools
{
    public sealed class AndroidReleaseBuilderWindow : EditorWindow
    {
        const string PackageIdKey = "DriftTherapy.Android.PackageId";
        const string KeystorePathKey = "DriftTherapy.Android.KeystorePath";
        const string KeyAliasKey = "DriftTherapy.Android.KeyAlias";
        const string DefaultPackageId = "com.turtlegameworks.drifttherapy";
        const string DefaultAlias = "drift-therapy-upload";
        const string DefaultKeystoreRelativePath = "BuildSecrets/Android/drift-therapy-upload.keystore";

        string packageId;
        string keystorePath;
        string keyAlias;
        string keystorePass = string.Empty;
        string keyPass = string.Empty;
        bool useSamePassword = true;

        [MenuItem("Tools/Drift Therapy/Android/Open Release Builder")]
        public static void Open()
        {
            GetWindow<AndroidReleaseBuilderWindow>("Android Release");
        }

        void OnEnable()
        {
            packageId = EditorPrefs.GetString(PackageIdKey, GetCurrentPackageIdOrDefault());
            keystorePath = EditorPrefs.GetString(KeystorePathKey, Path.GetFullPath(DefaultKeystoreRelativePath));
            keyAlias = EditorPrefs.GetString(KeyAliasKey, DefaultAlias);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Drift Therapy Android Release", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use this for Play Console internal testing builds. It creates/uses a local upload keystore, applies Android Player Settings, and builds a signed .aab. Passwords are only used in this editor session unless you also provide them through environment variables.",
                MessageType.Info);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                packageId = EditorGUILayout.TextField("Package ID", packageId);
                keystorePath = EditorGUILayout.TextField("Keystore Path", keystorePath);
                keyAlias = EditorGUILayout.TextField("Key Alias", keyAlias);
                keystorePass = EditorGUILayout.PasswordField("Keystore Password", keystorePass);
                useSamePassword = EditorGUILayout.Toggle("Use Same Key Password", useSamePassword);
                using (new EditorGUI.DisabledScope(useSamePassword))
                {
                    keyPass = EditorGUILayout.PasswordField("Key Password", useSamePassword ? keystorePass : keyPass);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use Default Drift Therapy Values"))
                {
                    packageId = DefaultPackageId;
                    keystorePath = Path.GetFullPath(DefaultKeystoreRelativePath);
                    keyAlias = DefaultAlias;
                }

                if (GUILayout.Button("Browse Keystore"))
                {
                    string selected = EditorUtility.OpenFilePanel("Select Android upload keystore", Directory.GetCurrentDirectory(), "keystore,jks");
                    if (!string.IsNullOrWhiteSpace(selected))
                        keystorePath = selected;
                }
            }

            if (GUILayout.Button("Apply Android Player Settings"))
            {
                SaveNonSecretPrefs();
                AndroidReleaseBuilder.ApplyAndroidPlayerSettings(packageId);
            }

            if (GUILayout.Button("Create Drift Therapy Upload Keystore"))
            {
                SaveNonSecretPrefs();
                AndroidReleaseBuilder.CreateUploadKeystore(keystorePath, keyAlias, keystorePass, useSamePassword ? keystorePass : keyPass);
            }

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Build Signed Android App Bundle (.aab)"))
            {
                SaveNonSecretPrefs();
                AndroidReleaseBuilder.BuildAAB(packageId, keystorePath, keystorePass, keyAlias, useSamePassword ? keystorePass : keyPass);
            }
        }

        void SaveNonSecretPrefs()
        {
            EditorPrefs.SetString(PackageIdKey, packageId ?? string.Empty);
            EditorPrefs.SetString(KeystorePathKey, keystorePath ?? string.Empty);
            EditorPrefs.SetString(KeyAliasKey, keyAlias ?? string.Empty);
        }

        static string GetCurrentPackageIdOrDefault()
        {
            string current = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            return string.IsNullOrWhiteSpace(current) || current.StartsWith("com.DefaultCompany", StringComparison.Ordinal)
                ? DefaultPackageId
                : current;
        }
    }

    public static class AndroidReleaseBuilder
    {
        const string OutputPath = "Builds/Android";
        const string DefaultPackageId = "com.turtlegameworks.drifttherapy";
        const string EnvFileRelativePath = "UserSettings/drift-therapy-android-keystore.env";

        [MenuItem("Tools/Drift Therapy/Android/Apply Android Player Settings")]
        public static void ApplyDefaultAndroidPlayerSettings()
        {
            string current = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            string packageId = string.IsNullOrWhiteSpace(current) || current.StartsWith("com.DefaultCompany", StringComparison.Ordinal)
                ? DefaultPackageId
                : current;
            ApplyAndroidPlayerSettings(packageId);
        }

        [MenuItem("Tools/Drift Therapy/Android/Build AAB Release")]
        public static void BuildAABMenu()
        {
            var signing = LoadSigningFromEnvironment();
            string packageId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            BuildAAB(packageId, signing.keystorePath, signing.keystorePass, signing.keyAlias, signing.keyPass);
        }

        public static void ApplyAndroidPlayerSettings(string packageId)
        {
            ValidatePackageId(packageId);

            PlayerSettings.productName = "Drift Therapy";
            PlayerSettings.companyName = "TurtleGameWorks";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, packageId.Trim());
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            AssetDatabase.SaveAssets();
            Debug.Log($"[AndroidReleaseBuilder] Android Player Settings applied for {packageId}.");
        }

        public static void CreateUploadKeystore(string keystorePath, string alias, string keystorePass, string keyPass)
        {
            ValidateSigningInputs(keystorePath, keystorePass, alias, keyPass, requireExistingKeystore: false);

            string fullPath = Path.GetFullPath(keystorePath);
            if (File.Exists(fullPath))
                throw new BuildFailedException($"Keystore already exists at: {fullPath}");

            string directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new BuildFailedException("Keystore path must include a directory.");

            Directory.CreateDirectory(directory);

            string keytool = FindKeytool();
            string args =
                "-genkeypair -v" +
                $" -keystore {Quote(fullPath)}" +
                $" -storepass {Quote(keystorePass)}" +
                $" -alias {Quote(alias)}" +
                $" -keypass {Quote(keyPass)}" +
                " -keyalg RSA -keysize 2048 -validity 10000" +
                $" -dname {Quote("CN=Drift Therapy, OU=Drift Therapy, O=TurtleGameWorks, L=Unknown, ST=Unknown, C=US")}";

            RunProcess(keytool, args, "keystore creation");
            Debug.Log($"[AndroidReleaseBuilder] Created Drift Therapy upload keystore at: {fullPath}");
        }

        public static void BuildAAB(string packageId, string keystorePath, string keystorePass, string alias, string keyPass)
        {
            ApplyAndroidPlayerSettings(packageId);
            ApplyKeystoreSettings(keystorePath, keystorePass, alias, keyPass);

            string[] scenes = GetEnabledScenes();
            if (scenes.Length == 0)
                throw new BuildFailedException("No enabled scenes found in Build Settings.");

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Failed to switch active build target to Android.");

            string outputDir = Path.GetFullPath(Path.Combine(OutputPath, $"{PlayerSettings.bundleVersion}_{PlayerSettings.Android.bundleVersionCode}"));
            Directory.CreateDirectory(outputDir);

            string fileName = $"DriftTherapy_{PlayerSettings.bundleVersion}_{PlayerSettings.Android.bundleVersionCode}.aab";
            string outputFile = Path.Combine(outputDir, fileName);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputFile,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Android AAB build failed: {report.summary.result}");

            Debug.Log($"[AndroidReleaseBuilder] Signed AAB written to: {outputFile}");
        }

        static void ApplyKeystoreSettings(string keystorePath, string keystorePass, string alias, string keyPass)
        {
            ValidateSigningInputs(keystorePath, keystorePass, alias, keyPass, requireExistingKeystore: true);

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(keystorePath);
            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = keyPass;
        }

        static (string keystorePath, string keystorePass, string keyAlias, string keyPass) LoadSigningFromEnvironment()
        {
            string keystorePath = Env("DRIFT_THERAPY_KEYSTORE_PATH");
            string keystorePass = Env("DRIFT_THERAPY_KEYSTORE_PASS");
            string keyAlias = Env("DRIFT_THERAPY_KEY_ALIAS");
            string keyPass = Env("DRIFT_THERAPY_KEY_PASS");

            string envFile = Path.GetFullPath(EnvFileRelativePath);
            if (File.Exists(envFile))
            {
                foreach (string line in File.ReadAllLines(envFile))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#", StringComparison.Ordinal))
                        continue;

                    int index = line.IndexOf('=');
                    if (index <= 0)
                        continue;

                    string key = line.Substring(0, index).Trim();
                    string value = line.Substring(index + 1).Trim().Trim('"');

                    switch (key)
                    {
                        case "DRIFT_THERAPY_KEYSTORE_PATH": keystorePath = value; break;
                        case "DRIFT_THERAPY_KEYSTORE_PASS": keystorePass = value; break;
                        case "DRIFT_THERAPY_KEY_ALIAS": keyAlias = value; break;
                        case "DRIFT_THERAPY_KEY_PASS": keyPass = value; break;
                    }
                }
            }

            return (keystorePath, keystorePass, keyAlias, keyPass);
        }

        static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
        }

        static void ValidatePackageId(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
                throw new BuildFailedException("Package ID is required.");

            string trimmed = packageId.Trim();
            if (trimmed.StartsWith("com.DefaultCompany", StringComparison.Ordinal))
                throw new BuildFailedException("Package ID still uses the Unity default company namespace. Choose the final Play Store package ID before uploading.");

            string[] parts = trimmed.Split('.');
            if (parts.Length < 3 || parts.Any(part => string.IsNullOrWhiteSpace(part) || !IsJavaIdentifier(part)))
                throw new BuildFailedException($"Invalid Android package ID: {packageId}");
        }

        static void ValidateSigningInputs(string keystorePath, string keystorePass, string alias, string keyPass, bool requireExistingKeystore)
        {
            if (string.IsNullOrWhiteSpace(keystorePath))
                throw new BuildFailedException("Keystore path is required.");
            if (string.IsNullOrEmpty(keystorePass))
                throw new BuildFailedException("Keystore password is required.");
            if (string.IsNullOrWhiteSpace(alias))
                throw new BuildFailedException("Key alias is required.");
            if (string.IsNullOrEmpty(keyPass))
                throw new BuildFailedException("Key password is required.");

            string fullPath = Path.GetFullPath(keystorePath);
            if (requireExistingKeystore && !File.Exists(fullPath))
                throw new BuildFailedException($"Keystore not found at: {fullPath}");
        }

        static bool IsJavaIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            if (!char.IsLetter(value[0]) && value[0] != '_')
                return false;
            for (int i = 1; i < value.Length; i++)
            {
                char c = value[i];
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }
            return true;
        }

        static string FindKeytool()
        {
            string unityKeytool = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "OpenJDK", "bin", "keytool.exe");
            if (File.Exists(unityKeytool))
                return unityKeytool;

            string javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrWhiteSpace(javaHome))
            {
                string javaHomeKeytool = Path.Combine(javaHome, "bin", "keytool.exe");
                if (File.Exists(javaHomeKeytool))
                    return javaHomeKeytool;
            }

            return "keytool";
        }

        static void RunProcess(string fileName, string arguments, string description)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                    throw new BuildFailedException($"Failed to start {description} process.");

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    throw new BuildFailedException($"{description} failed with exit code {process.ExitCode}: {error}");

                if (!string.IsNullOrWhiteSpace(output))
                    Debug.Log($"[AndroidReleaseBuilder] {description} completed.");
            }
        }

        static string Quote(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        static string Env(string key)
        {
            return Environment.GetEnvironmentVariable(key) ?? string.Empty;
        }
    }
}
#endif
