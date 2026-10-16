using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

[DefaultExecutionOrder(-500), DisallowMultipleComponent]
public sealed class DeadMemoryTitleMenu : MonoBehaviour
{
    public static DeadMemoryTitleMenu Instance { get; private set; }
    public static bool IsVisible { get { return Instance != null && Instance.visible; } }
    public Texture2D background;
    public Font regularFont, titleFont;
    public Shader signalShader;
    public DeadMemorySettings settings;
    GameObject canvasObject, mainPanel, optionsPanel, optionsShade, loadPanel, newGamePanel;
    RectTransform titleRect, cyanTitle, redTitle, backgroundRect;
    TitleMenuButton optionsButton, backButton, continueButton, newGameButton, loadGameButton, loadCheckpointButton, loadBackButton, confirmNewButton;
    TitleMenuButton[] qualityButtons;
    Text qualityDescription, saveStatus, checkpointDetails;
    Material signalMaterial;
    Texture2D auraTexture, selectionTexture;
    Sprite auraSprite, selectionSprite;
    AudioSource uiAudio;
    AudioClip hoverClip, clickClip;
    bool visible = true, enteringGame;
    int page;
    float nextGlitch, glitchUntil, lastHover;
    Vector2 titleHome;
    readonly Color ink = new Color(.83f, .91f, .90f);
    readonly Color cyan = new Color(.32f, .80f, .76f);

