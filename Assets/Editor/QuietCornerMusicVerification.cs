using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Exports the actual generated clips, rather than a second implementation of the synth.
public static class QuietCornerMusicVerification
{
    public static void Export()
    {
        string[] args = Environment.GetCommandLineArgs();
        int arg = Array.IndexOf(args, "-qc-audio-output");
        if (arg < 0 || arg + 1 >= args.Length) throw new ArgumentException("Missing -qc-audio-output");
        string folder = args[arg + 1];
        Directory.CreateDirectory(folder);
        var host = new GameObject("Music verification");
        try
        {
            var controller = host.AddComponent<QuietCornerController>();
            var generate = typeof(QuietCornerController).GetMethod("MakeMusicClip", BindingFlags.Instance | BindingFlags.NonPublic);
            using (var metrics = new StreamWriter(Path.Combine(folder, "clip_metrics.csv")))
            {
                metrics.WriteLine("track,sample_rate,seconds,rms,peak,first_sample,last_sample,max_adjacent_step");
                foreach (bool warm in new[] { true, false })
                {
                    var clip = (AudioClip)generate.Invoke(controller, new object[] { warm });
                    string name = warm ? "Warm" : "Cool";
                    var data = new float[clip.samples * clip.channels];
                    if (!clip.GetData(data, 0)) throw new Exception("Cannot read " + name);
                    double energy = 0; float peak = 0, step = 0;
                    for (int i = 0; i < data.Length; i++)
                    {
                        float sample = data[i];
                        if (float.IsNaN(sample) || float.IsInfinity(sample)) throw new Exception("Nonfinite sample");
                        peak = Mathf.Max(peak, Mathf.Abs(sample)); energy += sample * sample;
                        if (i > 0) step = Mathf.Max(step, Mathf.Abs(sample - data[i - 1]));
                    }
                    double rms = Math.Sqrt(energy / data.Length);
                    if (peak >= .99f || rms < .001 || Math.Abs(data[0] - data[data.Length-1]) > .001)
                        throw new Exception(name + " clipping, silent, or discontinuous loop");
                    metrics.WriteLine(FormattableString.Invariant($"{name},{clip.frequency},{clip.length},{rms},{peak},{data[0]},{data[data.Length-1]},{step}"));
                    ExportWav(data, clip.frequency, Path.Combine(folder, name + ".wav"));
                    UnityEngine.Object.DestroyImmediate(clip);
                }
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(host); }
        Debug.Log("QUIET_CORNER_AUDIO_EXPORT_SUCCESS " + folder);
    }

    static void ExportWav(float[] data, int rate, string path)
    {
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + data.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
            writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(data.Length * 2);
            foreach (float sample in data) writer.Write((short)(sample * 32767));
        }
    }
}
