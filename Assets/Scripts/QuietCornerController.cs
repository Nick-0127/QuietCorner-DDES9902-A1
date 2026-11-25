using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class QuietCornerController : MonoBehaviour
{
    public bool useDesktopInput = true;
    public Camera viewCamera;
    public CharacterController player;
    public Transform orb;
    public Light sun, orbLight;
    public Light[] lamps;
    public Renderer[] lampSurfaces;
    public TextMesh lightStatus, soundStatus, pauseStatus, orbStatus;
    public float walkingSpeed = 1.6f;
    public float sprintMultiplier = 1.8f;
    public float MovementSpeed(bool sprinting) => walkingSpeed * (sprinting ? sprintMultiplier : 1f);
    public bool warm = true, soundOn, guideRunning;
    public int completedCycles;
    public float guideTime;

    private AudioSource ambience;
    private AudioSource coolMusic;
    private AudioSource guideVoice;
    private AudioClip guideIn;
    private AudioClip guideOut;
    private float yaw, pitch, moodBlend = 1f;
    private float musicLevel;
    private const float MusicVolume = .50f;
    private const float MusicUnderVoiceVolume = .26f;
    private float baseOrbSize = .72f;
    private Material orbMaterial;
    private string lastPhase = "";
    private int lastGuideCue = -1;
    private Vector3 standingPosition;
    private Quaternion standingRotation;
    private Vector3 standingCameraPosition;
    private Quaternion standingCameraRotation;
    private Vector3 standingBodyCenter;
    private float standingBodyHeight;
    public bool seated;
    public QuietCornerSeat currentSeat { get; private set; }
    private string notice;
    private float noticeUntil;
    public void ShowNotice(string message) { notice = message; noticeUntil = Time.unscaledTime + 3f; }
    private Text subtitle;
    private GameObject subtitlePanel;
    private bool verifying;
    private int errorCount;
    private readonly List<string> checks = new List<string>();

    void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
        ambience = gameObject.AddComponent<AudioSource>();
        ambience.playOnAwake = false;
        ambience.loop = true;
        ambience.spatialBlend = 0f;
        ambience.volume = 0f;
        ambience.clip = MakeMusicClip(true);
        coolMusic = gameObject.AddComponent<AudioSource>();
        coolMusic.playOnAwake = false;
        coolMusic.loop = true;
        coolMusic.spatialBlend = 0f;
        coolMusic.volume = 0f;
        coolMusic.clip = MakeMusicClip(false);
        guideVoice = gameObject.AddComponent<AudioSource>();
        guideVoice.playOnAwake = false;
        guideVoice.loop = false;
        guideVoice.spatialBlend = 0f;
        guideIn = Resources.Load<AudioClip>("Audio/GuideIn");
        guideOut = Resources.Load<AudioClip>("Audio/GuideOut");
        guideIn?.LoadAudioData();
        guideOut?.LoadAudioData();
        orbMaterial = orb.GetComponent<Renderer>().material;
        Application.logMessageReceived += CountErrors;
        ResetExperience();
        if (player.GetComponent<Chapter6XRPlayer>()?.IsSimulation == true || useDesktopInput)
            CreateSubtitles();
        string[] args = Environment.GetCommandLineArgs();
        verifying = Array.IndexOf(args, "-qc-verify") >= 0;
        if (verifying) StartCoroutine(VerifyAndCapture(args));
    }

    void OnDestroy() { Application.logMessageReceived -= CountErrors; }
    void CountErrors(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errorCount++;
    }

    // Warm: low, rounded chords with a slow pluck. Cool: higher, clear bells.
    // Both loops have matched RMS and generous headroom for spoken guidance.
    AudioClip MakeMusicClip(bool warmTrack)
    {
        const int rate = 22050;
        const int seconds = 24;
        float[] samples = new float[rate * seconds];
        float[][] chords = warmTrack
            ? new[] {
                new[] { 130.81f, 164.81f, 196.00f },
                new[] { 174.61f, 220.00f, 261.63f },
                new[] { 146.83f, 174.61f, 220.00f },
                new[] { 196.00f, 246.94f, 293.66f }
            }
            : new[] {
                new[] { 220.00f, 329.63f, 440.00f },
                new[] { 196.00f, 293.66f, 392.00f },
                new[] { 174.61f, 261.63f, 349.23f },
                new[] { 196.00f, 293.66f, 392.00f }
            };
        float noteLength = warmTrack ? 2f : 1f;
        double energy = 0;
        float peak = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / rate;
            float inChord = t % 6f;
            float[] chord = chords[Mathf.FloorToInt(t / 6f) % chords.Length];
            int note = Mathf.FloorToInt(inChord / noteLength) % chord.Length;
            float noteTime = inChord % noteLength;
            float tone;
            if (warmTrack)
            {
                float pad = Mathf.Sin(2f * Mathf.PI * chord[0] * t) * .34f
                    + Mathf.Sin(2f * Mathf.PI * chord[1] * t) * .24f
                    + Mathf.Sin(2f * Mathf.PI * chord[2] * t) * .20f
                    + Mathf.Sin(Mathf.PI * chord[0] * t) * .12f;
                float pluckPhase = 2f * Mathf.PI * chord[note] * noteTime;
                float pluck = (Mathf.Sin(pluckPhase) + .16f * Mathf.Sin(pluckPhase * 2f))
                    * Mathf.Exp(-noteTime * 2f) * .32f;
                float envelope = Mathf.Min(1f, noteTime / .04f)
                    * Mathf.Min(1f, (noteLength - noteTime) / .1f);
                tone = pad + pluck * envelope;
            }
            else
            {
                float pad = Mathf.Sin(2f * Mathf.PI * chord[1] * t) * .26f
                    + Mathf.Sin(2f * Mathf.PI * chord[2] * t) * .18f;
                float bellPhase = 2f * Mathf.PI * chord[note] * 2f * noteTime;
                float bell = (Mathf.Sin(bellPhase) + .22f * Mathf.Sin(bellPhase * 2.76f)
                    + .10f * Mathf.Sin(bellPhase * 4.05f)) * Mathf.Exp(-noteTime * 3.2f) * .42f;
                float envelope = Mathf.Min(1f, noteTime / .015f)
                    * Mathf.Min(1f, (noteLength - noteTime) / .1f);
                tone = pad + bell * envelope;
            }
            float chordFade = Mathf.Min(1f, Mathf.Min(inChord, 6f-inChord) / .25f);
            samples[i] = tone * chordFade;
            energy += samples[i] * samples[i];
            peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
        }
        // Normalise average level without clipping transient bell notes.
        float rms = (float)Math.Sqrt(energy / samples.Length);
        float gain = Mathf.Min(.11f / Mathf.Max(rms, .0001f), .45f / Mathf.Max(peak, .0001f));
        for (int i = 0; i < samples.Length; i++) samples[i] *= gain;
        var clip = AudioClip.Create(warmTrack ? "Quiet Corner Warm Chords" : "Quiet Corner Cool Bells", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void Update()
    {
        if (!verifying && useDesktopInput) ReadInput();
        moodBlend = Mathf.MoveTowards(moodBlend, warm ? 1f : 0f, Time.deltaTime * .65f);
        ApplyMood();
        float targetMusicLevel = soundOn ? (guideVoice.isPlaying ? MusicUnderVoiceVolume : MusicVolume) : 0f;
        // One envelope keeps the total gain bounded during lighting changes.
        // Duck quickly for speech, then restore the music gently between cues.
        musicLevel = Mathf.MoveTowards(musicLevel, targetMusicLevel,
            Time.deltaTime * (targetMusicLevel < musicLevel ? 1.5f : .5f));
        ambience.volume = musicLevel * moodBlend;
        coolMusic.volume = musicLevel * (1f - moodBlend);
        if (soundOn)
        {
            if (!ambience.isPlaying) ambience.Play();
            if (!coolMusic.isPlaying) coolMusic.Play();
        }
        else
        {
            if (ambience.volume <= .001f && ambience.isPlaying) ambience.Stop();
            if (coolMusic.volume <= .001f && coolMusic.isPlaying) coolMusic.Stop();
        }
        if (guideRunning)
        {
            guideTime += Time.deltaTime;
            completedCycles = Mathf.FloorToInt(guideTime / 8f);
            if (guideTime >= 48f)
            {
                guideRunning = false;
                guideTime = 48f;
                completedCycles = 6;
                SetPhase("GUIDED BREAK COMPLETE");
                guideVoice.Stop();
                pauseStatus.text = "START GUIDED BREAK\n48 seconds";
            }
            else
            {
                float phase = guideTime % 8f;
                float size = .72f + .28f * (.5f - .5f * Mathf.Cos(phase / 8f * Mathf.PI * 2));
                orb.localScale = Vector3.one * size;
                int cue = completedCycles * 2 + (phase < 4f ? 0 : 1);
                if (cue != lastGuideCue)
                {
                    lastGuideCue = cue;
                    AudioClip line = phase < 4f ? guideIn : guideOut;
                    if (line != null)
                    {
                        // Use a real source clip so WebGL has its sample rate
                        // before initializing playback and loop boundaries.
                        guideVoice.Stop();
                        guideVoice.clip = line;
                        guideVoice.volume = .8f;
                        guideVoice.Play();
                    }
                    SetPhase((phase < 4f ? "BREATHE IN GENTLY" : "BREATHE OUT SLOWLY") + "\n" + (completedCycles + 1) + " / 6");
                }
            }
        }
        else orb.localScale = Vector3.Lerp(orb.localScale, Vector3.one * baseOrbSize, Time.deltaTime * 3f);
        if (subtitlePanel != null)
        {
            bool showingNotice = Time.unscaledTime < noticeUntil;
            subtitlePanel.SetActive(guideRunning || seated || showingNotice);
            subtitle.text = showingNotice ? notice : (guideRunning ? orbStatus.text.Replace("\n", "   ") : "Take your time.")
                + (seated ? "   |   E / F - stand up" : "   |   Esc - stop");
        }
    }

    void CreateSubtitles()
    {
        var canvasObject = new GameObject("Guidance subtitles", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        subtitlePanel = new GameObject("Caption background", typeof(RectTransform), typeof(Image));
        subtitlePanel.transform.SetParent(canvasObject.transform, false);
        var rect = subtitlePanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.1f, 0);
        rect.anchorMax = new Vector2(.9f, 0);
        rect.pivot = new Vector2(.5f, 0);
        rect.anchoredPosition = new Vector2(0, 24);
        rect.sizeDelta = new Vector2(0, 50);
        var background = subtitlePanel.GetComponent<Image>();
        background.color = new Color(.05f,.08f,.08f,.85f);
        background.raycastTarget = false;
        var label = new GameObject("Caption", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(subtitlePanel.transform, false);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12,4); labelRect.offsetMax = new Vector2(-12,-4);
        subtitle = label.GetComponent<Text>();
        subtitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subtitle.fontSize = 22;
        subtitle.alignment = TextAnchor.MiddleCenter;
        subtitle.color = Color.white;
        subtitle.raycastTarget = false;
        subtitlePanel.SetActive(false);
    }

    void SetPhase(string text)
    {
        if (text == lastPhase) return;
        orbStatus.text = text;
        lastPhase = text;
    }

    void ApplyMood()
    {
        Color glow = Color.Lerp(new Color(.43f,.8f,.9f), new Color(1f,.69f,.34f), moodBlend);
        sun.color = Color.Lerp(new Color(.75f,.88f,1f), new Color(1f,.83f,.65f), moodBlend);
        sun.intensity = Mathf.Lerp(.85f, 1.15f, moodBlend);
        RenderSettings.ambientLight = Color.Lerp(new Color(.37f,.47f,.53f),new Color(.52f,.47f,.38f),moodBlend);
        viewCamera.backgroundColor = Color.Lerp(new Color(.55f,.72f,.8f),new Color(.84f,.75f,.61f),moodBlend);
        foreach (Light lamp in lamps) { lamp.color = glow; lamp.intensity = .85f; }
        foreach (Renderer surface in lampSurfaces)
        {
            surface.material.color = glow;
            surface.material.SetColor("_EmissionColor",glow*.7f);
        }
        orbMaterial.color = glow;
        orbMaterial.SetColor("_EmissionColor",glow*.5f);
        orbLight.color = glow;
    }

    void ReadInput()
    {
        // Looking is always available, including during the visual guide.
        bool looking = Input.GetMouseButton(1);
        Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !looking;
        if (looking)
        {
            yaw += Input.GetAxisRaw("Mouse X") * 2f;
            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * 2f, -70, 70);
        }
        if (Input.GetKeyDown(KeyCode.Q)) yaw -= 30f;
        if (Input.GetKeyDown(KeyCode.E) && !TryToggleChairSeat()) yaw += 30f;
        player.transform.rotation = Quaternion.Euler(0,yaw,0);
        viewCamera.transform.localRotation = Quaternion.Euler(pitch,0,0);
        Vector3 direction = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) direction += player.transform.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) direction -= player.transform.forward;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) direction += player.transform.right;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) direction -= player.transform.right;
        if (!seated) player.Move(direction.normalized * MovementSpeed(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) * Time.deltaTime + Vector3.down * 2f * Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.F)) ToggleNearbySeat();
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = looking ? viewCamera.ViewportPointToRay(new Vector3(.5f,.5f,0)) : viewCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 25f))
            {
                var target = hit.collider.GetComponentInParent<QuietCornerTarget>();
                if (target != null) Activate(target.action,target.destination,target);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha1)) Activate("light",Vector3.zero);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Activate("sound",Vector3.zero);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Activate("pause",Vector3.zero);
        if (Input.GetKeyDown(KeyCode.R)) ResetExperience();
        if (Input.GetKeyDown(KeyCode.Home)) Teleport(new Vector3(0,.05f,-6.3f));
        if (Input.GetKeyDown(KeyCode.Escape)) StopExperience();
    }

    public void Activate(string action, Vector3 destination, QuietCornerTarget source = null)
    {
        if (source != null && (action == "seat" || action == "water" || action == "tea" || action == "takeDrink")
            && Vector3.Distance(viewCamera.transform.position, source.transform.position) > 3.2f)
        {
            ShowNotice("Move closer to use this.");
            return;
        }
        switch (action)
        {
            case "light":
                warm = !warm;
                lightStatus.text = warm ? "WARM LIGHT" : "COOL LIGHT";
                soundStatus.text = soundOn ? (warm ? "WARM MUSIC ON" : "COOL MUSIC ON") : "MUSIC OFF";
                break;
            case "sound":
                soundOn = !soundOn;
                if (soundOn && !ambience.isPlaying) ambience.Play();
                if (soundOn && !coolMusic.isPlaying) coolMusic.Play();
                soundStatus.text = soundOn ? (warm ? "WARM MUSIC ON" : "COOL MUSIC ON") : "MUSIC OFF";
                break;
            case "pause":
                if(guideRunning) StopGuide();
                else StartGuide();
                break;
            case "sofa": ToggleSofaSeat(); break;
            case "seat":
                var chair = source != null ? source.GetComponentInParent<QuietCornerSeat>() : null;
                if (chair != null) SitAtTable(chair);
                break;
            case "water": case "tea":
                source?.GetComponentInParent<QuietCornerVendingMachine>()?.SelectDrink(action);
                break;
            case "takeDrink": source?.GetComponentInParent<QuietCornerVendingMachine>()?.TakeDrink(); break;
            case "stop": StopExperience(); break;
            case "reset": ResetExperience(); break;
            case "teleport": Teleport(destination); break;
        }
    }

    public void Teleport(Vector3 destination)
    {
        if (seated) ToggleSofaSeat();
        player.enabled=false;
        player.transform.position=destination;
        player.enabled=true;
    }

    void StopGuide()
    {
        guideRunning=false;
        guideTime=0;
        completedCycles=0;
        lastGuideCue = -1;
        guideVoice.Stop();
        SetPhase("GUIDE OPTIONAL");
        pauseStatus.text="START GUIDED BREAK\n48 seconds";
    }

    void StartGuide()
    {
        guideRunning = true;
        guideTime = 0;
        completedCycles = 0;
        lastGuideCue = -1;
        pauseStatus.text = "STOP GUIDED BREAK";
    }

    public void ToggleSofaSeat()
    {
        if (player == null || viewCamera == null) return;
        if (seated) { StandUp(); return; }
        EnterSeat(new Vector3(0f,.05f,3.2f),Quaternion.Euler(0f,180f,0f),-9f);
        StartGuide();
    }

    public void ToggleNearbySeat()
    {
        if (!TryToggleChairSeat()) ToggleSofaSeat();
    }

    // E is a chair interaction nearby; callers only turn when no seat is available.
    public bool TryToggleChairSeat()
    {
        if (seated) { StandUp(); return true; }
        QuietCornerSeat nearest = null;
        float nearestDistance = 1.8f;
        foreach (var chair in FindObjectsByType<QuietCornerSeat>(FindObjectsSortMode.None))
        {
            float distance = Vector3.Distance(player.transform.position,chair.transform.position);
            if (distance < nearestDistance) { nearest = chair; nearestDistance = distance; }
        }
        if (nearest == null) return false;
        SitAtTable(nearest);
        return true;
    }

    public void SitAtTable(QuietCornerSeat chair)
    {
        if (currentSeat == chair && seated) { StandUp(); return; }
        if (seated) StandUp();
        EnterSeat(chair.SittingPosition,chair.transform.rotation,12f);
        currentSeat = chair;
        StopGuide();
    }

    void EnterSeat(Vector3 position,Quaternion rotation,float lookPitch)
    {
            standingPosition = player.transform.position;
            standingRotation = player.transform.rotation;
            standingBodyHeight = player.height;
            standingBodyCenter = player.center;
            standingCameraPosition = viewCamera.transform.localPosition;
            standingCameraRotation = viewCamera.transform.localRotation;
            player.enabled = false;
            player.height = 1.1f;
            player.center = new Vector3(player.center.x, .55f, player.center.z);
            player.transform.SetPositionAndRotation(position,rotation);
            viewCamera.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            viewCamera.transform.localRotation = Quaternion.Euler(lookPitch,0f,0f);
            player.enabled = true;
            seated = true;
            noticeUntil = 0;
            SyncSeatView();
    }

    public void StandUp()
    {
            if (!seated) return;
            player.enabled = false;
            player.height = standingBodyHeight;
            player.center = standingBodyCenter;
            player.transform.SetPositionAndRotation(standingPosition, standingRotation);
            viewCamera.transform.localPosition = standingCameraPosition;
            viewCamera.transform.localRotation = standingCameraRotation;
            player.enabled = true;
            seated = false;
            currentSeat = null;
            StopGuide();
            SyncSeatView();
    }

    void SyncSeatView()
    {
        yaw = player.transform.eulerAngles.y;
        pitch = Mathf.DeltaAngle(0f, viewCamera.transform.localEulerAngles.x);
        var simulator = player.GetComponent<Chapter6XRSimulator>();
        if (simulator != null && simulator.enabled)
            simulator.SetHeadPose(viewCamera.transform.localPosition, viewCamera.transform.localRotation);
        player.GetComponent<QuietCornerBrowserControls>()?.SyncView();
    }

    public void StopExperience()
    {
        StopGuide(); soundOn=false; musicLevel=0; ambience.Stop(); ambience.volume=0; coolMusic.Stop(); coolMusic.volume=0;
        soundStatus.text="MUSIC OFF";
        Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
    }

    public void ResetExperience()
    {
        StopExperience(); warm=true; lightStatus.text="WARM LIGHT";
        // Reset never forces the user's position or view direction.
    }

    void OnApplicationFocus(bool focused) { if(!focused && !verifying) StopExperience(); }

    void Check(bool condition,string label)
    {
        checks.Add((condition ? "PASS: " : "FAIL: ")+label);
        if(!condition) Debug.LogError("Verification failed: "+label);
    }

    IEnumerator Capture(string path)
    {
        yield return new WaitForEndOfFrame();
        // Render the actual active scene even if Windows has occluded the test window.
        var texture = new RenderTexture(1600,900,24);
        var previous = RenderTexture.active;
        var previousTarget = viewCamera.targetTexture;
        viewCamera.targetTexture=texture;
        viewCamera.Render();
        RenderTexture.active=texture;
        var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
        pixels.ReadPixels(new Rect(0,0,1600,900),0,0);
        pixels.Apply();
        File.WriteAllBytes(path,pixels.EncodeToPNG());
        viewCamera.targetTexture=previousTarget;
        RenderTexture.active=previous;
        Destroy(pixels);
        texture.Release();
        Destroy(texture);
    }

    IEnumerator VerifyAndCapture(string[] args)
    {
        int index=Array.IndexOf(args,"-qc-output");
        string output=index>=0 && index+1<args.Length ? args[index+1] : Application.persistentDataPath;
        Directory.CreateDirectory(output);
        yield return new WaitForSeconds(2f);
        Check(warm && !soundOn && !guideRunning,"Safe initial state: warm, silent, guide stopped");
        Check(guideIn != null && guideOut != null,"Spoken breathing cues are loaded for the captioned guide");
        Check(Physics.Raycast(viewCamera.transform.position,(lightStatus.transform.position-viewCamera.transform.position).normalized,out var rayHit,20f) && rayHit.collider.GetComponent<QuietCornerTarget>() != null,"Physical lighting button is reachable by pointer ray");
        yield return Capture(Path.Combine(output,"01_Warm_Overview.png"));
        Activate("sound",Vector3.zero);
        yield return new WaitForSeconds(.3f);
        Check(soundOn && ambience.volume > coolMusic.volume && ambience.clip != coolMusic.clip,"Warm lighting selects its own music loop");
        Activate("light",Vector3.zero);
        float transitionEnd = Time.time + 2f;
        bool boundedMix = true;
        while (Time.time < transitionEnd)
        {
            boundedMix &= ambience.volume + coolMusic.volume <= MusicVolume + .001f;
            yield return null;
        }
        Check(boundedMix,"Lighting crossfade never exceeds the music gain limit");
        Check(!warm && soundOn && coolMusic.isPlaying && coolMusic.volume > ambience.volume && soundStatus.text == "COOL MUSIC ON","Cool lighting fades to its matching music loop");
        Check(Mathf.Abs(coolMusic.volume - MusicVolume) < .001f,"Music reaches the increased listening level");
        yield return Capture(Path.Combine(output,"02_Cool_Sound_On.png"));
        Activate("pause",Vector3.zero);
        yield return new WaitForSeconds(.2f);
        Check(guideRunning && guideVoice.isPlaying && orbStatus.text.StartsWith("BREATHE IN GENTLY"),"Guided break speaks and captions its breathing cue");
        Check(coolMusic.volume <= MusicUnderVoiceVolume + .02f,"Music ducks beneath spoken guidance");
        yield return new WaitForSeconds(2f);
        Check(guideRunning && guideTime>1 && orb.localScale.x>.8f,"Pause guide animates orb and advances time");
        yield return Capture(Path.Combine(output,"03_Visual_Pause.png"));
        Activate("pause",Vector3.zero);
        Check(!guideRunning && guideTime==0,"Pause control cancels active guide");
        yield return new WaitForSeconds(.6f);
        Check(Mathf.Abs(coolMusic.volume - MusicVolume) < .001f,"Music returns smoothly after voice guidance stops");
        Activate("pause",Vector3.zero);
        guideTime=47.9f;
        yield return new WaitForSeconds(.3f);
        Check(!guideRunning && completedCycles==6,"Guide completes after six cycles");
        Activate("stop",Vector3.zero);
        Check(!guideRunning && !soundOn && !ambience.isPlaying && !coolMusic.isPlaying && !guideVoice.isPlaying,"Stop immediately silences music and voice guidance");
        yield return null;
        Check(ambience.volume == 0f && coolMusic.volume == 0f,"Stop clears the music envelope on subsequent frames");
        Vector3 prior=player.transform.position;
        var targets=FindObjectsByType<QuietCornerTarget>(FindObjectsSortMode.None);
        var pad=Array.Find(targets,t=>t.action=="teleport");
        Activate("teleport",pad.destination);
        Check(Vector3.Distance(player.transform.position,pad.destination)<.05f,"Floor destination moves the player");
        Teleport(prior);
        player.Move(Vector3.forward*.5f);
        Check(Vector3.Distance(player.transform.position,prior)>.3f,"Character controller can move through the room");
        Teleport(prior);
        player.transform.rotation=Quaternion.Euler(0,20,0);
        Check(Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y,20))<.1f,"View can freely rotate");
        player.transform.rotation=Quaternion.identity;
        ToggleSofaSeat();
        yield return new WaitForSeconds(.25f);
        Check(seated && guideRunning && Mathf.Abs(viewCamera.transform.localPosition.y-1.35f)<.01f,"Sofa interaction retains the seated height and starts a guided break");
        Check(Vector3.Dot(viewCamera.transform.forward, (orb.position-viewCamera.transform.position).normalized)>.9f,"Seated view faces the guided breathing orb");
        Check(subtitlePanel != null && subtitlePanel.activeSelf && subtitle.text.Contains("BREATHE"),"Breathing subtitles remain visible while seated");
        yield return Capture(Path.Combine(output,"04_Sofa_View.png"));
        ToggleSofaSeat();
        Check(!seated && !guideRunning,"Leaving the sofa ends the guided break and restores the standing pose");
        ResetExperience();
        Check(warm && !soundOn && !guideRunning,"Reset restores neutral interaction state");
        yield return new WaitForSeconds(2f);
        Check(errorCount==0,"No runtime errors during automated smoke check");
        File.WriteAllLines(Path.Combine(output,"verification.txt"),checks);
        Application.Quit(errorCount==0 ? 0 : 1);
    }
}