    void Awake()
    {
        Instance = this;
        if (settings == null) settings = GetComponent<DeadMemorySettings>();
        auraTexture = MakeAura();
        auraSprite = Sprite.Create(auraTexture, new Rect(0, 0, auraTexture.width, auraTexture.height), new Vector2(.5f, .5f));
        selectionTexture = MakeSelection();
        selectionSprite = Sprite.Create(selectionTexture, new Rect(0, 0, selectionTexture.width, selectionTexture.height), new Vector2(.5f, .5f));
        uiAudio = gameObject.AddComponent<AudioSource>();
        uiAudio.playOnAwake = false; uiAudio.spatialBlend = 0; uiAudio.volume = .16f;
        DeadMemorySettings.Route(uiAudio);
        hoverClip = MakeTick(false); clickClip = MakeTick(true);
        Build(); nextGlitch = Time.unscaledTime + 2.7f;
    }
    void Start() { RefreshCheckpoint(); }
    void Build()
    {
        if (EventSystem.current == null)
        {
            var events = new GameObject("Title Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        canvasObject = new GameObject("Title Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 12000;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var backdrop = Node("Background", canvasObject.transform, Vector2.zero, Vector2.zero); Stretch(backdrop);
        backgroundRect = backdrop; backgroundRect.localScale = Vector3.one * 1.025f;
        var image = backdrop.gameObject.AddComponent<RawImage>(); image.texture = background; image.color = Color.white; image.raycastTarget = false;
        var fit = backdrop.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = background != null ? background.width / (float)background.height : 16f / 9f;
        if (signalShader != null) { signalMaterial = new Material(signalShader); image.material = signalMaterial; }

        var dimmer = Image("Options shade", canvasObject.transform, Vector2.zero, Vector2.zero, new Color(.012f, .026f, .029f, .88f));
        Stretch(dimmer.rectTransform); optionsShade = dimmer.gameObject; optionsShade.SetActive(false);

        var frame = Node("Safe Content", canvasObject.transform, Vector2.zero, new Vector2(1920, 1080));
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0, .5f);
        mainPanel = Node("Main Menu", frame, Vector2.zero, Vector2.zero).gameObject; Stretch((RectTransform)mainPanel.transform);
        // The title is live text; three layers separate briefly during signal loss.
        redTitle = Label("Title Red", mainPanel.transform, "DeadMemory", new Vector2(143, 143), new Vector2(1140, 180), 132, new Color(.7f, .18f, .2f, .35f), true).rectTransform;
        cyanTitle = Label("Title Cyan", mainPanel.transform, "DeadMemory", new Vector2(137, 141), new Vector2(1140, 180), 132, new Color(.18f, .8f, .77f, .45f), true).rectTransform;
        titleRect = Label("DeadMemory", mainPanel.transform, "DeadMemory", new Vector2(140, 142), new Vector2(1140, 180), 132, Color.white, true).rectTransform;
        titleHome = titleRect.anchoredPosition;
        Label("Chapter", mainPanel.transform, "CASE 01   /   THE SCHOOL", new Vector2(151, 323), new Vector2(650, 30), 17, new Color(.44f, .59f, .58f));
        continueButton = MenuButton(mainPanel.transform, "CONTINUE", new Vector2(160, 407), new Vector2(570, 60), ContinueGame);
        newGameButton = MenuButton(mainPanel.transform, "NEW GAME", new Vector2(160, 472), new Vector2(570, 60), RequestNewGame);
        loadGameButton = MenuButton(mainPanel.transform, "LOAD GAME", new Vector2(160, 537), new Vector2(570, 60), ShowLoadGame);
        optionsButton = MenuButton(mainPanel.transform, "OPTIONS", new Vector2(160, 602), new Vector2(570, 60), delegate { ShowOptions(true); });
        MenuButton(mainPanel.transform, "EXIT", new Vector2(160, 667), new Vector2(570, 60), Exit);
        saveStatus = Label("Save status", mainPanel.transform, "", new Vector2(175, 760), new Vector2(1060, 55), 18, new Color(.56f, .69f, .67f));
        Label("Footer", mainPanel.transform, "DEAD MEMORY", new Vector2(151, 1010), new Vector2(650, 28), 14, new Color(.32f, .44f, .43f));
        continueButton.button.Select();

        optionsPanel = Node("Options", frame, Vector2.zero, Vector2.zero).gameObject; Stretch((RectTransform)optionsPanel.transform);
        Label("Options title", optionsPanel.transform, "OPTIONS", new Vector2(160, 136), new Vector2(1000, 120), 76, Color.white, true);
        Label("Audio", optionsPanel.transform, "AUDIO", new Vector2(167, 302), new Vector2(700, 40), 18, cyan);
        AudioSlider("MASTER VOLUME", 373, settings.Master, settings.SetMaster);
        AudioSlider("MUSIC", 466, settings.Music, settings.SetMusic);
        AudioSlider("SFX", 559, settings.Sfx, settings.SetSfx);
        Label("Graphics", optionsPanel.transform, "GRAPHICS", new Vector2(167, 697), new Vector2(700, 40), 18, cyan);
        qualityButtons = new TitleMenuButton[3];
        var names = new[] { "LOW", "MEDIUM", "HIGH" };
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            qualityButtons[i] = MenuButton(optionsPanel.transform, names[i], new Vector2(155 + i * 250, 754), new Vector2(235, 64), delegate { settings.SetGraphics(index); RefreshQuality(); });
        }
        qualityDescription = Label("Quality detail", optionsPanel.transform, "", new Vector2(170, 837), new Vector2(1000, 42), 18, new Color(.48f, .62f, .61f));
        backButton = MenuButton(optionsPanel.transform, "BACK", new Vector2(155, 932), new Vector2(260, 62), delegate { ShowOptions(false); });
        Label("Saved", optionsPanel.transform, "SETTINGS SAVE AUTOMATICALLY", new Vector2(940, 960), new Vector2(700, 32), 14, new Color(.36f, .49f, .48f));
        RefreshQuality(); optionsPanel.SetActive(false);

        loadPanel = Node("Load Game", frame, Vector2.zero, Vector2.zero).gameObject; Stretch((RectTransform)loadPanel.transform);
        Label("Load title", loadPanel.transform, "LOAD GAME", new Vector2(160, 136), new Vector2(1200, 120), 76, Color.white, true);
        Label("Slot name", loadPanel.transform, "AUTOSAVE   /   CASE 01 - THE SCHOOL", new Vector2(175, 330), new Vector2(1350, 65), 29, cyan);
        checkpointDetails = Label("Checkpoint details", loadPanel.transform, "", new Vector2(175, 419), new Vector2(1440, 145), 26, ink);
        loadCheckpointButton = MenuButton(loadPanel.transform, "LOAD CHECKPOINT", new Vector2(160, 630), new Vector2(640, 66), ContinueGame);
        loadBackButton = MenuButton(loadPanel.transform, "BACK", new Vector2(160, 751), new Vector2(300, 62), delegate { SetPage(0); });
        loadPanel.SetActive(false);

        newGamePanel = Node("New Game Confirmation", frame, Vector2.zero, Vector2.zero).gameObject; Stretch((RectTransform)newGamePanel.transform);
        Label("New game title", newGamePanel.transform, "NEW GAME", new Vector2(160, 136), new Vector2(1200, 120), 76, Color.white, true);
        Label("Overwrite save", newGamePanel.transform, "START A NEW MEMORY?\nYour current autosave will be replaced.", new Vector2(175, 337), new Vector2(1450, 140), 29, ink);
        confirmNewButton = MenuButton(newGamePanel.transform, "START NEW GAME", new Vector2(160, 548), new Vector2(640, 66), StartNewGame);
        MenuButton(newGamePanel.transform, "BACK", new Vector2(160, 665), new Vector2(300, 62), delegate { SetPage(0); });
        newGamePanel.SetActive(false);
    }
    void AudioSlider(string name, float y, float value, UnityEngine.Events.UnityAction<float> changed)
    {
        Label(name, optionsPanel.transform, name, new Vector2(170, y), new Vector2(380, 48), 26, ink);
        Text number = Label(name + " value", optionsPanel.transform, Mathf.RoundToInt(value * 100) + "%", new Vector2(1150, y), new Vector2(150, 48), 24, cyan);
        var root = Node(name + " slider", optionsPanel.transform, new Vector2(571, y + 3), new Vector2(540, 44));
        var slider = root.gameObject.AddComponent<Slider>();
        // The transparent image expands the pointer target beyond the thin track.
        var hit = root.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, .001f);
        var track = Image("Track", root, new Vector2(0, 19), new Vector2(540, 4), new Color(.16f, .25f, .25f));
        var fillArea = Node("Fill Area", root, new Vector2(7, 19), new Vector2(526, 4));
        var fill = Image("Fill", fillArea, Vector2.zero, Vector2.zero, new Color(.38f, .73f, .70f)); Stretch(fill.rectTransform);
        var handles = Node("Handle Area", root, new Vector2(7, 0), new Vector2(526, 44));
        var handle = Image("Handle", handles, Vector2.zero, new Vector2(13, 27), new Color(.77f, .95f, .91f));
        handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = new Vector2(0, .5f); handle.rectTransform.pivot = new Vector2(.5f, .5f);
        handle.rectTransform.sizeDelta = new Vector2(13, -17);
        slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight; slider.minValue = 0; slider.maxValue = 1;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(delegate(float v) { number.text = Mathf.RoundToInt(v * 100) + "%"; changed(v); });
    }
    void RefreshQuality()
    {
        if (qualityButtons == null) return;
        for (int i = 0; i < 3; i++) qualityButtons[i].latched = settings.Graphics == i;
        qualityDescription.text = new[] { "PRIORITIZE PERFORMANCE", "BALANCE QUALITY AND PERFORMANCE", "FULL RESOLUTION AND DETAILED SHADOWS" }[settings.Graphics];
    }
    public void ShowOptions(bool show)
    {
        SetPage(show ? 1 : 0);
    }
    void SetPage(int next)
    {
        int previous = page; page = next;
        mainPanel.SetActive(page == 0); optionsPanel.SetActive(page == 1);
        loadPanel.SetActive(page == 2); newGamePanel.SetActive(page == 3); optionsShade.SetActive(page != 0);
        if (page == 1) { RefreshQuality(); backButton.button.Select(); }
        else if (page == 2) (loadCheckpointButton.button.interactable ? loadCheckpointButton : loadBackButton).button.Select();
        else if (page == 3) confirmNewButton.button.Select();
        else
        {
            settings.Save(); RefreshCheckpoint();
            (previous == 1 ? optionsButton : previous == 2 ? loadGameButton : newGameButton).button.Select();
        }
    }
    void RefreshCheckpoint()
    {
        var director = SchoolPuzzleDirector.Instance;
        SchoolPuzzleProgress checkpoint = null; string reason = "SCHOOL LEVEL IS NOT AVAILABLE.";
        bool canLoad = director != null && director.TryReadCheckpoint(out checkpoint, out reason);
        continueButton.button.interactable = canLoad;
        loadCheckpointButton.button.interactable = canLoad;
        saveStatus.text = canLoad ? "" : reason;
        if (canLoad)
        {
            var elapsed = System.TimeSpan.FromSeconds(checkpoint.seconds);
            checkpointDetails.text = "FLOOR " + (checkpoint.position.y > 2.25f ? "2" : "1") +
                "    /    PLAY TIME " + elapsed.ToString(@"hh\:mm\:ss") + "\nEVIDENCE COLLECTED: " + checkpoint.notes.Count;
        }
        else checkpointDetails.text = reason;
        if (page == 0 && !canLoad) newGameButton.button.Select();
    }
    public void ShowLoadGame() { RefreshCheckpoint(); SetPage(2); }
    public void RequestNewGame()
    {
        var director = SchoolPuzzleDirector.Instance;
        if (director == null || enteringGame) return;
        if (PlayerPrefs.HasKey(director.SaveKey)) SetPage(3); else StartNewGame();
    }
    public void StartNewGame()
    {
        var director = SchoolPuzzleDirector.Instance;
        if (director == null || enteringGame) return;
        enteringGame = true;
        try { director.BeginNewCase(); DismissForGameplay(director); }
        finally { enteringGame = false; }
    }
    public void ContinueGame()
    {
        var director = SchoolPuzzleDirector.Instance;
        if (director == null || enteringGame) return;
        SchoolPuzzleProgress checkpoint; string reason;
        if (!director.TryReadCheckpoint(out checkpoint, out reason)) { RefreshCheckpoint(); return; }
        enteringGame = true;
        try { director.ContinueCase(); DismissForGameplay(director); }
        finally { enteringGame = false; }
    }
    void DismissForGameplay(SchoolPuzzleDirector director)
    {
        if (!director.Started || director.Modal != "") return;
        settings.Save(); visible = false; canvasObject.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
    }
    void Update()
    {
        var director = SchoolPuzzleDirector.Instance;
        visible = director == null || director.Modal == "start";
        if (canvasObject.activeSelf != visible) canvasObject.SetActive(visible);
        if (!visible) return;
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        if (page != 0 && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) SetPage(0);
        float t = Time.unscaledTime;
        if (t > nextGlitch) { glitchUntil = t + .13f; nextGlitch = t + Random.Range(3.5f, 7f); }
        bool glitch = t < glitchUntil;
        float shift = glitch ? Mathf.Sin(t * 153) * 7 : 0;
        titleRect.anchoredPosition = titleHome + new Vector2(shift, 0);
        cyanTitle.anchoredPosition = titleHome + new Vector2(-3 - shift * 1.5f, -1);
        redTitle.anchoredPosition = titleHome + new Vector2(3 + shift, 1);
        if (signalMaterial != null) { signalMaterial.SetFloat("_SignalTime", t); signalMaterial.SetFloat("_Glitch", glitch ? 1 : 0); }
        Vector2 pointer = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width, Screen.height) * .5f;
        Vector2 drift = new Vector2(Mathf.Clamp(pointer.x / Mathf.Max(1, Screen.width) - .5f, -.5f, .5f), Mathf.Clamp(pointer.y / Mathf.Max(1, Screen.height) - .5f, -.5f, .5f)) * -9f;
        backgroundRect.anchoredPosition = Vector2.Lerp(backgroundRect.anchoredPosition, drift, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 2f));
    }
    TitleMenuButton MenuButton(Transform parent, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var root = Node(text, parent, position, size);
        var hit = root.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, .001f);
        var glow = Image("Aura", root, new Vector2(-35, -17), size + new Vector2(70, 34), Color.clear);
        glow.sprite = auraSprite;
        var bar = Image("Selection", root, new Vector2(8, 3), size - new Vector2(20, 6), Color.clear);
        bar.sprite = selectionSprite;
        var stripe = Image("Edge", root, new Vector2(0, 4), new Vector2(5, size.y - 8), Color.clear);
        var upperTrace = Image("Upper signal trace", root, new Vector2(-36, 1), new Vector2(size.x + 12, 1), Color.clear);
        var lowerTrace = Image("Lower signal trace", root, new Vector2(-72, size.y - 1), new Vector2(size.x + 65, 1), Color.clear);
        var label = Label("Label", root, text, new Vector2(24, 1), size - new Vector2(28, 2), 41, ink);
        var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = hit; button.transition = Selectable.Transition.None;
        var effect = root.gameObject.AddComponent<TitleMenuButton>();
        effect.button = button; effect.label = label; effect.glow = glow; effect.bar = bar; effect.stripe = stripe; effect.owner = this;
        effect.upperTrace = upperTrace; effect.lowerTrace = lowerTrace;
        button.onClick.AddListener(delegate { effect.Pulse(); PlayClick(); action(); });
        return effect;
    }
    public void PlayHover()
    {
        if (Time.unscaledTime < lastHover + .075f) return;
        lastHover = Time.unscaledTime; uiAudio.PlayOneShot(hoverClip, .35f);
    }
    void PlayClick() { uiAudio.PlayOneShot(clickClip, .65f); }
    static void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    Text Label(string name, Transform parent, string value, Vector2 pos, Vector2 size, int fontSize, Color color, bool bold = false)
    {
        var rt = Node(name, parent, pos, size); var text = rt.gameObject.AddComponent<Text>();
        text.font = bold ? titleFont : regularFont; text.text = value; text.fontSize = fontSize; text.color = color;
        text.alignment = TextAnchor.MiddleLeft; text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow; text.raycastTarget = false; return text;
    }
    static RectTransform Node(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(pos.x, -pos.y); rt.sizeDelta = size; return rt;
    }
    static Image Image(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        var rt = Node(name, parent, pos, size); var image = rt.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }
    static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
    static Texture2D MakeAura()
    {
        var texture = new Texture2D(128, 64, TextureFormat.RGBA32, false); texture.name = "Menu aura"; texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[128 * 64];
        for (int y = 0; y < 64; y++) for (int x = 0; x < 128; x++)
        {
            float dx = (x - 63.5f) / 64, dy = (y - 31.5f) / 32;
            pixels[y * 128 + x] = new Color(.25f, .93f, .85f, Mathf.Pow(Mathf.Clamp01(1 - dx * dx - dy * dy), 2));
        }
        texture.SetPixels(pixels); texture.Apply(); return texture;
    }
    static Texture2D MakeSelection()
    {
        const int width = 256, height = 32;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Fragmented signal selection"; texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Point;
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            bool clipped = x >= width - Mathf.Max(0, 9 - y) || (y > 27 && x > 218 && x < 223) || (y < 3 && x > 50 && x < 58);
            float alpha = clipped ? 0 : (y == 6 || y == 23 ? .58f : 1f);
            pixels[y * width + x] = new Color(1, 1, 1, alpha);
        }
        texture.SetPixels(pixels); texture.Apply(); return texture;
    }
    static AudioClip MakeTick(bool click)
    {
        const int rate = 22050; int count = click ? 2205 : 882; var data = new float[count]; var random = new System.Random(click ? 47 : 31);
        for (int i = 0; i < count; i++) { float t = i / (float)rate, env = Mathf.Sin(Mathf.PI * i / count) * Mathf.Exp(-t * 50); data[i] = env * (Mathf.Sin(t * (click ? 440 : 680) * Mathf.PI * 2) * .22f + ((float)random.NextDouble() * 2 - 1) * .08f); }
        var clip = AudioClip.Create(click ? "Menu confirm" : "Menu hover", count, 1, rate, false); clip.SetData(data, 0); return clip;
    }
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (signalMaterial != null) Destroy(signalMaterial);
        if (auraSprite != null) Destroy(auraSprite); if (selectionSprite != null) Destroy(selectionSprite);
        if (auraTexture != null) Destroy(auraTexture);
        if (selectionTexture != null) Destroy(selectionTexture);
        if (hoverClip != null) Destroy(hoverClip); if (clickClip != null) Destroy(clickClip);
    }
}

