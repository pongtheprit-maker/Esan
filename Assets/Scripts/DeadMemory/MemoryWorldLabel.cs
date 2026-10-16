using UnityEngine;

[ExecuteAlways, RequireComponent(typeof(TextMesh))]
public class MemoryWorldLabel : MonoBehaviour
{
    static Font thaiFont;
    static Material depthMaterial;
    static bool subscribed;
    public Shader depthShader;
    public static Font Font
    {
        get
        {
            if (thaiFont == null) thaiFont = UnityEngine.Font.CreateDynamicFontFromOSFont(new[] { "Tahoma", "Leelawadee UI", "Noto Sans Thai", "Arial" }, 64);
            return thaiFont;
        }
    }
    void OnEnable() { Apply(); }
    public void Apply()
    {
        TextMesh mesh = GetComponent<TextMesh>();
        if (Font == null) return;
        mesh.font = Font;
        mesh.fontSize = 64;
        if (depthShader == null) depthShader = Shader.Find("Sprites/Default");
        if (depthMaterial == null)
        {
            depthMaterial = new Material(depthShader);
            depthMaterial.name = "Memory world text (depth tested)";
            depthMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        depthMaterial.mainTexture = Font.material.mainTexture;
        GetComponent<MeshRenderer>().sharedMaterial = depthMaterial;
        if (!subscribed) { UnityEngine.Font.textureRebuilt += RebuildAtlas; subscribed = true; }
    }
    static void RebuildAtlas(Font font) { if (font == thaiFont && depthMaterial != null) depthMaterial.mainTexture = font.material.mainTexture; }
}
