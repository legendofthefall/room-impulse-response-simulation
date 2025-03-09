using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using SteamAudio;

public class SteamAudioRIRAnalysis : MonoBehaviour
{
    public SteamAudioSource steamAudioSource;
    private List<float> rirSamples = new List<float>();

    void Start()
    {
        if (steamAudioSource == null)
            steamAudioSource = GetComponent<SteamAudioSource>();

        if (steamAudioSource == null)
        {
            UnityEngine.Debug.LogError("❌ SteamAudioSource component missing! Attach this script to a GameObject with SteamAudioSource.");
            return;
        }

        UnityEngine.Debug.Log("🔍 Extracting RIR...");
        ExtractImpulseResponse();
    }

    void ExtractImpulseResponse()
    {
        if (steamAudioSource == null)
        {
            UnityEngine.Debug.LogError("❌ SteamAudioSource reference missing.");
            return;
        }

        AudioSource audioSource = steamAudioSource.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            UnityEngine.Debug.LogError("❌ AudioSource missing on the same GameObject as SteamAudioSource!");
            return;
        }

        AudioClip clip = audioSource.clip;
        if (clip == null)
        {
            UnityEngine.Debug.LogError("❌ No audio clip found in AudioSource. Make sure Steam Audio has computed the IR.");
            return;
        }

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        rirSamples = samples.ToList();

        if (rirSamples.Count > 0)
        {
            UnityEngine.Debug.Log($"✅ RIR Extracted: {rirSamples.Count} samples.");
            PlotEDC();
        }
        else
        {
            UnityEngine.Debug.LogWarning("⚠ No valid RIR data extracted. Check Steam Audio settings.");
        }
    }

    void PlotEDC()
    {
        if (rirSamples == null || rirSamples.Count == 0)
        {
            UnityEngine.Debug.LogWarning("⚠ No valid RIR data to compute EDC.");
            return;
        }

        float maxAmplitude = rirSamples.Max();
        List<float> normalizedRIR = rirSamples.Select(sample => sample / maxAmplitude).ToList();

        List<float> edc = new List<float>();
        float energy = normalizedRIR.Sum(sample => sample * sample);
        for (int i = 0; i < normalizedRIR.Count; i++)
        {
            energy -= normalizedRIR[i] * normalizedRIR[i];
            edc.Add(energy);
        }

        UnityEngine.Debug.Log("📈 EDC Computed. Now saving graph...");
        SaveEDCToCSV(edc);
        RunPythonPlot();
    }

    void SaveEDCToCSV(List<float> edcData)
    {
        string filePath = Path.Combine(Application.dataPath, "EDC_Data.csv");
        File.WriteAllLines(filePath, edcData.Select(value => value.ToString()));
        UnityEngine.Debug.Log($"✅ EDC Data Saved: {filePath}");
    }

    void RunPythonPlot()
    {
        string pythonPath = "/usr/bin/python3"; // Explicitly use Python 3 on macOS
        string pythonScriptPath = Path.Combine(Application.dataPath, "PlotEDC.py");

        if (!File.Exists(pythonScriptPath))
        {
            UnityEngine.Debug.LogError($"❌ Python script not found: {pythonScriptPath}");
            return;
        }

        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = pythonPath;
        psi.Arguments = $"\"{pythonScriptPath}\"";
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        using (Process process = Process.Start(psi))
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                UnityEngine.Debug.LogError($"❌ Python Error: {error}");
            }
            else
            {
                UnityEngine.Debug.Log($"✅ Python Output: {output}");
                UnityEngine.Debug.Log("📸 EDC Graph Image Saved!");
            }
        }
    }
}
