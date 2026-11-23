# VR 360 Video Study Prototype

## Overview

The prototype plays a 360 video with its sound on a Meta Quest 3, then moves the participant into a number-pressing game while the video's sound keeps playing. After the game, the participant takes the headset off to answer the questionnaire outside VR, then puts it back on and starts the next trial.

Each trial runs like this:

1. The 360 video plays with its soundtrack for a set number of seconds.
2. The view fades to black and the number game appears. The soundtrack keeps playing.
3. The participant presses the numbers 1 to 10 in order. A wrong number turns red and can no longer be pressed.
4. When all numbers are done, the soundtrack fades out and the message "You can now remove the headset." appears.
5. The participant answers the questionnaire outside VR.
6. After 10 seconds the message is replaced by a Next trial button. When the participant is back in the headset, they press it and the next trial starts.

On the last trial, the participant sees a thank-you message instead of the button. Every step is written to a CSV log on the headset (see Getting the data).

## What you need

| What | Why | Where to get it |
| --- | --- | --- |
| Meta Quest 3 in developer mode, plus a USB-C cable | Running the prototype and copying the logs | Developer mode is turned on in the Meta Horizon app on your phone |
| Unity (the same version the project was made with) with Android Build Support | Opening, editing and building the project | Unity Hub; add Android Build Support as a module |
SideQuest | To get the data from the tests


## How the project is organised

The project has three scenes. MainScene is always loaded; the video scene and the game scene are loaded on top of it one at a time. Because the sound lives in MainScene, it keeps playing when the video scene is swapped for the game.

The sound plays from MainScene, so it carries on when Video360 is replaced by GameA.

| Scene | What it contains | Notes |
| --- | --- | --- |
| MainScene | The XR rig (head camera and controllers) and the Study object with `StudyFlow`, `StudyAudio` and `ScreenFader` | Must be first in the build scene list. This is where you add videos and set up trials |
| VideoScene | One Video Player with `VideoSkyboxSync`, the Render Texture and the skybox material | Shared by all trials; the trial tells it which video to play |
| GameA | The `NumberGame` object and an empty `PlayerSpawn` | The number game |

Never add a camera, XR rig or Audio Listener to Video360 or GameA. There must be only one, in MainScene.

| Script | What it does |
| --- | --- |
| `StudyFlow` | Runs the trials in order, handles the scene changes and writes the log. Holds the Trials list and the Participant ID |
| `StudyAudio` | Plays the video's sound separately from the video, so it can continue into the game |
| `VideoSkyboxSync` | Plays the 360 video without its own sound and keeps it in time with `StudyAudio` |
| `ScreenFader` + `ScreenFade.shader` | Fades the view to and from black between scenes |
| `NumberGame` | The number-pressing game and the end message with the Next trial button |
| `Presser` | The small yellow touch tip on each controller that presses buttons |
| `PokeButton`, `StudyPhase`, `StudyMaterials`, `UnlitColor.shader` | Helpers used by the game; you normally don't need to change them |

## Preparing a new 360 video

Every video must be a mono equirectangular 360 video at exactly 2:1, for example 4096 × 2048, encoded as H.264 MP4. Videos that play fine on your computer can show up black on the headset if they use another format or a higher resolution.

## Adding videos to Unity and creating trials

A trial is one row in the Trials list on `StudyFlow`. It pairs a video with its sound and says how long the video plays. Trials run in the order they are listed.

**Import the files**

1. Drag the MP4 into `Assets/Videos` and the WAV into `Assets/Audio` in Unity's Project window (create the folders if they don't exist).
2. Select the WAV and, in the Inspector, set Load Type to Streaming and Compression Format to Vorbis, then click Apply. Long sound files otherwise use too much memory on the headset.


**Add the trial**

1. Open the MainScene scene and select the Study object in the Hierarchy.
2. On `StudyFlow`, open Trials and click + to add a row.
3. Fill in the row:

| Field | What to enter |
| --- | --- |
| Label | A short name you will recognise in the log, e.g. `Square morning` |
| Video | The MP4 |
| Audio | The matching WAV |
| Video Seconds | How long the video plays before the game starts. |
| Game Scene | `GameA` |
| Loop Audio | On: the sound starts again if the game lasts longer than the sound |

4. Drag the rows by their handle to change the order. The last row is the one that ends with the thank-you message.
5. Save the scene (Ctrl/Cmd + S).

The Participant ID and the trial order are set here in the Inspector. To run a different order for another participant, change the list and the Participant ID, then build again.

## Running a session

**Test in the Editor first**

1. Open the MainScene scene and press Play. The session always starts from MainScene.
2. To jump ahead, click the ⋮ menu on the `StudyFlow` component and choose Skip current phase.
3. While the game is loaded, select the `NumberGame` object in the Hierarchy and use its ⋮ menu: press next correct number, press a wrong number, press Next trial.

Video playback in the Editor uses your computer's decoder. Always check new videos on the headset too.

**Build to the headset**

