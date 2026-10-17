using UnityEngine;
using UnityEngine.UI;

/// <summary>Runtime horror HUD: sanity head, stamina bar, and ECG health trace.</summary>
[DisallowMultipleComponent]
public class SanityHUD : MonoBehaviour
{
    Canvas canvas;
    Text sanityValue;
    SanityHeadGraphic sanityHead;
    Image staminaFill;
    Text staminaValue;
    SanityHeartbeatGraphic heartbeat;
    StaminaSystem staminaSystem;
    float healthBaseRate = 1.05f;
    float healthBaseStrength = .20f;
    Color healthTint = new Color(.46f, .82f, .60f, .92f);
    bool healthDead;
    float fatigueAmount;

    Sprite whiteSprite;

    public void Initialize(Camera playerCamera, SanitySystem system)
    {
        if (canvas != null) return;

        Texture2D pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.name = "SanityHUD_WhitePixel";
        pixel.hideFlags = HideFlags.DontSave;
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply(false, true);
        whiteSprite = Sprite.Create(pixel, new Rect(0f, 0f, 1f, 1f), new Vector2(.5f, .5f), 100f);
        whiteSprite.name = "SanityHUD_WhiteSprite";
        whiteSprite.hideFlags = HideFlags.DontSave;

        GameObject canvasObject = new GameObject("SanityHUD_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        BuildSanityMeter(root);
        BuildStaminaMeter(root);
        BuildHeartbeat(root);

        SetSanity(system.CurrentSanity, system.maxSanity, system.Distortion);
        SetHealth(system.Health, system.maxHealth);
        staminaSystem = system.GetComponent<StaminaSystem>();
        SetStamina(staminaSystem != null ? staminaSystem.Normalized : 1f);
    }

    void Update()
    {
        if (staminaSystem != null)
            SetStamina(staminaSystem.Normalized);
    }

void BuildSanityMeter(RectTransform canvasRoot)
    {
        RectTransform head = MakeRect("SanityHead", canvasRoot, new Vector2(142f, 154f),
                                      new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(90f, 70f));
        if (head.GetComponent<CanvasRenderer>() == null)
            head.gameObject.AddComponent<CanvasRenderer>();
        sanityHead = head.gameObject.AddComponent<SanityHeadGraphic>();
        sanityHead.color = new Color(.78f, .82f, .83f, .82f);
        sanityHead.raycastTarget = false;

        sanityValue = MakeText("SanityValue", head, new Vector2(100f, 42f),
                               new Vector2(.45f, .50f), new Vector2(.5f, .5f), Vector2.zero,
                               30, TextAnchor.MiddleCenter);
        sanityValue.fontStyle = FontStyle.Bold;
        sanityValue.color = Color.white;
    }

void BuildHeartbeat(RectTransform canvasRoot)
    {
        RectTransform pulse = MakeRect("HealthHeartbeat", canvasRoot, new Vector2(420f, 80f),
                                      new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -28f));
        if (pulse.GetComponent<CanvasRenderer>() == null)
            pulse.gameObject.AddComponent<CanvasRenderer>();
        heartbeat = pulse.gameObject.AddComponent<SanityHeartbeatGraphic>();
        heartbeat.raycastTarget = false;
    }

    void BuildStaminaMeter(RectTransform canvasRoot)
    {
        RectTransform group = MakeRect("StaminaMeter", canvasRoot, new Vector2(300f, 58f),
                                      new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-56f, 28f));
        Image plate = group.gameObject.AddComponent<Image>();
        plate.sprite = whiteSprite;
        plate.color = new Color(.012f, .020f, .025f, .74f);
        plate.raycastTarget = false;

