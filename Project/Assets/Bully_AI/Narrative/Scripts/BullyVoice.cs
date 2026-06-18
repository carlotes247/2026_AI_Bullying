using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;   // System.Diagnostics also has a Debug; disambiguate

namespace Bully
{
    /// <summary>
    /// Speaks the bully's taunt out loud using the operating system's built-in TTS
    /// (macOS `say`, Windows SAPI via PowerShell). Desktop only — Process isn't
    /// available on mobile/WebGL.
    ///
    /// Completely separate from the speech bubble: both just subscribe to
    /// BullyBrain.OnTaunt, so the voice and the on-screen text fire at the same moment.
    /// Put this on its OWN GameObject (e.g. "BullyVoice").
    /// </summary>
    public class BullyVoice : MonoBehaviour
    {
        [Header("Wiring")]
        public BullyBrain brain;

        [Header("macOS voice — run `say -v ?` in Terminal to list options")]
        public string macVoice  = "";   // e.g. "Daniel", "Samantha", "Trinoids"; empty = system default
        public int    macRateWpm = 0;   // words per minute; 0 = default

        Process current;

        void OnEnable()  { if (brain) brain.OnTaunt += Speak; }
        void OnDisable() { if (brain) brain.OnTaunt -= Speak; StopCurrent(); }
        void OnApplicationQuit() => StopCurrent();

        void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            StopCurrent();   // interrupt any taunt still being spoken

            try
            {
                var psi = new ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true };

                switch (Application.platform)
                {
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.OSXPlayer:
                        psi.FileName = "/usr/bin/say";
                        if (!string.IsNullOrEmpty(macVoice)) { psi.ArgumentList.Add("-v"); psi.ArgumentList.Add(macVoice); }
                        if (macRateWpm > 0)                  { psi.ArgumentList.Add("-r"); psi.ArgumentList.Add(macRateWpm.ToString()); }
                        psi.ArgumentList.Add(text);          // passed as a real argument — no shell escaping needed
                        break;

                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer:
                        psi.FileName = "powershell";
                        psi.ArgumentList.Add("-NoProfile");
                        psi.ArgumentList.Add("-Command");
                        // Read the text from stdin so we don't fight PowerShell's quoting.
                        psi.ArgumentList.Add("Add-Type -AssemblyName System.Speech; " +
                            "(New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak([Console]::In.ReadToEnd())");
                        psi.RedirectStandardInput = true;
                        break;

                    default:
                        Debug.LogWarning("[BullyVoice] OS TTS is only wired for macOS / Windows.");
                        return;
                }

                current = Process.Start(psi);   // returns immediately; speech plays in the background

                if (psi.RedirectStandardInput && current != null)   // Windows path
                {
                    current.StandardInput.Write(text);
                    current.StandardInput.Close();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BullyVoice] TTS failed: {e.Message}");
            }
        }

        void StopCurrent()
        {
            try { if (current != null && !current.HasExited) current.Kill(); }
            catch { /* already gone */ }
            current = null;
        }
    }
}
