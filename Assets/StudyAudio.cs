using System.Collections;
using UnityEngine;

// Plays the extracted soundtrack of the 360 video.
// Lives in the Bootstrap scene, so it keeps playing while the
// video and game scenes are loaded and unloaded.
[RequireComponent(typeof(AudioSource))]
public class StudyAudio : MonoBehaviour
{
    public static StudyAudio Instance { get; private set; }

    [SerializeField, Range(0f, 1f)] float volume = 1f;

    AudioSource source;
    Coroutine fade;

    // Current playback position in seconds. The 360 video follows this clock.
    public double PlaybackTime =>
        source.clip ? (double)source.timeSamples / source.clip.frequency : 0d;

    public bool IsPlaying => source.isPlaying;

    void Awake()
    {
        Instance = this;
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
    }

    public void Play(AudioClip clip, bool loop)
    {
        if (fade != null) { StopCoroutine(fade); fade = null; }
        source.Stop();
        source.clip = clip;
        source.loop = loop;
        source.volume = volume;
        source.Play();
    }

    public void FadeOutAndStop(float seconds)
    {
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(FadeOutRoutine(seconds));
    }

    IEnumerator FadeOutRoutine(float seconds)
    {
        float start = source.volume;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(start, 0f, t / seconds);
            yield return null;
        }
        source.Stop();
        source.volume = volume;
        fade = null;
    }
}