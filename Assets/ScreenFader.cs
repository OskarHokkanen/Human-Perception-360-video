using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// Fades the headset view to and from black using a small sphere around
// the head, so it looks correct in both eyes.
// Put it in the Bootstrap scene, assign the XR head camera (e.g. CenterEyeAnchor)
// and a material that uses the "Study/ScreenFade" shader.
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] Camera headCamera;
    [Tooltip("A material using the Study/ScreenFade shader.")]
    [SerializeField] Material fadeMaterial;
    [SerializeField] Color color = Color.black;
    [SerializeField] bool startBlack = true;

    Material mat;
    MeshRenderer sphere;
    float alpha;

    void Awake()
    {
        Instance = this;
        if (!headCamera) headCamera = Camera.main;

        if (fadeMaterial)
        {
            mat = new Material(fadeMaterial);
        }
        else
        {
            var shader = Shader.Find("Study/ScreenFade");
            if (!shader)
            {
                Debug.LogError("ScreenFader: assign a material that uses the Study/ScreenFade shader.");
                return;
            }
            mat = new Material(shader);
        }

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "FadeSphere";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(headCamera.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        // Must be larger than the camera's near clip plane to be visible.
        float radius = Mathf.Max(0.2f, headCamera.nearClipPlane * 3f);
        go.transform.localScale = Vector3.one * radius * 2f;

        sphere = go.GetComponent<MeshRenderer>();
        sphere.sharedMaterial = mat;
        sphere.shadowCastingMode = ShadowCastingMode.Off;
        sphere.receiveShadows = false;

        SetAlpha(startBlack ? 1f : 0f);
    }

    // 1 = fully black, 0 = fully visible
    public IEnumerator FadeTo(float target, float seconds)
    {
        if (!sphere) yield break;
        float start = alpha;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            SetAlpha(Mathf.Lerp(start, target, t / seconds));
            yield return null;
        }
        SetAlpha(target);
    }

    void SetAlpha(float a)
    {
        alpha = a;
        var c = color;
        c.a = a;
        mat.SetColor("_Color", c);
        // Hide the sphere when fully clear so it costs nothing while playing.
        sphere.enabled = a > 0.001f;
    }
}