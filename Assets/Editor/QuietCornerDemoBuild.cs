using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Separate standalone recording build; does not replace the published WebGL package.
public static class QuietCornerDemoBuild
{
    public static void Build()
    {
        string output = Path.GetFullPath("../A2_Report_2026-11-25/Development_Materials/DemoRecorder/Windows/QuietCorner.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { Chapter6Setup.SimulationScene },
            target = BuildTarget.StandaloneWindows64,
            locationPathName = output,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Demo recorder build failed");
        Debug.Log("QUIET_CORNER_DEMO_BUILD_SUCCESS " + output);
    }
}
