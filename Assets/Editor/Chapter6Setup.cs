using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using Unity.XR.CoreUtils;
using Object = UnityEngine.Object;

public static class Chapter6Setup
{
    public const string SimulationScene = "Assets/Scenes/QuietCorner_Chapter6_Simulation.unity";
    public const string HeadsetScene = "Assets/Scenes/QuietCorner_Chapter6_OpenXR.unity";
    public static string OutputRoot => Path.GetFullPath("..");

    public static void Create()
    {
        Directory.CreateDirectory("Assets/Chapter6/Prefabs");
        Directory.CreateDirectory("Assets/Chapter6/Materials");
        Directory.CreateDirectory("Assets/XR/Settings");
        ConfigureOpenXR();
        EditorSceneManager.OpenScene("Assets/Scenes/QuietCorner.unity");
        var experience = Object.FindFirstObjectByType<QuietCornerController>();
        var oldPlayer = experience.player.gameObject;
        var start = oldPlayer.transform.position;
        oldPlayer.name = "04 Flat Screen Player (DISABLED for Chapter 6)";
        oldPlayer.SetActive(false);

        var root = new GameObject("06 XR Player - OpenXR");
        root.transform.position = start;
        var origin = root.AddComponent<XROrigin>();
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        origin.CameraYOffset = 0;
        var body = root.AddComponent<CharacterController>();
        body.height=1.7f; body.center=new Vector3(0,.85f,0); body.radius=.24f; body.stepOffset=.2f;
        var offset = new GameObject("Camera Floor Offset");
        offset.transform.SetParent(root.transform,false);
        origin.CameraFloorOffsetObject=offset;
        var headObject = new GameObject("XR Head Camera");
        headObject.tag="MainCamera"; headObject.transform.SetParent(offset.transform,false);
        headObject.transform.localPosition=new Vector3(0,1.65f,0);
        var camera = headObject.AddComponent<Camera>();
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.84f,.75f,.61f);
        camera.nearClipPlane=.05f; camera.farClipPlane=100; camera.fieldOfView=64;
        headObject.AddComponent<AudioListener>();
        AddTracking(headObject,"<XRHMD>",true);
        origin.Camera=camera;
        var left = CreateHand(offset.transform,true);
        var right = CreateHand(offset.transform,false);
        var simulator = root.AddComponent<Chapter6XRSimulator>();
        simulator.headCamera=camera; simulator.trackingSpace=offset.transform; simulator.enabled=false;
        var player = root.AddComponent<Chapter6XRPlayer>();
        player.head=camera; player.body=body; player.simulator=simulator;
        PrefabUtility.SaveAsPrefabAssetAndConnect(root,"Assets/Chapter6/Prefabs/Chapter6 XR Player.prefab",InteractionMode.AutomatedAction);

        // Scene references are overrides; the reusable prefab contains no scene references.
        player.experience=experience; left.experience=experience; right.experience=experience;
        experience.useDesktopInput=false; experience.player=body; experience.viewCamera=camera;
        foreach (var component in new Component[] {player,left,right}) PrefabUtility.RecordPrefabInstancePropertyModifications(component);

