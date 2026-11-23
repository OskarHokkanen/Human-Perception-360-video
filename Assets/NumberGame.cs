using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Press the numbers in order on a flat board in front of the participant.
// A wrong press turns that number red and makes it inactive.
// When done: the soundtrack stops and a message asks the participant to remove
// the headset, with a Continue button that starts the next trial.
public class NumberGame : StudyPhase
{
    [Header("Task")]
    [SerializeField] int numberCount = 10;
    [SerializeField] int columns = 5;
    [Tooltip("Seconds a wrongly pressed number stays red. " +
             "0 = for the rest of the round; it is then skipped when its turn comes.")]
    [SerializeField] float wrongLockSeconds = 0f;
    [Tooltip("Seconds to show the finished board before the message appears.")]
    [SerializeField] float endDelay = 1f;
    [Tooltip("Decides where each number goes. Same value = same layout every time.")]
    [SerializeField] int layoutSeed = 12345;

    [Header("End message")]
    [TextArea] [SerializeField] string removeHeadsetText = "You can now remove the headset.";
    [TextArea] [SerializeField] string lastTrialText = "Thank you!\nYou can now remove the headset.";
    [Tooltip("Seconds the message is shown before it is replaced by the Next trial button.")]
    [SerializeField] float buttonDelaySeconds = 10f;
    [SerializeField] string nextTrialLabel = "Next trial";

    [Header("Placement (metres)")]
    [SerializeField] float distance = 0.45f;
    [SerializeField] float belowEyes = 0.2f;
    [SerializeField] float tileSize = 0.075f;
    [SerializeField] float gap = 0.015f;

    [Header("Colours")]
    [SerializeField] Color tileColor = new Color(0.95f, 0.95f, 0.95f);
    [SerializeField] Color correctColor = new Color(0.35f, 0.8f, 0.45f);
    [SerializeField] Color wrongColor = new Color(0.9f, 0.25f, 0.25f);
    [SerializeField] Color boardColor = new Color(0.15f, 0.15f, 0.17f);
    [SerializeField] Color continueColor = new Color(0.35f, 0.6f, 0.95f);

    Transform board;
    Transform background;
    readonly List<PokeButton> tiles = new List<PokeButton>();
    readonly HashSet<int> locked = new HashSet<int>();
    int expected = 1;
    int errors;
    int skipped;
    float startTime;
    bool done;
    bool showingMessage;

    float Elapsed => Time.time - startTime;

    protected override void OnBegin()
    {
        board = new GameObject("Board").transform;
        board.SetParent(transform, false);
        PlaceBoardInFrontOfHead();

        int rows = Mathf.CeilToInt(numberCount / (float)columns);
        float pitch = tileSize + gap;
        float width = columns * pitch + gap;
        float height = rows * pitch + gap;

        background = StudyMaterials.CreateQuad(board, "Background", new Vector3(0f, 0f, 0.01f),
                                               new Vector2(width, height), boardColor).transform;

        var numbers = FixedOrder();
        for (int i = 0; i < numberCount; i++)
        {
            int col = i % columns;
            int row = i / columns;
            float x = (col - (columns - 1) * 0.5f) * pitch;
            float y = ((rows - 1) * 0.5f - row) * pitch;

            int n = numbers[i];
            var b = PokeButton.Create(board, n.ToString(), n, new Vector2(tileSize, tileSize), tileColor);
            b.transform.localPosition = new Vector3(x, y, 0f);
            b.Pressed += OnPressed;
            tiles.Add(b);
        }

        startTime = Time.time;
        Log("game_layout", $"count={numberCount};seed={layoutSeed}");
    }

    void PlaceBoardInFrontOfHead()
    {
        GetHeadPose(out var eye, out var fwd);
        board.SetPositionAndRotation(eye + fwd * distance + Vector3.down * belowEyes,
                                     Quaternion.LookRotation(fwd));
    }

