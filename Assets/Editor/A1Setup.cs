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
            }
            foreach(var tr in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if(tr.name.StartsWith("Console"))tr.gameObject.SetActive(false);
            }
            foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                if(text.text=="01   LIGHT")text.transform.position=new Vector3(-3.4f,1.9f,2);
                if(text.text=="02   SOUND")text.transform.position=new Vector3(3.4f,1.9f,2);
                if(text.text=="03   PAUSE")text.transform.position=new Vector3(0,1.48f,.5f);
                if(text.text.StartsWith("CHAPTER 6"))text.text="QUIET CORNER   /   NICK LI   /   A1";
                if(text.text.StartsWith("Point a controller"))text.text="CHOOSE YOUR ENVIRONMENT\n\nLight beside the lantern\nSound beside the garden\nPace beside the orb\n\nPoint and press trigger.\nSTOP ends sound + guide.";
                if(text.text.StartsWith("NO HEADSET"))text.text="YOUR PACE, YOUR CHOICE\n\nThe guide is optional.\nNo need to match the orb.\nMove or stop at any time.\n\nFloor pads move you.\nRESET keeps your position.\n\nPreview: WASD / mouse";
            }
            Chapter6Setup.Validate();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        PlayerSettings.productName="Quiet Corner - A1";
        PlayerSettings.companyName="Nick Li - DDES9902";
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Chapter6Setup.SimulationScene);
        Build(BuildTarget.StandaloneWindows64,Path.Combine(Output,"A1_Playable/QuietCorner_A1.exe"));
    }
    public static void BuildWebGL()
    {
        EditorSceneManager.OpenScene(Chapter6Setup.SimulationScene);
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback=false;
        PlayerSettings.WebGL.initialMemorySize=128;
        PlayerSettings.WebGL.maximumMemorySize=512;
        PlayerSettings.WebGL.memoryGrowthMode=WebGLMemoryGrowthMode.Geometric;
        PlayerSettings.colorSpace=ColorSpace.Gamma;
        Build(BuildTarget.WebGL,Path.Combine(Output,"A1_WebGL"));
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
