using UnityEngine;

// Creates simple flat-colour materials that render correctly on Quest.
// Requires "Study/UnlitColor" in Project Settings > Graphics > Always Included Shaders.
public static class StudyMaterials
{
    static Shader shader;
    static readonly int ColorId = Shader.PropertyToID("_Color");

    public static Material Create(Color color)
    {
        if (!shader) shader = Shader.Find("Study/UnlitColor");
        if (!shader)
        {
            Debug.LogError("Shader 'Study/UnlitColor' not found. Add it to " +
                           "Project Settings > Graphics > Always Included Shaders.");
            shader = Shader.Find("Hidden/InternalErrorShader");
        }
        var m = new Material(shader);
        m.SetColor(ColorId, color);
        return m;
    }

    public static void SetColor(Material m, Color color) => m.SetColor(ColorId, color);

    // A flat coloured rectangle (no collider, no shadows).
    public static Renderer CreateQuad(Transform parent, string name, Vector3 localPos, Vector2 size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = Create(color);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return r;
    }
}