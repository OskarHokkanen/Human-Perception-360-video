using UnityEngine;
using UnityEngine.Video;

// Goes on the Video Player in the 360 video scene.
// Mutes the video's own audio and keeps the picture in sync with
// the separate soundtrack played by StudyAudio.
//
// With "Feed Skybox Directly" on (recommended), the video's frames go straight
// to the skybox at the video's full resolution, so no Render Texture is needed
// and its size can't make the video blurry.
[RequireComponent(typeof(VideoPlayer))]
public class VideoSkyboxSync : MonoBehaviour
{
    [Tooltip("Send the video straight to the skybox at full resolution (no Render Texture).")]
    [SerializeField] bool feedSkyboxDirectly = true;

    VideoPlayer vp;

    public bool IsPrepared => vp.isPrepared;

    void Awake()
    {
        vp = GetComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.isLooping = true;
        vp.audioOutputMode = VideoAudioOutputMode.None;
        vp.timeReference = VideoTimeReference.ExternalTime;

        if (feedSkyboxDirectly)
        {
            vp.renderMode = VideoRenderMode.APIOnly;
            vp.prepareCompleted += AssignToSkybox;
        }
    }

    // Pass null to keep the clip already set on the Video Player.
    public void Prepare(VideoClip clip)
    {
        if (clip)
        {
            vp.source = VideoSource.VideoClip;
            vp.clip = clip;
        }
        vp.Prepare();
    }

    public void Play() => vp.Play();

    void AssignToSkybox(VideoPlayer player)
    {
        if (!RenderSettings.skybox)
        {
            Debug.LogError("VideoSkyboxSync: the video scene has no skybox material (Lighting > Environment).");
            return;
        }
        // Use a copy so the material asset in the project is not changed.
        var mat = new Material(RenderSettings.skybox);
        mat.SetTexture("_MainTex", player.texture);
        RenderSettings.skybox = mat;
        Debug.Log($"Video {player.width}x{player.height} sent to skybox.");
    }

    void Update()
    {
        if (vp.isPlaying && StudyAudio.Instance)
            vp.externalReferenceTime = StudyAudio.Instance.PlaybackTime;
    }
}