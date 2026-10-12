using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Optional reproducible capture of real scene frames. Not enabled in normal play.
public sealed class A1DemoCapture : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Begin()
    {
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-a1-demo") >= 0)
            new GameObject("A1 demonstration recorder").AddComponent<A1DemoCapture>();
    }
    IEnumerator Start()
    {
        var args = System.Environment.GetCommandLineArgs();
        int arg = System.Array.IndexOf(args, "-a1-demo-dir");
        string folder = arg >= 0 ? args[arg+1] : Path.GetFullPath("DemoFrames");
        Directory.CreateDirectory(folder);
        var sim = FindFirstObjectByType<Chapter6XRSimulator>();
        var rig = FindFirstObjectByType<Chapter6XRPlayer>();
        var experience = rig.experience;
        sim.automated = true;
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        Time.captureFramerate = 10;
        for (int j=0; j<20; j++) yield return null;
        experience.ResetExperience();
        var targets = FindObjectsByType<QuietCornerTarget>(FindObjectsSortMode.None);
        QuietCornerTarget Find(string action) => targets.First(t => t.action == action);
        var light = Find("light"); var sound=Find("sound"); var pace=Find("pause");
        var stop=Find("stop"); var reset=Find("reset");
        var pad=targets.Where(t=>t.action=="teleport").OrderBy(t=>t.destination.z).First();
        var camera=rig.head;
        var rt=new RenderTexture(1280,720,24);
        var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
        var log=new System.Collections.Generic.List<string>();
        ExportAudio(experience.GetComponent<AudioSource>().clip,Path.Combine(folder,"ambient-original.wav"));
        for(int frame=0;frame<1500;frame++)
        {
            float t=frame/10f;
            var focus=t<12 ? null : t<38 ? light : t<50 ? sound : t<108 ? pace : t<116 ? stop : t<132 ? pad : reset;
            Vector3 look = focus != null ? focus.transform.position : new Vector3(0,1.5f,2);
            if(t>=54 && t<104) look=experience.orb.position+Vector3.up*.15f;
            if(t>=121) look=experience.orb.position+Vector3.up*.15f;
            var direction=rig.transform.InverseTransformDirection(look-camera.transform.position);
            float yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
            float pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
            var rotation=Quaternion.Euler(Mathf.Clamp(pitch,-60,60),yaw,0);
            sim.headRotation=Quaternion.Slerp(sim.headRotation,rotation,.18f);
            sim.leftPosition=sim.headPosition+sim.headRotation*new Vector3(-.25f,-.4f,.35f);
            sim.rightPosition=sim.headPosition+sim.headRotation*new Vector3(.25f,-.4f,.35f);
            sim.leftRotation=sim.headRotation;
            if(focus!=null)sim.AimAt(focus.GetComponent<Collider>().bounds.center);
            else sim.rightRotation=sim.headRotation;
            bool press=frame==160 || frame==280 || frame==420 || frame==520 || frame==1060 || frame==1120 || frame==1200 || frame==1360;
            sim.rightTrigger=press;
            yield return null;
            yield return new WaitForEndOfFrame();
            if(press)log.Add(t+"s: trigger "+focus.action+" | warm="+experience.warm+" sound="+experience.soundOn+" guide="+experience.guideRunning+" position="+rig.transform.position);
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
            File.WriteAllBytes(Path.Combine(folder,"frame_"+frame.ToString("D5")+".jpg"),pixels.EncodeToJPG(87));
            camera.targetTexture=oldTarget;RenderTexture.active=oldActive;
        }
        File.WriteAllLines(Path.Combine(folder,"Demo_Interaction_Log.txt"),log);
        Time.captureFramerate=0;rt.Release();Destroy(rt);Destroy(pixels);
        Application.Quit();
    }
    static void ExportAudio(AudioClip clip,string path)
    {
        var data=new float[clip.samples*clip.channels];clip.GetData(data,0);
        using(var stream=new BinaryWriter(File.Create(path)))
        {
            stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));stream.Write(36+data.Length*2);
            stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));stream.Write(16);
            stream.Write((short)1);stream.Write((short)clip.channels);stream.Write(clip.frequency);
            stream.Write(clip.frequency*clip.channels*2);stream.Write((short)(clip.channels*2));stream.Write((short)16);
            stream.Write(System.Text.Encoding.ASCII.GetBytes("data"));stream.Write(data.Length*2);
            foreach(float sample in data)stream.Write((short)(Mathf.Clamp(sample,-1,1)*32767));
        }
    }
}