        foreach (var text in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
        {
            if (text.text.StartsWith("DDES9902")) text.text="CHAPTER 6   /   OPENXR   /   QUIET CORNER";
            if (text.text.StartsWith("W A S D")) text.text="NO HEADSET: SIMULATION\nWASD / arrows   Move\nRight mouse   Look\nLeft click   Select\nF   Sit / stand | Q / E   Turn\n\nHEADSET: OPENXR SCENE\nLeft stick   Move\nRight stick   Snap turn";
            if (text.text.StartsWith("Click a labelled control")) text.text="Point a controller ray\nat a labelled control.\nPress the trigger to select.\n\nLIGHT   Warm / cool\nMUSIC   Matches the light\nGUIDED BREAK   Start / stop\n\nFloor pads   Teleport\nSTOP   Music + guide";
        }
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),HeadsetScene);
        simulator.enabled=true;
        PrefabUtility.RecordPrefabInstancePropertyModifications(simulator);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),SimulationScene);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SimulationScene,true), new EditorBuildSettingsScene(HeadsetScene,true) };
        PlayerSettings.productName="Quiet Corner - Chapter 6 XR";
        PlayerSettings.runInBackground=true;
        PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[] { GraphicsDeviceType.Direct3D11 });
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("CHAPTER6_SCENES_CREATED");
    }

    static void ConfigureOpenXR()
    {
        const string path="Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset";
        var settings=AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
        if(settings==null) {settings=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(settings,path);}
        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,settings,true);
        if(!settings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Standalone)) settings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Standalone);
        var general=settings.SettingsForBuildTarget(BuildTargetGroup.Standalone);
        general.InitManagerOnStart=false; // The headset scene starts XR; simulation never starts a native runtime.
        if(!XRPackageMetadataStore.AssignLoader(general.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",BuildTargetGroup.Standalone)) throw new Exception("Failed to enable OpenXR loader");
        var xr=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
        if(xr==null) throw new Exception("OpenXR settings not created");
        xr.renderMode=OpenXRSettings.RenderMode.MultiPass;
        var touch=xr.GetFeature<OculusTouchControllerProfile>();
        if(touch==null) throw new Exception("Oculus Touch profile not available");
        touch.enabled=true;
        EditorUtility.SetDirty(touch); EditorUtility.SetDirty(xr); EditorUtility.SetDirty(general); EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    static void AddTracking(GameObject gameObject,string device,bool head)
    {
        var driver=gameObject.AddComponent<TrackedPoseDriver>();
        driver.positionInput=new InputActionProperty(new InputAction("Position",InputActionType.Value,device+(head ? "/centerEyePosition" : "/devicePosition"),expectedControlType:"Vector3"));
        driver.rotationInput=new InputActionProperty(new InputAction("Rotation",InputActionType.Value,device+(head ? "/centerEyeRotation" : "/deviceRotation"),expectedControlType:"Quaternion"));
        driver.trackingStateInput=new InputActionProperty(new InputAction("Tracking State",InputActionType.Value,device+"/trackingState",expectedControlType:"Integer"));
        driver.trackingType=TrackedPoseDriver.TrackingType.RotationAndPosition;
        driver.updateType=TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
    }

    static Chapter6XRRay CreateHand(Transform parent,bool isLeft)
    {
        var hand=new GameObject(isLeft ? "Left Controller" : "Right Controller");
        hand.transform.SetParent(parent,false);
        hand.transform.localPosition=new Vector3(isLeft ? -.25f : .25f,1.25f,.35f);
        AddTracking(hand,"<XRController>{"+(isLeft ? "LeftHand" : "RightHand")+"}",false);
        var visual=GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name="Controller visual"; visual.transform.SetParent(hand.transform,false);
        visual.transform.localScale=new Vector3(.065f,.09f,.065f);
        visual.transform.localRotation=Quaternion.Euler(65,0,0);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        var material=new Material(Shader.Find("Standard"));
        material.color=isLeft ? new Color(.1f,.7f,.75f) : new Color(.9f,.5f,.1f);
        AssetDatabase.CreateAsset(material,"Assets/Chapter6/Materials/"+(isLeft ? "Left" : "Right")+" Controller.mat");
        visual.GetComponent<Renderer>().sharedMaterial=material;
        var line=hand.AddComponent<LineRenderer>();
        line.enabled=false; // Hide the beam while keeping controller ray interactions active.
        line.positionCount=2; line.useWorldSpace=true; line.startWidth=.004f; line.endWidth=.008f;
        var rayMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Chapter6/Materials/Controller Ray.mat");
        if(rayMaterial==null){rayMaterial=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(rayMaterial,"Assets/Chapter6/Materials/Controller Ray.mat");}
        line.sharedMaterial=rayMaterial;
        var ray=hand.AddComponent<Chapter6XRRay>(); ray.leftHand=isLeft; ray.line=line;
        return ray;
    }

    [MenuItem("Chapter 6/Open Simulation (No Headset)")]
    public static void OpenSimulation() {EditorSceneManager.OpenScene(SimulationScene);}

    [MenuItem("Chapter 6/Open OpenXR Headset Scene")]
    public static void OpenHeadset() {EditorSceneManager.OpenScene(HeadsetScene);}

    [MenuItem("Chapter 6/Validate Scene")]
    public static void Validate()
    {
        var all=EditorSceneManager.GetActiveScene().GetRootGameObjects();
        foreach(var root in all)
            foreach(var transform in root.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)>0) throw new Exception("Missing script: "+transform.name);
        var experience=Object.FindFirstObjectByType<QuietCornerController>();
        if(experience.useDesktopInput) throw new Exception("Desktop input is still active");
        if(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length!=1) throw new Exception("Expected one active camera");
        if(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length!=1) throw new Exception("Expected one audio listener");
        if(Object.FindObjectsByType<TrackedPoseDriver>(FindObjectsSortMode.None).Length!=3) throw new Exception("Expected tracked head and two hands");
        if(!all.Any(g=>g.name.Contains("Flat Screen")&&!g.activeSelf)) throw new Exception("Flat screen player not disabled");
        Debug.Log("CHAPTER6_STATIC_VALIDATION_PASS");
    }

    public static void CreateAndVerify()
    {
        Create();
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Chapter 6/Build Windows Demo")]
    public static void Build()
    {
        var packages=UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
        foreach(var package in packages)
            if(package.errors!=null && package.errors.Length>0)
                throw new Exception("Package error: "+package.name+" "+string.Join("; ",package.errors.Select(error=>error.message)));
        Directory.CreateDirectory(Path.Combine(OutputRoot,"Chapter6_Evidence"));
        File.WriteAllLines(Path.Combine(OutputRoot,"Chapter6_Evidence/PackageAudit.txt"),packages
            .Where(package=>package.name.StartsWith("com.unity.xr")||package.name=="com.unity.inputsystem")
            .Select(package=>package.name+" "+package.version+" | source="+package.source+" | package errors=0"));
        OpenSimulation(); Validate();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{SimulationScene,HeadsetScene}, locationPathName=Path.Combine(OutputRoot,"Chapter6_Playable/QuietCorner_Chapter6.exe"),
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.None
        });
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Build failed: "+report.summary.result);
        Debug.Log("CHAPTER6_BUILD_SUCCEEDED");
    }
}
