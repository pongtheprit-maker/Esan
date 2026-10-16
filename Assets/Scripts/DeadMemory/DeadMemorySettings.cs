using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(-900), DisallowMultipleComponent]
public sealed class DeadMemorySettings : MonoBehaviour
{
    const string Prefix = "DEAD_MEMORY.Options.";
    public static DeadMemorySettings Instance { get; private set; }
    public AudioMixer mixer;
    public AudioMixerGroup musicBus, sfxBus;
    public UniversalRenderPipelineAsset low, medium, high;
    public float Master { get; private set; }
    public float Music { get; private set; }
    public float Sfx { get; private set; }
    public int Graphics { get; private set; }
    bool dirty;
    float saveAt;

    void Awake()
    {
        Instance = this;
        Master = Read("Master", 1f); Music = Read("Music", 1f); Sfx = Read("Sfx", 1f);
        Graphics = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Graphics", 2), 0, 2);
    }
    void Start()
    {
        ApplyAudio(); ApplyGraphics();
        foreach (var source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Route(source, source.GetComponentInParent<SchoolBackgroundMusic>() != null);
    }
    static float Read(string key, float fallback)
    {
        float value = PlayerPrefs.GetFloat(Prefix + key, fallback);
        return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp01(value);
    }
    public static void Route(AudioSource source, bool music = false)
    {
        if (Instance == null || source == null) return;
        if (source.outputAudioMixerGroup == null || source.GetComponentInParent<SchoolBackgroundMusic>() != null)
            source.outputAudioMixerGroup = music ? Instance.musicBus : Instance.sfxBus;
    }
    public void SetMaster(float value) { Master = Mathf.Clamp01(value); ApplyAudio(); Changed(); }
    public void SetMusic(float value) { Music = Mathf.Clamp01(value); ApplyAudio(); Changed(); }
    public void SetSfx(float value) { Sfx = Mathf.Clamp01(value); ApplyAudio(); Changed(); }
    public void SetGraphics(int value) { Graphics = Mathf.Clamp(value, 0, 2); ApplyGraphics(); Changed(); }
    void ApplyAudio()
    {
        if (mixer == null) return;
        mixer.SetFloat("MasterVolume", Decibels(Master));
        mixer.SetFloat("MusicVolume", Decibels(Music));
        mixer.SetFloat("SFXVolume", Decibels(Sfx));
    }
    static float Decibels(float value) { return value <= 0.0001f ? -80f : 20f * Mathf.Log10(value); }
    void ApplyGraphics()
    {
        var asset = Graphics == 0 ? low : Graphics == 1 ? medium : high;
        if (asset != null) QualitySettings.renderPipeline = asset;
        QualitySettings.globalTextureMipmapLimit = Graphics == 0 ? 1 : 0;
        QualitySettings.lodBias = Graphics == 0 ? .7f : Graphics == 1 ? 1f : 1.5f;
    }
    void Changed() { dirty = true; saveAt = Time.realtimeSinceStartup + .5f; }
    void Update() { if (dirty && Time.realtimeSinceStartup >= saveAt) Save(); }
    public void Save()
    {
        if (!dirty) return;
        PlayerPrefs.SetFloat(Prefix + "Master", Master); PlayerPrefs.SetFloat(Prefix + "Music", Music);
        PlayerPrefs.SetFloat(Prefix + "Sfx", Sfx); PlayerPrefs.SetInt(Prefix + "Graphics", Graphics);
        PlayerPrefs.Save(); dirty = false;
    }
    void OnApplicationPause(bool paused) { if (paused) Save(); }
    void OnDisable() { Save(); }
    void OnDestroy() { if (Instance == this) Instance = null; }
}
