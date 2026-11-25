using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class A1Setup
{
    static string Output => Path.GetFullPath("..");
    public static void PrepareAndBuildWindows()
    {
        PrepareScenes();
        Build(BuildTarget.StandaloneWindows64,Path.Combine(Output,"A1_Playable/QuietCorner_A1.exe"));
    }

    public static void PrepareScenes()
    {
        foreach (string audioPath in new[] { "Assets/Resources/Audio/GuideIn.wav", "Assets/Resources/Audio/GuideOut.wav" })
        {
            var importer = AssetImporter.GetAtPath(audioPath) as AudioImporter;
            if (importer == null) throw new Exception("Missing voice cue: " + audioPath);
            var audioSettings = importer.defaultSampleSettings;
            audioSettings.loadType = AudioClipLoadType.DecompressOnLoad;
            audioSettings.compressionFormat = AudioCompressionFormat.PCM;
            audioSettings.preloadAudioData = true;
            importer.defaultSampleSettings = audioSettings;
            importer.SaveAndReimport();
        }
        foreach(var scene in new[]{Chapter6Setup.SimulationScene,Chapter6Setup.HeadsetScene})
        {
            EditorSceneManager.OpenScene(scene);
            var targets=Object.FindObjectsByType<QuietCornerTarget>(FindObjectsSortMode.None);
            foreach(var t in targets)
            {
                if(t.GetComponent<EZPZQuietCornerBridge>()==null)t.gameObject.AddComponent<EZPZQuietCornerBridge>();
                if(t.action=="light")t.transform.position=new Vector3(-3.4f,1.35f,2.1f);
                if(t.action=="sound")t.transform.position=new Vector3(3.4f,1.35f,2.1f);
                if(t.action=="pause")t.transform.position=new Vector3(0,1.08f,.59f);
                if(t.action=="stop")t.transform.position=new Vector3(-2.35f,.8f,-.15f);
                if(t.action=="teleport" && t.transform.position.x<0)
                {
                    t.transform.position=new Vector3(-1.9f,.04f,-3.5f);
                    t.destination=new Vector3(-1.9f,.05f,-3.5f);
                }
            }
            foreach(var tr in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if(tr.name.StartsWith("Console"))tr.gameObject.SetActive(false);
            }
            foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                if(text.text=="MOVE HERE" && text.transform.position.x<0)
                    text.transform.position=new Vector3(-1.9f,.067f,-3.5f);
                if(text.text=="01   LIGHT")text.transform.position=new Vector3(-3.4f,1.9f,2);
                if(text.text=="02   SOUND")
                {
                    text.text="02   MUSIC";
                    text.transform.position=new Vector3(3.4f,1.9f,2);
                }
                if(text.text=="03   PAUSE")
                {
                    text.text="03   GUIDED BREAK";
                    text.transform.position=new Vector3(0,1.48f,.5f);
                    text.characterSize=.018f;
                }
                if(text.text.StartsWith("CHAPTER 6"))text.text="QUIET CORNER   /   NICK LI   /   A1";
                if(text.text.StartsWith("Point a controller"))text.text="CHOOSE YOUR ENVIRONMENT\n\nLight beside the lantern\nSound beside the garden\nGuided break beside the orb\n\nPoint and press trigger.\nSTOP ends sound + guide.";
                if(text.text.StartsWith("NO HEADSET"))text.text="YOUR PACE, YOUR CHOICE\n\nThe guide is optional.\nNo need to match the orb.\nMove or stop at any time.\n\nFloor pads move you.\nRESET keeps your position.\n\nPreview: arrows / mouse";
            }
            ApplyFeedbackChanges(scene == Chapter6Setup.SimulationScene);
            Chapter6Setup.Validate();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        PlayerSettings.productName="Quiet Corner";
        PlayerSettings.companyName="Nick Li - DDES9902";
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Chapter6Setup.SimulationScene);
    }

    static void ApplyFeedbackChanges(bool simulation)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        const string materialPath = "Assets/Materials/World Text.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("QuietCorner/World Text"));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.mainTexture = font.material.mainTexture;
        EditorUtility.SetDirty(material);
        foreach (var text in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
        {
            text.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (text.name == "MAKE YOURSELF AT HOME")
            {
                text.text = "START HERE";
                text.transform.position = new Vector3(-4.7f, 2.55f, -.221f);
                text.characterSize = .024f;
            }
            else if (text.name == "EXPLORE AT YOUR PACE")
            {
                text.text = "MOVE & LOOK";
                text.transform.position = new Vector3(4.7f, 2.55f, -.221f);
                text.characterSize = .024f;
            }
            else if (text.text.StartsWith("CHOOSE YOUR ENVIRONMENT") || text.name == "Simple start instructions")
            {
                text.name = "Simple start instructions";
                text.text = "Try LIGHT, then MUSIC.\nMusic follows LIGHT.\nGuide is optional.";
                text.transform.position = new Vector3(-4.7f, 2.15f, -.221f);
                text.characterSize = .018f;
            }
            else if (text.text.StartsWith("YOUR PACE, YOUR CHOICE") || text.name == "Simple movement instructions")
            {
                text.name = "Simple movement instructions";
                text.text = simulation
                    ? "Click + move mouse - look\nQ - turn left | E - chair / right\nAim dot + click - select\nWASD / arrows - move | Shift fast\nF sofa | Esc - stop"
                    : "Left stick - move\nRight stick - turn\nPoint + trigger - select";
                text.transform.position = new Vector3(4.7f, 2.10f, -.221f);
                text.characterSize = .016f;
            }
            else if (text.text.StartsWith("QUIET CORNER   /") || text.text == "A small space to choose your own pace")
                text.gameObject.SetActive(false);
        }
        foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (tr.name != "Instruction plaque") continue;
            tr.position = new Vector3(Mathf.Sign(tr.position.x) * 4.7f, 2.25f, -.15f);
            tr.localScale = new Vector3(1.9f, 1.25f, .12f);
        }
        var experience = Object.FindFirstObjectByType<QuietCornerController>();
        experience.lightStatus.text = "WARM LIGHT";
        experience.soundStatus.text = "MUSIC OFF";
        experience.pauseStatus.text = "START GUIDED BREAK\n48 seconds";
        experience.orbStatus.text = "GUIDE OPTIONAL";
        experience.lightStatus.characterSize = .023f;
        experience.soundStatus.characterSize = .020f;
        experience.pauseStatus.characterSize = .017f;
        if (simulation) EnsureSofaControl(material);
        QuietCornerCafeSetup.Ensure(material);
    }

    static void EnsureSofaControl(Material textMaterial)
    {
        var panel = GameObject.Find("Sofa guided break target") ?? GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Sofa guided break target";
        panel.transform.position = new Vector3(-2.05f, 1.58f, 2.76f);
        panel.transform.localScale = new Vector3(1.2f, .4f, .1f);
        const string sofaMaterialPath = "Assets/Materials/Sofa Control.mat";
        var panelMaterial = AssetDatabase.LoadAssetAtPath<Material>(sofaMaterialPath);
        if (panelMaterial == null)
        {
            panelMaterial = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(panelMaterial, sofaMaterialPath);
        }
        panelMaterial.color = new Color(.16f, .25f, .23f);
        panelMaterial.EnableKeyword("_EMISSION");
        panelMaterial.SetColor("_EmissionColor", new Color(.045f, .075f, .06f));
        panel.GetComponent<MeshRenderer>().sharedMaterial = panelMaterial;
        var target = panel.GetComponent<QuietCornerTarget>() ?? panel.AddComponent<QuietCornerTarget>();
        target.action = "sofa";
        if (panel.GetComponent<EZPZQuietCornerBridge>() == null) panel.AddComponent<EZPZQuietCornerBridge>();

        var labelObject = GameObject.Find("Sofa guided break label") ?? new GameObject("Sofa guided break label");
        labelObject.transform.position = new Vector3(-2.05f, 1.58f, 2.704f);
        labelObject.transform.rotation = Quaternion.identity;
        var label = labelObject.GetComponent<TextMesh>() ?? labelObject.AddComponent<TextMesh>();
        label.text = "SOFA\nF - sit / stand";
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 64;
        label.characterSize = .019f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = new Color(.96f, .91f, .8f);
        label.GetComponent<MeshRenderer>().sharedMaterial = textMaterial;
    }

    public static void BuildFeedbackWindows()
    {
        PrepareScenes();
        Build(BuildTarget.StandaloneWindows64,Path.Combine(Output,"Feedback_2026-11-09/Windows/QuietCorner.exe"));
    }

    public static void BuildFeedbackWebGL()
    {
        PrepareScenes();
        ConfigureWebGL();
        Build(BuildTarget.WebGL,Path.Combine(Output,"Feedback_2026-11-09/WebGL"));
    }
    public static void BuildMouseFixWebGL()
    {
        PrepareScenes();
        ConfigureWebGL();
        Build(BuildTarget.WebGL,Path.Combine(Output,"MouseFix_2026-11-10/WebGL"));
    }
    public static void BuildMouseFixWindows()
    {
        PrepareScenes();
        Build(BuildTarget.StandaloneWindows64,Path.Combine(Output,"MouseFix_2026-11-10/Windows/QuietCorner.exe"));
    }
    public static void BuildMouseLookWebGL()
    {
        PrepareScenes();
        ConfigureWebGL();
        Build(BuildTarget.WebGL,Path.Combine(Output,"MouseLook_2026-11-10/WebGL"));
    }
    public static void BuildMouseLookWindows()
    {
        PrepareScenes();
        Build(BuildTarget.StandaloneWindows64,Path.Combine(Output,"MouseLook_2026-11-10/Windows/QuietCorner.exe"));
    }
    public static void BuildContinuousLookWebGL()
    {
        PrepareScenes();
        ConfigureWebGL();
        Build(BuildTarget.WebGL,Path.Combine(Output,"ContinuousLook_2026-11-10/WebGL"));
    }
    public static void BuildHiddenCursorWebGL()
    {
        PrepareScenes();
        ConfigureWebGL();
        Build(BuildTarget.WebGL,Path.Combine(Output,"HiddenCursor_2026-11-10/WebGL"));
    }
    public static void BuildWebGL()
    {
        PrepareScenes();
        ConfigureWebGL();
        Build(BuildTarget.WebGL,Path.Combine(Output,"A1_WebGL"));
    }
    public static void BuildFeedbackRevisionWindows()
    {
        PrepareScenes();
        Build(BuildTarget.StandaloneWindows64,Path.Combine(Output,"Feedback_2026-11-20/Windows/QuietCorner.exe"));
    }
    public static void BuildCafeWindows()
    {
        PrepareScenes();
        Build(BuildTarget.StandaloneWindows64,Path.Combine(Output,"Cafe_2026-11-21/Windows/QuietCorner.exe"));
    }
    public static void BuildSeatWindows()
    {
        PrepareScenes();
        Build(BuildTarget.StandaloneWindows64,Path.Combine(Output,"ESeat_2026-11-22/Windows/QuietCorner.exe"));
    }
    public static void BuildSeatWebGL()
    {
        PrepareScenes();
        ConfigureWebGL();
        Build(BuildTarget.WebGL,Path.Combine(Output,"ESeat_2026-11-22/WebGL"));
    }
    public static void BuildNoEdgeTurnWebGL()
    {
        PrepareScenes();
        ConfigureWebGL();
        Build(BuildTarget.WebGL,Path.Combine(Output,"NoEdgeTurn_2026-11-23/WebGL"));
    }
    public static void BuildFullTurnWebGL()
    {
        PrepareScenes();
        ConfigureWebGL();
        Build(BuildTarget.WebGL,Path.Combine(Output,"FullTurn_2026-11-24/WebGL"));
    }
    public static void BuildCameraFallbackWebGL()
    {
        PrepareScenes();
        ConfigureWebGL();
        Build(BuildTarget.WebGL,Path.Combine(Output,"Camera360Fix_2026-11-24/WebGL"));
    }
    public static void BuildMusicContrastWindows()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-qc-audio-output") >= 0)
            QuietCornerMusicVerification.Export();
        PrepareScenes();
        Build(BuildTarget.StandaloneWindows64,Path.Combine(Output,"MusicContrast_2026-11-25/Windows/QuietCorner.exe"));
    }
    public static void BuildMusicContrastWebGL()
    {
        PrepareScenes();
        ConfigureWebGL();
        Build(BuildTarget.WebGL,Path.Combine(Output,"MusicContrast_2026-11-25/WebGL"));
    }
    public static void BuildCafeWebGL()
    {
        PrepareScenes();
        ConfigureWebGL();
        Build(BuildTarget.WebGL,Path.Combine(Output,"Cafe_2026-11-21/WebGL"));
    }
    public static void BuildFeedbackRevisionAll()
    {
        BuildFeedbackRevisionWindows();
        BuildFeedbackRevisionWebGL();
    }
    public static void BuildFeedbackRevisionWebGL()
    {
        PrepareScenes();
        ConfigureWebGL();
        Build(BuildTarget.WebGL,Path.Combine(Output,"Feedback_2026-11-20/WebGL"));
    }
    static void ConfigureWebGL()
    {
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback=false;
        PlayerSettings.WebGL.initialMemorySize=128;
        PlayerSettings.WebGL.maximumMemorySize=512;
        PlayerSettings.WebGL.memoryGrowthMode=WebGLMemoryGrowthMode.Geometric;
        PlayerSettings.colorSpace=ColorSpace.Gamma;
    }
    static void Build(BuildTarget target,string location)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(location));
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Chapter6Setup.SimulationScene},target=target,locationPathName=location,options=BuildOptions.None});
        Directory.CreateDirectory(Path.Combine(Output,"A1_Evidence"));
        File.WriteAllText(Path.Combine(Output,"A1_Evidence",target+"_Build.txt"),"Result: "+result.summary.result+"\nBytes: "+result.summary.totalSize+"\nSeconds: "+result.summary.totalTime.TotalSeconds+"\nErrors: "+result.summary.totalErrors+"\nUnity: "+Application.unityVersion);
        if(result.summary.result!=BuildResult.Succeeded)throw new Exception("A1 build failed: "+target);
        Debug.Log("A1_BUILD_SUCCESS "+target);
    }
}