public sealed class TitleMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    public Button button; public Text label; public Image glow, bar, stripe, upperTrace, lowerTrace; public DeadMemoryTitleMenu owner;
    public bool latched;
    bool hovered, selected, down;
    float intensity, pulse;
    Vector2 home;
    Color original;
    void Start() { home = label.rectTransform.anchoredPosition; original = label.color; }
    public void Pulse() { pulse = 1; down = false; }
    void Update()
    {
        bool available = button.IsInteractable();
        pulse = Mathf.MoveTowards(pulse, 0, Time.unscaledDeltaTime * 2.8f);
        intensity = Mathf.MoveTowards(intensity, available && (hovered || selected || latched) ? 1 : 0, Time.unscaledDeltaTime * 8);
        float breath = .92f + .08f * Mathf.Sin(Time.unscaledTime * 2.6f);
        glow.color = new Color(1, 1, 1, intensity * .22f * breath + pulse * .65f);
        bar.color = new Color(.30f, .67f, .64f, intensity * .63f + pulse * .15f);
        stripe.color = new Color(.57f, .95f, .87f, intensity);
        var trace = new Color(.38f, .88f, .84f, intensity * .34f + pulse * .4f);
        if (upperTrace != null) upperTrace.color = trace;
        if (lowerTrace != null) lowerTrace.color = trace;
        label.color = available ? Color.Lerp(original, new Color(.015f, .06f, .06f), intensity) : new Color(original.r, original.g, original.b, .3f);
        label.rectTransform.anchoredPosition = home + new Vector2(intensity * 6 + (down ? 3 : 0), 0);
        glow.rectTransform.localScale = Vector3.one * (1 + pulse * .12f);
    }
    public void OnPointerEnter(PointerEventData e) { if (!button.IsInteractable()) return; hovered = true; owner.PlayHover(); }
    public void OnPointerExit(PointerEventData e) { hovered = false; down = false; }
    public void OnPointerDown(PointerEventData e) { if (!button.IsInteractable()) return; down = true; pulse = .7f; }
    public void OnPointerUp(PointerEventData e) { down = false; }
    public void OnSelect(BaseEventData e) { selected = true; }
    public void OnDeselect(BaseEventData e) { selected = false; }
    void OnDisable() { hovered = selected = down = false; pulse = 0; }
}
