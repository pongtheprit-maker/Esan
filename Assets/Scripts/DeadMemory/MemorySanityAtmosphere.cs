using System.Collections.Generic;
using UnityEngine;

/// <summary>Perception only. Reads sanity; never creates scares, threats or sanity damage.</summary>
[DefaultExecutionOrder(200)]
public class MemorySanityAtmosphere : MonoBehaviour
{
    public SanitySystem sanity;
    [Range(0, .4f)] public float maxDroneVolume = .16f;
    [Range(0, .4f)] public float maxDistantVolume = .12f;
    public float Redness { get; private set; }
    public float AmbientAudioLevel { get { return drone == null ? 0 : drone.volume; } }
    readonly List<Light> lights = new List<Light>();
    readonly List<Color> colors = new List<Color>();
    readonly List<float> intensities = new List<float>();
    AudioSource drone, distant;
    AudioClip droneClip, distantClip;
    Color ambient, fog;
    float fogDensity;
    bool initialized;
    float phase;

    void Start()
    {
        if (sanity == null) sanity = FindFirstObjectByType<SanitySystem>();
        ambient = RenderSettings.ambientLight; fog = RenderSettings.fogColor; fogDensity = RenderSettings.fogDensity;
        foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l.GetComponentInParent<FlashlightController>() != null || l.type == LightType.Directional) continue;
            lights.Add(l); colors.Add(l.color); intensities.Add(l.intensity);
        }
        drone = gameObject.AddComponent<AudioSource>();
        drone.playOnAwake = false; drone.loop = true; drone.spatialBlend = 0; drone.volume = 0;
        DeadMemorySettings.Route(drone);
        droneClip = MakeLoop(false); drone.clip = droneClip; drone.Play();
        var source = new GameObject("Distant building resonance"); source.transform.SetParent(transform, false);
        distant = source.AddComponent<AudioSource>(); distant.playOnAwake = false; distant.loop = true;
        distant.spatialBlend = 1; distant.rolloffMode = AudioRolloffMode.Linear; distant.minDistance = 4; distant.maxDistance = 22;
        distant.volume = 0; DeadMemorySettings.Route(distant); distantClip = MakeLoop(true); distant.clip = distantClip; distant.Play();
        initialized = true;
    }
    AudioClip MakeLoop(bool spatial)
    {
        const int rate = 22050, seconds = 16;
        var samples = new float[rate * seconds];
        var random = new System.Random(spatial ? 1983 : 0315);
        float noise = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            noise = Mathf.Lerp(noise, (float)random.NextDouble() * 2 - 1, .018f);
            // Integer loop cycles and a cosine envelope avoid a transient at the loop boundary.
            float envelope = Mathf.Pow(.5f - .5f * Mathf.Cos(t * Mathf.PI / 8), 2);
            float v = spatial ? Mathf.Sin(2 * Mathf.PI * t * (97 + 3 * Mathf.Sin(t * Mathf.PI / 4))) * .22f + noise * .7f
                              : Mathf.Sin(2 * Mathf.PI * 48 * t) * .3f + Mathf.Sin(2 * Mathf.PI * 48.5f * t) * .18f + noise * .4f;
            samples[i] = v * envelope * .5f;
        }
        var clip = AudioClip.Create(spatial ? "Distant warped timber and air" : "Memory low resonance", samples.Length, 1, rate, false);
        clip.SetData(samples, 0); return clip;
    }
    void LateUpdate()
    {
        if (!initialized || sanity == null) return;
        Redness = Mathf.SmoothStep(0, 1, sanity.Distortion);
        RenderSettings.ambientLight = Color.Lerp(ambient, new Color(.22f, .035f, .027f), Redness);
        RenderSettings.fogColor = Color.Lerp(fog, new Color(.20f, .022f, .018f), Redness);
        RenderSettings.fogDensity = Mathf.Lerp(fogDensity, fogDensity * 1.45f, Redness);
        for (int i = 0; i < lights.Count; i++)
        {
            if (lights[i] == null) continue;
            lights[i].color = Color.Lerp(colors[i], new Color(1f, .095f, .045f), Redness);
            lights[i].intensity = intensities[i] * Mathf.Lerp(1, .84f, Redness);
        }
        bool playing = SchoolPuzzleDirector.Instance != null && SchoolPuzzleDirector.Instance.Started && !SchoolPuzzleDirector.Instance.state.complete;
        float audible = playing && !SchoolPuzzleDirector.ModalOpen ? Redness : 0;
        drone.volume = Mathf.MoveTowards(drone.volume, audible * maxDroneVolume, Time.deltaTime * .07f);
        distant.volume = Mathf.MoveTowards(distant.volume, Mathf.SmoothStep(.15f, 1, audible) * audible * maxDistantVolume, Time.deltaTime * .05f);
        phase += Time.deltaTime * .08f;
        distant.transform.position = sanity.transform.position + new Vector3(Mathf.Sin(phase) * 9, 1.2f, Mathf.Cos(phase * .73f) * 9);
    }
    void OnDisable()
    {
        if (!initialized) return;
        for (int i = 0; i < lights.Count; i++) if (lights[i] != null) { lights[i].color = colors[i]; lights[i].intensity = intensities[i]; }
        RenderSettings.ambientLight = ambient; RenderSettings.fogColor = fog; RenderSettings.fogDensity = fogDensity;
        if (drone != null) drone.Stop(); if (distant != null) distant.Stop();
    }
    void OnDestroy() { if (droneClip != null) Destroy(droneClip); if (distantClip != null) Destroy(distantClip); }
}
