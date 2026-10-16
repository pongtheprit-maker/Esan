using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.Universal;

public static class DeadMemoryTitleBuilder
{
    const string Folder = "Assets/DeadMemory/TitleUI/";
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    [MenuItem("DEAD MEMORY/Install Title and Options")]
    public static void Install()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Install the title in Edit Mode.");
        string backgroundPath = Folder + "TitleBackground_Investigation.png";
        var background = AssetDatabase.LoadAssetAtPath<Texture2D>(backgroundPath);
        if (background == null) { backgroundPath = Folder + "TitleBackground.png"; background = AssetDatabase.LoadAssetAtPath<Texture2D>(backgroundPath); }
        var shader = Shader.Find("DeadMemory/TitleSignal");
        var regular = AssetDatabase.LoadAssetAtPath<Font>("Packages/com.unity.dt.app-ui/PackageResources/Fonts/Inter-Regular.ttf");
        var bold = AssetDatabase.LoadAssetAtPath<Font>("Packages/com.unity.dt.app-ui/PackageResources/Fonts/Inter-SemiBold.ttf");
        if (background == null || shader == null || regular == null || bold == null)
            throw new InvalidOperationException("Title background, shader or fonts are missing.");
        var importer = (TextureImporter)AssetImporter.GetAtPath(backgroundPath);
        importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true; importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp; importer.maxTextureSize = 4096; importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();

        var go = GameObject.Find("DeadMemory_TitleUI");
        if (go == null) { go = new GameObject("DeadMemory_TitleUI"); Undo.RegisterCreatedObjectUndo(go, "Install title UI"); }
        var settings = go.GetComponent<DeadMemorySettings>(); if (settings == null) settings = Undo.AddComponent<DeadMemorySettings>(go);
        var menu = go.GetComponent<DeadMemoryTitleMenu>(); if (menu == null) menu = Undo.AddComponent<DeadMemoryTitleMenu>(go);
        settings.mixer = CreateMixer();
        var groups = settings.mixer.FindMatchingGroups("");
        settings.musicBus = groups.First(g => g.name == "Music"); settings.sfxBus = groups.First(g => g.name == "SFX");
        var source = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (source == null) throw new InvalidOperationException("School URP asset missing.");
        settings.low = Pipeline(source, "Low", .70f, 18, 1024, 512, false, false, 4, 1);
        settings.medium = Pipeline(source, "Medium", .85f, 28, 2048, 1024, true, true, 6, 2);
        settings.high = Pipeline(source, "High", 1f, 45, 2048, 2048, true, true, 8, 2);
        menu.background = background; menu.regularFont = regular; menu.titleFont = bold; menu.signalShader = shader; menu.settings = settings;
        foreach (var audio in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (audio.outputAudioMixerGroup != null) continue;
            audio.outputAudioMixerGroup = audio.GetComponentInParent<SchoolBackgroundMusic>() != null ? settings.musicBus : settings.sfxBus;
            EditorUtility.SetDirty(audio);
        }
        EditorUtility.SetDirty(settings); EditorUtility.SetDirty(menu);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(go.scene); EditorSceneManager.SaveScene(go.scene);
        Selection.activeGameObject = go;
        Debug.Log("DEAD MEMORY title installed. New Game, Continue, Load Game, Options and Exit are connected.");
    }
    static UniversalRenderPipelineAsset Pipeline(UniversalRenderPipelineAsset source, string label, float scale, float distance, int main, int additional, bool shadows, bool soft, int lights, int msaa)
    {
        string path = Folder + "School_" + label + ".asset";
        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if (asset == null) { asset = UnityEngine.Object.Instantiate(source); asset.name = "School " + label; AssetDatabase.CreateAsset(asset, path); }
        asset.renderScale = scale; asset.shadowDistance = distance; asset.mainLightShadowmapResolution = main;
        asset.additionalLightsShadowmapResolution = additional;
        var serialized = new SerializedObject(asset);
        serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue = shadows;
        serialized.FindProperty("m_SoftShadowsSupported").boolValue = soft;
        serialized.ApplyModifiedProperties();
        asset.maxAdditionalLightsCount = lights; asset.msaaSampleCount = msaa;
        asset.supportsCameraDepthTexture = true; asset.supportsHDR = true;
        EditorUtility.SetDirty(asset); return asset;
    }
    static AudioMixer CreateMixer()
    {
        string path = Folder + "DeadMemoryAudio.mixer";
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
        Type type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.Audio.AudioMixerController")).First(t => t != null);
        if (mixer == null) mixer = (AudioMixer)type.GetMethod("CreateMixerControllerAtPath", Flags).Invoke(null, new object[] { path });
        var master = (UnityEngine.Object)type.GetProperty("masterGroup", Flags).GetValue(mixer, null);
        var groups = ((System.Collections.IEnumerable)type.GetMethod("GetAllAudioGroupsSlow", Flags).Invoke(mixer, null)).Cast<UnityEngine.Object>().ToList();
        foreach (string name in new[] { "Music", "SFX" })
            if (!groups.Any(g => g.name == name)) groups.Add((UnityEngine.Object)type.GetMethod("CreateNewGroup", Flags).Invoke(mixer, new object[] { name, true }));
        var children = Array.CreateInstance(master.GetType(), 2);
        children.SetValue(groups.First(g => g.name == "Music"), 0); children.SetValue(groups.First(g => g.name == "SFX"), 1);
        master.GetType().GetProperty("children", Flags).SetValue(master, children, null);
        var serialized = new SerializedObject(mixer); var parameters = serialized.FindProperty("m_ExposedParameters"); parameters.arraySize = 3;
        var labels = new[] { "Master", "Music", "SFX" };
        for (int i = 0; i < labels.Length; i++)
        {
            var group = new SerializedObject(groups.First(g => g.name == labels[i]));
            var element = parameters.GetArrayElementAtIndex(i);
            for (int j = 0; j < 4; j++) element.FindPropertyRelative("guid.data[" + j + "]").longValue = group.FindProperty("m_Volume.data[" + j + "]").longValue;
            element.FindPropertyRelative("name").stringValue = labels[i] + "Volume";
        }
        serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(mixer); return mixer;
    }
}
