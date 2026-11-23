using System;
using TMPro;
using UnityEngine;

// A flat button with a number on it. Pressed when a Presser tip touches it.
// Created from code with PokeButton.Create(...).
public class PokeButton : MonoBehaviour
{
    public event Action<PokeButton> Pressed;

    public int Value { get; private set; }
    public bool Interactable { get; set; } = true;

    Material faceMaterial;
    float lastPressTime = -10f;
    const float Debounce = 0.3f;

    public static PokeButton Create(Transform parent, string text, int value, Vector2 size, Color color)
    {
        var root = new GameObject("Button " + text);
        root.transform.SetParent(parent, false);

        // Touch area: a little thicker than the button so presses register reliably.
        var col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(size.x, size.y, 0.03f);

        var face = StudyMaterials.CreateQuad(root.transform, "Face", Vector3.zero, size, color);

        // Number on the side facing the participant (local -Z), sized to fit the button.
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(root.transform, false);
        labelGO.transform.localPosition = new Vector3(0f, 0f, -0.002f);
        var tmp = labelGO.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = size * 0.8f;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.01f;
        tmp.fontSizeMax = 1f;

        var button = root.AddComponent<PokeButton>();
        button.Value = value;
        button.faceMaterial = face.sharedMaterial;
        return button;
    }

    public void SetColor(Color c) => StudyMaterials.SetColor(faceMaterial, c);

    // For testing from code or the Editor.
    public void SimulatePress() => Pressed?.Invoke(this);

    void OnTriggerEnter(Collider other)
    {
        if (!Interactable) return;
        if (!other.GetComponentInParent<Presser>()) return;
        if (Time.time - lastPressTime < Debounce) return;
        lastPressTime = Time.time;
        Pressed?.Invoke(this);
    }
}