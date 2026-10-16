using UnityEngine;

/// <summary>Separate title and school music, with a quiet crossfade between them.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class SchoolBackgroundMusic : MonoBehaviour
{
    [SerializeField] AudioClip music;
    [SerializeField] AudioClip titleMusic;
    [SerializeField] AudioSource titleSource;
    [SerializeField, Range(0f, 1f)] float titleVolume = 0.04f;
    [SerializeField, Range(0f, 1f)] float gameplayVolume = 0.085f;
    [SerializeField, Range(0f, 1f)] float readingVolume = 0.06f;
    [SerializeField, Min(0.1f)] float fadeSeconds = 4f;
    AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        if (titleSource == null)
        {
            var child = transform.Find("Title Music");
            if (child == null)
            {
                var go = new GameObject("Title Music");
                go.transform.SetParent(transform, false); child = go.transform;
            }
            titleSource = child.GetComponent<AudioSource>();
            if (titleSource == null) titleSource = child.gameObject.AddComponent<AudioSource>();
        }
        Configure(source, music);
        Configure(titleSource, titleMusic);
    }

    static void Configure(AudioSource audio, AudioClip clip)
    {
        audio.playOnAwake = false; audio.Stop(); audio.clip = clip;
        audio.loop = true; audio.spatialBlend = 0f; audio.dopplerLevel = 0f;
        audio.priority = 128; audio.volume = 0f;
        DeadMemorySettings.Route(audio, true);
    }

    void Update()
    {
        var director = SchoolPuzzleDirector.Instance;
        bool title = DeadMemoryTitleMenu.IsVisible;
        bool gameplay = !title && director != null && director.Started && !director.state.complete;
        float gameplayTarget = gameplay ? (SchoolPuzzleDirector.ModalOpen ? readingVolume : gameplayVolume) : 0f;
        Fade(source, gameplayTarget, gameplayVolume);
        Fade(titleSource, title ? titleVolume : 0f, titleVolume);
    }

    void Fade(AudioSource audio, float target, float fullVolume)
    {
        if (audio == null) return;
        if (target > 0f && audio.clip != null && !audio.isPlaying) audio.Play();
        audio.volume = Mathf.MoveTowards(audio.volume, target,
            Mathf.Max(0.001f, fullVolume) * Time.unscaledDeltaTime / Mathf.Max(0.1f, fadeSeconds));
        if (target <= 0f && audio.volume <= 0f && audio.isPlaying) audio.Stop();
    }

    void OnDisable()
    {
        if (source != null) { source.Stop(); source.volume = 0f; }
        if (titleSource != null) { titleSource.Stop(); titleSource.volume = 0f; }
    }
}
