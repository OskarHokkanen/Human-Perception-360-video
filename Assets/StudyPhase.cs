using System.Globalization;
using TMPro;
using UnityEngine;

// Base class for anything that runs in a study scene (e.g. the number game).
// StudyFlow calls Begin() after the scene is loaded and the player is placed.
// Call TaskDone() when the task is completed (stops the soundtrack),
// and Finish() when the participant is ready to move on.
public abstract class StudyPhase : MonoBehaviour
{
    bool begun;

    public void Begin()
    {
        if (begun) return;
        begun = true;
        OnBegin();
    }

    protected abstract void OnBegin();

    // Lets you press Play directly in this scene for quick testing.
    protected virtual void Start()
    {
        if (!StudyFlow.Instance) Begin();
    }

    protected void TaskDone()
    {
        if (StudyFlow.Instance) StudyFlow.Instance.TaskDone();
        else Debug.Log("[Study] Task done");
    }

    protected void Finish()
    {
        if (StudyFlow.Instance) StudyFlow.Instance.FinishPhase();
        else Debug.Log("[Study] Phase finished");
    }

    protected static bool IsLastTrial => StudyFlow.Instance && StudyFlow.Instance.IsLastTrial;

    protected void Log(string evt, string detail = "")
    {
        if (StudyFlow.Instance) StudyFlow.Instance.LogEvent(evt, detail);
        else Debug.Log($"[Study] {evt} {detail}");
    }

    // Eye position and the horizontal direction the participant is facing.
    protected static void GetHeadPose(out Vector3 eye, out Vector3 flatForward)
    {
        var cam = Camera.main;
        eye = cam ? cam.transform.position : new Vector3(0f, 1.6f, 0f);
        flatForward = cam ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up) : Vector3.forward;
        if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;
        flatForward.Normalize();
    }

    // Text that shrinks automatically to fit its box. Readable from the participant's side (local -Z).
    protected static TextMeshPro CreateText(Transform parent, string name, string text,
                                            Vector3 localPos, Vector2 size, float maxFontSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.sizeDelta = size;
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.01f;
        t.fontSizeMax = maxFontSize;
        return t;
    }

    // Number formatting that always uses a decimal point (safe for CSV on Swedish systems).
    protected static string F(float v) => v.ToString("F2", CultureInfo.InvariantCulture);
}