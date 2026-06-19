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

        [Header("Enable")]
        public bool enabled_tts = false;

        [Header("macOS voice — run `say -v ?` in Terminal to list options")]
        public string macVoice  = "";   // e.g. "Daniel", "Samantha", "Trinoids"; empty = system default
        public int    macRateWpm = 0;   // words per minute; 0 = default

        Process current;

        void Awake()
        {
            if (brain == null)
                brain = GetComponent<BullyBrain>();
            if (brain == null)
                brain = FindFirstObjectByType<BullyBrain>(FindObjectsInactive.Include);
        }

        void OnEnable()  { if (brain) brain.OnTaunt += Speak; }
        void OnDisable() { if (brain) brain.OnTaunt -= Speak; StopCurrent(); }
        void OnApplicationQuit() => StopCurrent();

        void Speak(string text)
        {
            if (!enabled_tts || string.IsNullOrWhiteSpace(text)) return;
            StopCurrent();   // interrupt any taunt still being spoken

            try
            {
                var psi = new ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true };

                switch (Application.platform)
                {
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.OSXPlayer:
                        psi.FileName = "/usr/bin/say";
                        // Unity's Mono process layer can silently ignore ArgumentList.
                        // `say` reads text from stdin, which also avoids quoting generated text.
                        psi.RedirectStandardInput = true;
                        string macArguments = "";
                        if (!string.IsNullOrWhiteSpace(macVoice))
                            macArguments += $" -v {QuoteArgument(macVoice)}";
                        if (macRateWpm > 0)
                            macArguments += $" -r {macRateWpm}";
                        psi.Arguments = macArguments.TrimStart();
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

                if (psi.RedirectStandardInput && current != null)
                {
                    current.StandardInput.Write(text);
                    current.StandardInput.Close();
                }

                Debug.Log($"[BullyVoice] Speaking: {text}");
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

        static string QuoteArgument(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}
