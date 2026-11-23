using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class SurveyQuestion
{
    public string id = "Q1";
    [TextArea] public string text = "How pleasant was the environment?";
    [Tooltip("Number of answer buttons, e.g. 5 or 7.")]
    public int points = 7;
    public string lowLabel = "Not at all";
    public string highLabel = "Very much";
}

// Shows rating-scale questions one at a time on a flat board in front of the participant.
// Answers are pressed with the same touch tips as the game and logged to the study CSV.
public class SurveyManager : StudyPhase
{
    [SerializeField] List<SurveyQuestion> questions = new List<SurveyQuestion>();

    [Header("Placement (metres)")]
    [SerializeField] float distance = 0.45f;
    [SerializeField] float belowEyes = 0.2f;
    [SerializeField] float buttonSize = 0.06f;
    [SerializeField] float buttonSpacing = 0.075f;

    [Header("Colours")]
    [SerializeField] Color boardColor = new Color(0.15f, 0.15f, 0.17f);
    [SerializeField] Color buttonColor = new Color(0.95f, 0.95f, 0.95f);
    [SerializeField] Color selectedColor = new Color(0.35f, 0.6f, 0.95f);

    [Tooltip("Seconds the chosen answer stays highlighted before the next question.")]
    [SerializeField] float confirmDelay = 0.5f;

    Transform board;
    TextMeshPro progressText, questionText, lowText, highText;
    readonly List<PokeButton> buttons = new List<PokeButton>();
    int index;
    bool answering;
    float shownAt;

    protected override void OnBegin()
    {
        GetHeadPose(out var eye, out var fwd);

        board = new GameObject("SurveyBoard").transform;
        board.SetParent(transform, false);
        board.SetPositionAndRotation(eye + fwd * distance + Vector3.down * belowEyes,
                                     Quaternion.LookRotation(fwd));

        StudyMaterials.CreateQuad(board, "Background", new Vector3(0f, 0.08f, 0.01f),
                                  new Vector2(0.7f, 0.4f), boardColor);

        progressText = MakeText("Progress", new Vector3(0f, 0.24f, 0f), new Vector2(0.6f, 0.03f), 0.15f);
        questionText = MakeText("Question", new Vector3(0f, 0.14f, 0f), new Vector2(0.6f, 0.12f), 0.3f);
        lowText = MakeText("LowLabel", Vector3.zero, new Vector2(0.14f, 0.04f), 0.15f);
        highText = MakeText("HighLabel", Vector3.zero, new Vector2(0.14f, 0.04f), 0.15f);

        if (questions.Count == 0)
        {
            Debug.LogWarning("SurveyManager has no questions.");
            Finish();
            return;
        }
        ShowQuestion(0);
    }

    void ShowQuestion(int i)
    {
        index = i;
        var q = questions[i];

        foreach (var b in buttons) Destroy(b.gameObject);
        buttons.Clear();

        progressText.text = $"Question {i + 1} of {questions.Count}";
        questionText.text = q.text;

        float half = (q.points - 1) * 0.5f;
        for (int k = 0; k < q.points; k++)
        {
            int value = k + 1;
            var b = PokeButton.Create(board, value.ToString(), value,
                                      new Vector2(buttonSize, buttonSize), buttonColor);
            b.transform.localPosition = new Vector3((k - half) * buttonSpacing, 0f, 0f);
            b.Pressed += OnAnswer;
            buttons.Add(b);
        }

        float edge = half * buttonSpacing;
        lowText.text = q.lowLabel;
        highText.text = q.highLabel;
        lowText.transform.localPosition = new Vector3(-edge, -0.055f, 0f);
        highText.transform.localPosition = new Vector3(edge, -0.055f, 0f);

        answering = false;
        shownAt = Time.time;
    }

    void OnAnswer(PokeButton b)
    {
        if (answering) return;
        answering = true;

        var q = questions[index];
        b.SetColor(selectedColor);
        foreach (var other in buttons) other.Interactable = false;
        Log("survey_answer", $"id={q.id};answer={b.Value};points={q.points};rt={F(Time.time - shownAt)}");

        StartCoroutine(Next());
    }

    IEnumerator Next()
    {
        yield return new WaitForSeconds(confirmDelay);

        if (index + 1 < questions.Count)
        {
            ShowQuestion(index + 1);
            yield break;
        }

        foreach (var b in buttons) Destroy(b.gameObject);
        buttons.Clear();
        progressText.text = "";
        lowText.text = "";
        highText.text = "";
        questionText.text = "Thank you!";
        yield return new WaitForSeconds(1f);
        Finish();
    }

    // White text that shrinks automatically to fit its box.
    TextMeshPro MakeText(string name, Vector3 localPos, Vector2 size, float maxFontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(board, false);
        go.transform.localPosition = localPos;
        var t = go.AddComponent<TextMeshPro>();
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.sizeDelta = size;
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.01f;
        t.fontSizeMax = maxFontSize;
        return t;
    }

    [ContextMenu("Debug: answer current question")]
    void DebugAnswer()
    {
        if (buttons.Count > 0) buttons[buttons.Count / 2].SimulatePress();
    }
}