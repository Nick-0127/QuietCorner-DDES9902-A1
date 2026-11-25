#if UNITY_STANDALONE && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.XR;

// Offline assessment demonstration only. Never starts during normal play or WebGL.
// The film records real running scene frames; its actions are scripted, not user tests.
public sealed class QuietCornerDemoRecorder : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Begin()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-qc-record-demo") >= 0)
            new GameObject("Optional offline demonstration recorder").AddComponent<QuietCornerDemoRecorder>();
    }

    IEnumerator Start()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-qc-demo-output");
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Missing -qc-demo-output");
        string output = args[index + 1];
        Directory.CreateDirectory(output);
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        Time.captureFramerate = 15;
        yield return null;
        yield return null;

        var rig = FindFirstObjectByType<Chapter6XRPlayer>();
        var experience = rig.experience;
        var camera = rig.head;
        var browser = rig.GetComponent<QuietCornerBrowserControls>();
        if (browser != null) browser.enabled = false;
        rig.enabled = false;
        rig.simulator.enabled = false;
        experience.useDesktopInput = false;
        foreach (var driver in rig.GetComponentsInChildren<TrackedPoseDriver>()) driver.enabled = false;
        foreach (var ray in rig.GetComponentsInChildren<Chapter6XRRay>()) ray.gameObject.SetActive(false);
        // A hidden batch player has no presented back buffer. Render the real
        // game camera explicitly, including its existing subtitle canvas.
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = .3f;
        }
        var captureTexture = new RenderTexture(1280, 720, 24);
        var capturePixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        experience.ResetExperience();
        experience.Teleport(new Vector3(0f, .05f, -5f));
        rig.transform.rotation = Quaternion.identity;
        camera.transform.localPosition = new Vector3(0, 1.65f, 0);
        camera.transform.rotation = Quaternion.LookRotation(new Vector3(0, 1.45f, 1.5f) - camera.transform.position);

        var targets = FindObjectsByType<QuietCornerTarget>(FindObjectsSortMode.None);
        QuietCornerTarget Find(string action) => targets.First(t => t.action == action);
        var light = Find("light");
        var sound = Find("sound");
        var chair = FindObjectsByType<QuietCornerSeat>(FindObjectsSortMode.None).OrderBy(c => c.name).First();
        var chairTarget = chair.GetComponent<QuietCornerTarget>();
        var water = Find("water");
        var tea = Find("tea");
        var tray = targets.First(t => t.action == "takeDrink" && t.name.Contains("button"));
        var machine = FindFirstObjectByType<QuietCornerVendingMachine>();
        var sources = experience.GetComponents<AudioSource>();
        ExportAudio(sources.First(s => s.clip != null && s.clip.name.Contains("Warm")).clip, Path.Combine(output, "Warm_Current.wav"));
        ExportAudio(sources.First(s => s.clip != null && s.clip.name.Contains("Cool")).clip, Path.Combine(output, "Cool_Current.wav"));
        ExportAudio(Resources.Load<AudioClip>("Audio/GuideIn"), Path.Combine(output, "GuideIn_Current.wav"));
        ExportAudio(Resources.Load<AudioClip>("Audio/GuideOut"), Path.Combine(output, "GuideOut_Current.wav"));

        var events = new List<string> { "Scripted walkthrough of the running current Unity scene. Not a participant test or hardware XR test." };
        var failures = new List<string>();
        void Check(bool ok, string label) { events.Add((ok ? "PASS: " : "FAIL: ") + label); if (!ok) failures.Add(label); }
        void Log(float t, string action) => events.Add(t.ToString("F3", CultureInfo.InvariantCulture) + "s " + action +
            " | warm=" + experience.warm + " sound=" + experience.soundOn + " guide=" + experience.guideRunning +
            " seated=" + experience.seated + " drink=" + machine.readyDrink);

        using (var state = new StreamWriter(Path.Combine(output, "frame_states.csv")))
        {
            state.WriteLine("frame,time,warm,sound,guide,seated,guide_time,collected_drinks");
            for (int frame = 0; frame < 1200; frame++)
            {
                float t = frame / 15f;
                if (frame == 7 * 15) { experience.Activate("sound", Vector3.zero); Check(experience.soundOn, "Music enabled"); Log(t, "sound on"); }
                if (frame == 14 * 15) { experience.Activate("light", Vector3.zero); Check(!experience.warm, "Cool lighting selected"); Log(t, "cool light"); }
                if (frame == 24 * 15) { experience.Activate("light", Vector3.zero); Check(experience.warm, "Warm lighting restored"); Log(t, "warm light"); }
                if (frame == 30 * 15) { experience.Activate("pause", Vector3.zero); Check(experience.guideRunning, "Guided break started"); Log(t, "start guide"); }
                if (frame == 40 * 15) { experience.Activate("pause", Vector3.zero); Check(!experience.guideRunning, "Guided break stopped"); Log(t, "stop guide"); }
                if (frame == 41 * 15) { experience.ToggleSofaSeat(); Check(experience.seated && experience.guideRunning, "Sofa seats and starts guide"); Log(t, "sit on sofa"); }
                if (frame == 51 * 15)
                {
                    experience.StandUp();
                    Check(!experience.seated && !experience.guideRunning, "Leaving sofa ends guide");
                    experience.Teleport(chair.transform.position - chair.transform.forward * 1.1f + Vector3.up * .05f);
                    rig.transform.rotation = chair.transform.rotation;
                    camera.transform.localRotation = Quaternion.identity;
                    Log(t, "stand and move to table chair");
                }
                if (frame == 54 * 15) { experience.Activate("seat", Vector3.zero, chairTarget); Check(experience.currentSeat == chair && !experience.guideRunning, "Table chair seats without guide"); Log(t, "sit at table"); }
                if (frame == 60 * 15)
                {
                    experience.StandUp();
                    experience.Teleport(new Vector3(3.15f, .05f, -5.85f));
                    rig.transform.rotation = Quaternion.Euler(0, 90, 0);
                    camera.transform.localRotation = Quaternion.identity;
                    Log(t, "stand and move to vending machine");
                }
                if (frame == 62 * 15) { experience.Activate("water", Vector3.zero, water); Check(machine.dispensing, "Water preparation started"); Log(t, "select water"); }
                if (frame == 64 * 15) { Check(machine.readyDrink == "Water", "Water appears ready"); Log(t, "water ready"); }
                if (frame == 66 * 15) { experience.Activate("takeDrink", Vector3.zero, tray); Check(machine.collectedDrinks == 1, "Water collected"); Log(t, "collect water"); }
                if (frame == 68 * 15) { experience.Activate("tea", Vector3.zero, tea); Check(machine.dispensing, "Tea preparation started"); Log(t, "select tea"); }
                if (frame == 70 * 15) { Check(machine.readyDrink == "Tea", "Tea appears ready"); Log(t, "tea ready"); }
                if (frame == 72 * 15) { experience.Activate("takeDrink", Vector3.zero, tray); Check(machine.collectedDrinks == 2, "Tea collected"); Log(t, "collect tea"); }
                if (frame == 74 * 15)
                {
                    experience.Activate("stop", Vector3.zero);
                    Check(!experience.soundOn && !experience.guideRunning, "Stop ends music and guidance");
                    experience.Teleport(new Vector3(0f, .05f, -5f));
                    rig.transform.rotation = Quaternion.identity;
                    Log(t, "stop all and return to overview");
                }
                if (frame == 76 * 15) { experience.Activate("reset", Vector3.zero); Check(experience.warm && !experience.soundOn && !experience.guideRunning, "Reset gives warm silent stopped state"); Log(t, "reset"); }

                if (!experience.seated)
                {
                    Vector3 look = t < 7 || t >= 74 ? new Vector3(0, 1.45f, 1.5f)
                        : t < 14 ? sound.transform.position
                        : t < 30 ? light.transform.position
                        : t < 41 ? experience.orb.position
                        : t < 60 ? chair.transform.position + Vector3.up * .5f
                        : t < 66 ? water.transform.position
                        : t < 68 ? tray.transform.position
                        : t < 72 ? tea.transform.position : tray.transform.position;
                    var desired = Quaternion.LookRotation(look - camera.transform.position);
                    camera.transform.rotation = Quaternion.Slerp(camera.transform.rotation, desired, .18f);
                }
                state.WriteLine(FormattableString.Invariant($"{frame},{t:F3},{experience.warm},{experience.soundOn},{experience.guideRunning},{experience.seated},{experience.guideTime:F3},{machine.collectedDrinks}"));
                yield return new WaitForEndOfFrame();
                var previousTarget = camera.targetTexture;
                var previousActive = RenderTexture.active;
                camera.targetTexture = captureTexture;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = captureTexture;
                capturePixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                capturePixels.Apply();
                File.WriteAllBytes(Path.Combine(output, $"frame_{frame:D5}.jpg"), capturePixels.EncodeToJPG(91));
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
            }
        }
        File.WriteAllLines(Path.Combine(output, "Demo_Interaction_Log.txt"), events);
        captureTexture.Release();
        Destroy(captureTexture);
        Destroy(capturePixels);
        Time.captureFramerate = 0;
        Debug.Log("QUIET_CORNER_DEMO_DONE frames=1200 failures=" + failures.Count);
        Application.Quit(failures.Count == 0 ? 0 : 1);
    }

    static void ExportAudio(AudioClip clip, string path)
    {
        if (clip == null) throw new Exception("Missing demo audio " + path);
        clip.LoadAudioData();
        var data = new float[clip.samples * clip.channels];
        if (!clip.GetData(data, 0)) throw new Exception("Cannot read demo audio " + path);
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + data.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)clip.channels); writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2); writer.Write((short)(clip.channels * 2)); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(data.Length * 2);
            foreach (float sample in data) writer.Write((short)(Mathf.Clamp(sample, -1, 1) * 32767));
        }
    }
}
#endif
