using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[Serializable]
public class Trial
{
    public string label = "Trial 1";
    [Tooltip("Leave empty to use the clip already set on the Video Player.")]
    public VideoClip video;
    [Tooltip("The soundtrack extracted from this video.")]
    public AudioClip audio;
    [Tooltip("How long the 360 video plays before switching to the game.")]
    public float videoSeconds = 60f;
    public string gameScene = "GameA";
    [Tooltip("Restart the soundtrack if it ends during the game.")]
    public bool loopAudio = true;
}

// Runs the study:
// 360 video -> fade -> game (soundtrack keeps playing) -> task done (soundtrack stops,
// "remove the headset" message) -> Continue -> fade -> next trial.
// Put it in the Bootstrap scene together with StudyAudio and ScreenFader.
public class StudyFlow : MonoBehaviour
{
    public static StudyFlow Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] string videoScene = "Video360";

    [Header("Player")]
    [Tooltip("The top-level object of the XR rig.")]
    [SerializeField] Transform xrRig;
    [Tooltip("Name of an empty GameObject in each scene marking where the player starts.")]
    [SerializeField] string spawnPointName = "PlayerSpawn";

    [Header("Timing")]
    [SerializeField] float fadeSeconds = 0.6f;
    [SerializeField] bool autoStart = true;

    [Header("Session")]
    [SerializeField] string participantId = "P01";
    [SerializeField] List<Trial> trials = new List<Trial>();

    public Trial CurrentTrial { get; private set; }
    public bool IsLastTrial => trialIndex == trials.Count - 1;

    int trialIndex = -1;
    bool phaseFinished;
    bool running;
    StreamWriter log;

    void Awake() => Instance = this;

    void Start()
    {
        if (autoStart) StartSession();
    }

    public void StartSession()
    {
        if (!running) StartCoroutine(RunSession());
    }

    // Called by the game when the participant presses Continue.
    public void FinishPhase() => phaseFinished = true;

    // Called by the game when the task is completed.
    public void TaskDone()
    {
        LogEvent("task_done");
        StudyAudio.Instance.FadeOutAndStop(fadeSeconds);
    }

    // Ends the current phase early. In the Editor, use the component's menu.
    [ContextMenu("Skip current phase")]
    public void Skip() => FinishPhase();

    IEnumerator RunSession()
    {
        running = true;
        OpenLog();
        LogEvent("session_start");

        for (trialIndex = 0; trialIndex < trials.Count; trialIndex++)
        {
            CurrentTrial = trials[trialIndex];
            LogEvent("trial_start");
            yield return RunTrial(CurrentTrial);
            LogEvent("trial_end");
        }

        LogEvent("session_end");
        CloseLog();
        running = false;
    }

    IEnumerator RunTrial(Trial t)
    {
        // 1. 360 video
        yield return LoadAndActivate(videoScene);
        PlaceRig();

        var video = FindFirstObjectByType<VideoSkyboxSync>();
        if (video)
        {
            video.Prepare(t.video);
            while (!video.IsPrepared) yield return null;
        }
        else
        {
            Debug.LogError("No VideoSkyboxSync found in scene " + videoScene);
        }

        StudyAudio.Instance.Play(t.audio, t.loopAudio);
        if (video) video.Play();
        phaseFinished = false;
        LogEvent("video_start");
        yield return ScreenFader.Instance.FadeTo(0f, fadeSeconds);
        yield return WaitForPhaseEnd(t.videoSeconds);

        // 2. Game. The soundtrack keeps playing until the task is done.
        yield return ScreenFader.Instance.FadeTo(1f, fadeSeconds);
        LogEvent("transition");
        yield return SceneManager.UnloadSceneAsync(videoScene);
        yield return EnterPhaseScene(t.gameScene);
        LogEvent("game_start");

        // Stays in the game until the participant presses Continue.
        yield return WaitForPhaseEnd(0f);
        LogEvent("game_end");

        // Last trial: keep the "remove the headset" message visible.
        if (IsLastTrial) yield break;

        if (StudyAudio.Instance.IsPlaying) StudyAudio.Instance.FadeOutAndStop(fadeSeconds);
        yield return ScreenFader.Instance.FadeTo(1f, fadeSeconds);
        yield return SceneManager.UnloadSceneAsync(t.gameScene);
    }

    // Loads the game scene, places the player, starts the game and fades in.
    IEnumerator EnterPhaseScene(string sceneName)
    {
        yield return LoadAndActivate(sceneName);
        PlaceRig();
        phaseFinished = false;
        var phase = FindFirstObjectByType<StudyPhase>();
        if (phase) phase.Begin();
        else Debug.LogWarning("No game script found in scene " + sceneName);
        yield return ScreenFader.Instance.FadeTo(0f, fadeSeconds);
    }

    IEnumerator LoadAndActivate(string sceneName)
    {
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
    }

    // Waits until FinishPhase() is called, or until maxSeconds (0 = no limit).
    IEnumerator WaitForPhaseEnd(float maxSeconds)
    {
        float end = Time.time + maxSeconds;
        while (!phaseFinished && (maxSeconds <= 0f || Time.time < end))
            yield return null;
        phaseFinished = false;
    }

    void PlaceRig()
    {
        if (!xrRig) return;
        var spawn = GameObject.Find(spawnPointName);
        if (!spawn) return;
        xrRig.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
    }

    // On Quest this usually fires when the headset is taken off / put back on.
    void OnApplicationPause(bool paused)
    {
        LogEvent(paused ? "app_paused" : "app_resumed");
    }

    // ---------- Logging (CSV in the app's data folder on the headset) ----------

    void OpenLog()
    {
        string file = $"{participantId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string path = Path.Combine(Application.persistentDataPath, file);
        log = new StreamWriter(path) { AutoFlush = true };
        log.WriteLine("time_utc,session_seconds,participant,trial_index,trial_label,event,audio_seconds,detail");
        Debug.Log("Study log: " + path);
    }

    public void LogEvent(string evt, string detail = "")
    {
        if (log == null)
        {
            Debug.Log($"[Study] {evt} {detail}");
            return;
        }

        var inv = CultureInfo.InvariantCulture;
        string audio = StudyAudio.Instance && StudyAudio.Instance.IsPlaying
            ? StudyAudio.Instance.PlaybackTime.ToString("F3", inv)
            : "";

        log.WriteLine(string.Join(",",
            DateTime.UtcNow.ToString("o", inv),
            Time.realtimeSinceStartup.ToString("F3", inv),
            Csv(participantId),
            trialIndex.ToString(inv),
            Csv(CurrentTrial != null ? CurrentTrial.label : ""),
            Csv(evt),
            audio,
            Csv(detail)));
    }

    static string Csv(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

    void CloseLog()
    {
        log?.Close();
        log = null;
    }

    void OnApplicationQuit()
    {
        LogEvent("app_quit");
        CloseLog();
    }
}