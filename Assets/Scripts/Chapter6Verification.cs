using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;

// Runs actual simulated-device input through pose drivers, rays and scene logic.
public class Chapter6Verification : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RunIfRequested()
    {
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-chapter6-verify")>=0)
            new GameObject("Temporary automated verification").AddComponent<Chapter6Verification>();
    }
    readonly List<string> checks=new List<string>();
    int failures, runtimeErrors;
    Chapter6XRSimulator sim;
    Chapter6XRPlayer rig;
    QuietCornerController experience;
    string evidence;

    void Check(bool condition,string name) {checks.Add((condition?"PASS: ":"FAIL: ")+name);if(!condition)failures++;}
    void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert){runtimeErrors++;checks.Add("RUNTIME ERROR: "+message);}}

    IEnumerator Frames(int count=5){for(int i=0;i<count;i++)yield return null;}

    IEnumerator Activate(string action,bool left=false)
    {
        var target=FindObjectsByType<QuietCornerTarget>(FindObjectsSortMode.None).First(t=>t.action==action);
        var collider=target.GetComponent<Collider>();
        sim.AimAt(collider.bounds.center,left);
        yield return Frames();
        var ray=rig.GetComponentsInChildren<Chapter6XRRay>().First(r=>r.leftHand==left);
        Check(ray.hovered==target,"Controller ray reaches "+action+(left?" (left hand)":" (right hand)"));
        var bridge=target.GetComponent<EZPZQuietCornerBridge>();
        Check(ray.hoveredEZPZ==target.GetComponent<InteractableGeneral>() && bridge!=null,"EZPZ event layer present for "+action);
        int eventBefore=bridge.activationCount;
        int before=ray.interactionCount;
        if(left)sim.leftTrigger=true;else sim.rightTrigger=true;
        yield return Frames();
        Check(ray.interactionCount==before+1,"Trigger invokes "+action+" exactly once");
        Check(bridge.activationCount==eventBefore+1,"EZPZ primary event routes "+action+" exactly once");
        if(left)sim.leftTrigger=false;else sim.rightTrigger=false;
        yield return Frames();
    }

    IEnumerator Start()
    {
        Application.logMessageReceived+=Log;
        var args=System.Environment.GetCommandLineArgs();
        int index=System.Array.IndexOf(args,"-chapter6-evidence");
        evidence=index>=0&&index+1<args.Length ? args[index+1] : Path.Combine(Path.GetFullPath(".."),"Chapter6_Evidence");
        Directory.CreateDirectory(evidence);
        sim=FindFirstObjectByType<Chapter6XRSimulator>();sim.automated=true;
        rig=FindFirstObjectByType<Chapter6XRPlayer>();experience=rig.experience;
        yield return Frames(20);
        Check(InputSystem.devices.Count(d=>d.name.StartsWith("Chapter6"))==3,"Three simulated XR devices are registered");
        Check(rig.IsSimulation,"Simulation mode active; no hardware session required");
        sim.headPosition=new Vector3(.12f,1.72f,.08f);sim.headRotation=Quaternion.Euler(5,18,0);
        sim.leftPosition=new Vector3(-.32f,1.35f,.45f);
        yield return Frames();
        Check(Vector3.Distance(rig.head.transform.localPosition,sim.headPosition)<.005f,"HMD position drives camera (simulated 6-DoF)");
        Check(Quaternion.Angle(rig.head.transform.localRotation,sim.headRotation)<.5f,"HMD rotation drives camera");
        var left=rig.GetComponentsInChildren<Chapter6XRRay>().First(r=>r.leftHand);
        Check(Vector3.Distance(left.transform.localPosition,sim.leftPosition)<.005f,"Left controller position follows XR input");
        Capture("02_Tracked_Head_View.png");
        sim.headPosition=new Vector3(0,1.65f,0);sim.headRotation=Quaternion.identity;
        sim.leftPosition=new Vector3(-.25f,1.25f,.35f);
        yield return Frames();
        yield return Activate("light");Check(!experience.warm,"XR trigger switches light to cool");
        yield return Activate("light",true);Check(experience.warm,"Left-hand trigger switches light back to warm");
        yield return Activate("sound");Check(experience.soundOn,"XR trigger starts ambient sound");
        yield return Activate("pause");Check(experience.guideRunning,"XR trigger starts visual guide");
        yield return new WaitForSeconds(1);
        Check(experience.guideTime>.5f,"Visual guide advances with time");
        yield return Activate("stop");Check(!experience.guideRunning&&!experience.soundOn,"Stop control ends guide and sound");
        yield return Activate("reset");Check(experience.warm&&!experience.soundOn&&!experience.guideRunning,"Reset restores initial state");
        Vector3 before=rig.transform.position;
        yield return Activate("teleport");Check(Vector3.Distance(before,rig.transform.position)>1,"XR trigger teleports the player");
        experience.Teleport(new Vector3(0,.05f,-6.3f));
        sim.headPosition=new Vector3(0,1.65f,0);sim.headRotation=Quaternion.identity;
        yield return Frames();
        sim.AimAt(FindObjectsByType<QuietCornerTarget>(FindObjectsSortMode.None).First(t=>t.action=="light").transform.position);
        yield return Frames();Capture("01_Chapter6_Simulation.png");
        Check(runtimeErrors==0,"No Unity errors or exceptions during interaction checks");
        checks.Add("ENVIRONMENT: No native OpenXR runtime is installed. Native plugin startup may log XR_ERROR_RUNTIME_UNAVAILABLE; simulated input checks do not validate an OpenXR session.");
        checks.Add("NOT TESTED: Physical headset, Quest Link, native OpenXR runtime, stereo rendering and real tracking.");
        checks.Add("Unity "+Application.unityVersion+" | Scene "+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        File.WriteAllLines(Path.Combine(evidence,"Verification.txt"),checks);
        Debug.Log("CHAPTER6_VERIFICATION_COMPLETE failures="+failures);
        Application.logMessageReceived-=Log;
#if UNITY_EDITOR
        EditorApplication.Exit(failures==0?0:1);
#else
        Application.Quit(failures==0?0:1);
#endif
    }

    void Capture(string name)
    {
        var camera=rig.head;var previous=RenderTexture.active;var previousTarget=camera.targetTexture;
        var target=new RenderTexture(1600,900,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();
        File.WriteAllBytes(Path.Combine(evidence,name),pixels.EncodeToPNG());
        camera.targetTexture=previousTarget;RenderTexture.active=previous;Destroy(pixels);target.Release();Destroy(target);
    }
}