1. Set the Participant ID on `StudyFlow`. It becomes part of the log file name.
2. Open File → Build Profiles and select Android. The scene list must contain MainScene first, then VideoScene and GameA.
3. Connect the Quest with the USB cable. In the headset, allow USB debugging if asked.
4. Click Build And Run.
5. To start the app later without the computer, open the app library in the headset and filter on Unknown Sources.

## Adjusting the number game and end message

All settings are on the `NumberGame` component in the GameA scene. Distances are in metres. Values you change in the Inspector win over the defaults in the script.

| Setting | Default | What it changes |
| --- | --- | --- |
| Number Count | 10 | How many numbers to press |
| Columns | 5 | Numbers per row on the board |
| Wrong Lock Seconds | 0 | 0 = a wrong number stays red for the rest of the round and is skipped. A value such as 2 makes it pressable again after 2 seconds |
| End Delay | 1 | Seconds the finished board stays before the message appears |
| Layout Seed | 12345 | Where each number sits. Same value = same layout for everyone |
| Remove Headset Text | You can now remove the headset. | The message after the game |
| Last Trial Text | Thank you! You can now remove the headset. | The message after the last trial |
| Button Delay Seconds | 10 | Seconds before the message is replaced by the Next trial button |
| Next Trial Label | Next trial | The text on the button |
| Distance | 0.45 | How far in front of the eyes the board appears |
| Below Eyes | 0.2 | How far below eye height the board appears |
| Tile Size / Gap | 0.075 / 0.015 | Size of each number button and the space between them |

Two related settings live in MainScene. Fade Seconds on `StudyFlow` (default 0.6) sets the length of every fade. Tip Offset on each `Presser` moves the yellow touch tip; Z is forward from the controller, Y is up.

Change values when Play mode is stopped. Changes made while playing are thrown away when you stop.

## Getting the data
You can use SideQuest to get the data from the headset. Try this when you are doing you pilot runs, also explore what data you would want, if any. 

Export the data before you hand back the headset. Since you are multiple people sharing there might be conflicts in the data export and you would not want to have 10 participants data in the headset only to be overwritten.

**Columns**

| Column | Meaning |
| --- | --- |
| time\_utc | Date and time of the event (UTC) |
| session\_seconds | Seconds since the app started |
| participant | The Participant ID |
| trial\_index | 0 for the first trial, 1 for the second, and so on |
| trial\_label | The trial's Label |
| event | What happened (see below) |
| audio\_seconds | Position in the soundtrack when it happened; empty when no sound is playing |
| detail | Extra information, as `key=value` pairs separated by `;` |

**Events**

| Event | When | Detail |
| --- | --- | --- |
| session\_start / session\_end | Start and end of the session |  |
| trial\_start / trial\_end | Start and end of each trial |  |
| video\_start | The 360 video and its sound start |  |
| transition | The fade from video to game starts |  |
| game\_start | The game is visible |  |
| game\_layout | The board is created | count, seed |
| press | A number is pressed | pressed, expected, result (correct or wrong), t = seconds since the board appeared |
| skip | A locked number is skipped | number, t |
| game\_result | The last number is done | errors, skipped, time |
| task\_done | The sound fades out and the message appears |  |
| next\_button\_shown | The Next trial button appears |  |
| continue\_pressed | Next trial is pressed |  |
| game\_end | The game scene is about to close |  |
| app\_paused / app\_resumed | Usually the headset being taken off and put back on |  |

The time between task\_done and continue\_pressed is roughly how long the participant spent outside VR on the questionnaire. Use app\_paused and app\_resumed for a closer measure.

## Troubleshooting

| Problem | Likely cause | Fix |
| --- | --- | --- |
| Video works in the Editor but is black on the headset | The headset can't decode the video | Re-encode to H.264 at 4096 × 2048 (Preparing a new 360 video, step 2) |
| Video is stretched or warped at the top and bottom | The video isn't 2:1, or the Render Texture is a different size | Check the video is 2:1. Render Texture size = video size. Skybox material: Mapping Latitude Longitude Layout, Image Type 360 Degrees |
| Objects are pink on the headset | A shader was left out of the build | `Study/UnlitColor` must be in Project Settings → Graphics → Always Included Shaders. The fade material on `ScreenFader` must use `Study/ScreenFade` |
| No sound during the game | Video Seconds is longer than the sound and Loop Audio is off, or no WAV is set | Shorten Video Seconds or turn on Loop Audio. Check the trial's Audio field |
| Video and sound drift apart | The Video Player is playing its own sound or isn't synced | `VideoSkyboxSync` must be on the Video Player. Audio Output Mode should be None |
| Numbers don't react to touch | No `Presser` on the controllers, or the tip doesn't reach | Add `Presser` to both controller anchors in MainScene. Touch the numbers with the yellow ball |
| Numbers or text are missing | TextMeshPro resources aren't imported | Window → TextMeshPro → Import TMP Essential Resources |
| A scene fails to load | The scene isn't in the build list, or the name doesn't match | Add it in Build Profiles. Game Scene must be spelled exactly like the scene file |
| Warning about two Audio Listeners, or a strange view | A camera or rig in Video360 or GameA | Delete it. The only rig belongs in MainScene |
| No log file on the headset | The session never started, or you're looking in the wrong folder | Check the Package Name, and that the app ran past the first video |