    void OnPressed(PokeButton b)
    {
        if (done) return;

        if (b.Value == expected)
        {
            b.SetColor(correctColor);
            b.Interactable = false;
            Log("press", $"pressed={b.Value};expected={expected};result=correct;t={F(Elapsed)}");
            expected++;
            AdvancePastLocked();
            CheckDone();
        }
        else
        {
            errors++;
            Log("press", $"pressed={b.Value};expected={expected};result=wrong;t={F(Elapsed)}");
            StartCoroutine(LockTile(b));
        }
    }

    IEnumerator LockTile(PokeButton b)
    {
        b.SetColor(wrongColor);
        b.Interactable = false;

        if (wrongLockSeconds <= 0f)
        {
            locked.Add(b.Value);
            yield break;
        }

        yield return new WaitForSeconds(wrongLockSeconds);
        if (!done)
        {
            b.SetColor(tileColor);
            b.Interactable = true;
        }
    }

    void AdvancePastLocked()
    {
        while (expected <= numberCount && locked.Contains(expected))
        {
            skipped++;
            Log("skip", $"number={expected};t={F(Elapsed)}");
            expected++;
        }
    }

    void CheckDone()
    {
        if (done || expected <= numberCount) return;
        done = true;
        Log("game_result", $"errors={errors};skipped={skipped};time={F(Elapsed)}");
        StartCoroutine(EndSequence());
    }

    IEnumerator EndSequence()
    {
        yield return new WaitForSeconds(endDelay);
        TaskDone(); // stops the soundtrack
        ShowEndMessage();
    }

    void ShowEndMessage()
    {
        foreach (var t in tiles) Destroy(t.gameObject);
        tiles.Clear();

        background.localScale = new Vector3(0.45f, 0.24f, 1f);
        showingMessage = true;
        bool last = IsLastTrial;

        var message = CreateText(board, "Message", last ? lastTrialText : removeHeadsetText,
                                 new Vector3(0f, 0f, -0.002f), new Vector2(0.4f, 0.16f), 0.3f, Color.white);

        if (last)
        {
            Finish(); // the message stays visible; the session ends here
            return;
        }

        StartCoroutine(ReplaceMessageWithButton(message.gameObject));
    }

    // After a delay, the message is replaced by the "Next trial" button.
    IEnumerator ReplaceMessageWithButton(GameObject message)
    {
        // Real time, so it keeps counting while the headset is off and the app is paused.
        yield return new WaitForSecondsRealtime(buttonDelaySeconds);

        Destroy(message);
        var next = PokeButton.Create(board, nextTrialLabel, 0, new Vector2(0.2f, 0.08f), continueColor);
        next.transform.localPosition = Vector3.zero;
        next.Pressed += OnContinue;
        Log("next_button_shown");
    }

    void OnContinue(PokeButton b)
    {
        b.Interactable = false;
        Log("continue_pressed");
        Finish();
    }

    // When the headset is put back on, move the message in front of the participant again.
    void OnApplicationPause(bool paused)
    {
        if (!paused && showingMessage && board) StartCoroutine(RecenterAfterResume());
    }

    IEnumerator RecenterAfterResume()
    {
        yield return null;
        yield return null; // give tracking a moment to update
        PlaceBoardInFrontOfHead();
    }

    // Shuffled numbers, identical every run for the same seed.
    List<int> FixedOrder()
    {
        var rng = new System.Random(layoutSeed);
        var list = new List<int>();
        for (int i = 1; i <= numberCount; i++) list.Add(i);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    // ---------- Editor testing ----------

    [ContextMenu("Debug: press next correct number")]
    void DebugPressCorrect()
    {
        var b = tiles.Find(t => t.Value == expected);
        if (b) b.SimulatePress();
    }

    [ContextMenu("Debug: press a wrong number")]
    void DebugPressWrong()
    {
        var b = tiles.Find(t => t.Value > expected && t.Interactable);
        if (b) b.SimulatePress();
    }

    [ContextMenu("Debug: press Next trial")]
    void DebugContinue()
    {
        if (showingMessage) Finish();
    }
}