        Text label = MakeText("StaminaLabel", group, new Vector2(174f, 22f),
                              new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -5f),
                              16, TextAnchor.MiddleLeft);
        label.text = "STAMINA";
        label.fontStyle = FontStyle.Bold;
        label.color = new Color(.27f, .91f, .94f, .96f);

        staminaValue = MakeText("StaminaValue", group, new Vector2(80f, 22f),
                                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -5f),
                                16, TextAnchor.MiddleRight);
        staminaValue.fontStyle = FontStyle.Bold;
        staminaValue.color = new Color(.84f, .92f, .90f, .96f);

        RectTransform track = MakeRect("StaminaTrack", group, new Vector2(280f, 18f),
                                       new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 9f));
        Image trackImage = track.gameObject.AddComponent<Image>();
        trackImage.sprite = whiteSprite;
        trackImage.color = new Color(.07f, .11f, .12f, .98f);
        trackImage.raycastTarget = false;

        RectTransform fill = MakeRect("StaminaFill", track, new Vector2(280f, 18f),
                                      new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero);
        staminaFill = fill.gameObject.AddComponent<Image>();
        staminaFill.sprite = whiteSprite;
        staminaFill.type = Image.Type.Filled;
        staminaFill.fillMethod = Image.FillMethod.Horizontal;
        staminaFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        staminaFill.fillAmount = 1f;
        staminaFill.color = new Color(.27f, .91f, .94f, 1f);
        staminaFill.raycastTarget = false;
    }

    void SetStamina(float normalized)
    {
        float value = Mathf.Clamp01(normalized);
        if (staminaFill != null)
        {
            staminaFill.fillAmount = value;
            staminaFill.color = new Color(.27f, .91f, .94f, 1f);
        }
        if (staminaValue != null)
            staminaValue.text = Mathf.CeilToInt(value * 100f).ToString("00");
    }

    RectTransform MakeRect(string objectName, RectTransform parent, Vector2 size,
                           Vector2 anchor, Vector2 pivot, Vector2 position)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    Text MakeText(string objectName, RectTransform parent, Vector2 size,
                  Vector2 anchor, Vector2 pivot, Vector2 position, int fontSize, TextAnchor alignment)
    {
        RectTransform rect = MakeRect(objectName, parent, size, anchor, pivot, position);
        Text text = rect.gameObject.AddComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    public void SetSanity(float value, float maximum, float distortion)
    {
        if (sanityValue == null) return;
        sanityValue.text = Mathf.RoundToInt(value).ToString();
    }

public void SetHealth(int health, int maximum)
    {
        if (heartbeat == null) return;

        int max = Mathf.Max(1, maximum);
        int remaining = Mathf.Clamp(health, 0, max);
        int hits = max - remaining;
        healthDead = remaining == 0;

        if (healthDead) healthTint = new Color(1f, .08f, .06f, 1f);
        else if (hits == 0) healthTint = new Color(.46f, .82f, .60f, .92f);
        else if (hits == 1) healthTint = new Color(.18f, 1f, .34f, 1f);
        else if (hits == 2) healthTint = new Color(.60f, 1f, .18f, 1f);
        else if (hits == 3) healthTint = new Color(1f, .74f, .12f, 1f);
        else healthTint = new Color(1f, .28f, .10f, 1f);

        float[] rateByHit = { 1.05f, 1.35f, 1.65f, 1.95f, 2.25f };
        float[] strengthByHit = { .20f, .72f, .84f, .93f, 1f };
        healthBaseRate = rateByHit[Mathf.Clamp(hits, 0, rateByHit.Length - 1)];
        healthBaseStrength = strengthByHit[Mathf.Clamp(hits, 0, strengthByHit.Length - 1)];
        RefreshHeartbeat();
    }

public void SetFatigue(float staminaNormalized)
    {
        fatigueAmount = 1f - Mathf.Clamp01(staminaNormalized);
        RefreshHeartbeat();
    }

    void RefreshHeartbeat()
    {
        if (heartbeat == null) return;
        float rate = healthDead ? 0f : healthBaseRate + fatigueAmount * .85f;
        float strength = healthDead ? 0f : Mathf.Clamp01(healthBaseStrength + fatigueAmount * .20f);
        heartbeat.SetPulse(rate, healthTint, strength, healthDead);
    }


    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
        if (whiteSprite != null) Destroy(whiteSprite);
    }
}
