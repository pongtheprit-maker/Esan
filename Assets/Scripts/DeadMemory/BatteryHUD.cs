using UnityEngine;
using UnityEngine.UI;

/// <summary>Original four-segment battery silhouette, upper left. No external game artwork.</summary>
public class BatteryHUD : MonoBehaviour
{
    Canvas canvas;
    Image[] bars;
    FlashlightBattery battery;
    Image Part(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var g = new GameObject(name, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(parent, false);
        var r = g.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = new Vector2(0, 1);
        r.pivot = new Vector2(0, 1); r.anchoredPosition = position; r.sizeDelta = size;
        var image = g.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }
    void Start()
    {
        battery = GetComponent<FlashlightBattery>();
        var g = new GameObject("FlashlightBatteryHUD_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        g.transform.SetParent(transform, false); canvas = g.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 101;
        var scaler = g.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var frame = Part(g.transform, "Battery outline", new Vector2(48, -40), new Vector2(126, 43), new Color(.77f, .81f, .73f, .86f));
        Part(frame.transform, "Dark interior", new Vector2(3, -3), new Vector2(120, 37), new Color(.018f, .025f, .022f, .96f));
        Part(frame.transform, "Terminal", new Vector2(126, -12), new Vector2(7, 19), new Color(.77f, .81f, .73f, .86f));
        bars = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            Part(frame.transform, "Empty cell " + i, new Vector2(8 + i * 28, -8), new Vector2(23, 27), new Color(.12f, .15f, .12f, .8f));
            bars[i] = Part(frame.transform, "Charge cell " + i, new Vector2(8 + i * 28, -8), new Vector2(23, 27), Color.white);
        }
    }
    void LateUpdate()
    {
        if (canvas == null || battery == null) return;
        var d = SchoolPuzzleDirector.Instance;
        canvas.enabled = d == null || (d.Started && !d.state.complete && !SchoolPuzzleDirector.ModalOpen);
        for (int i = 0; i < bars.Length; i++)
        {
            float fraction = Mathf.Clamp01(battery.Cells - i);
            bars[i].rectTransform.sizeDelta = new Vector2(23 * fraction, 27);
            bars[i].color = battery.Cells <= 1 ? new Color(.8f, .34f, .25f, .95f) : new Color(.76f, .83f, .65f, .94f);
        }
    }
    void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); }
}
