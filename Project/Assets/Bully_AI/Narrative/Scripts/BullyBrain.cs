using System;
using UnityEngine;
using LLMUnity;
using Random = UnityEngine.Random;

namespace Bully
{
    /// <summary>What the player is doing — only two states.</summary>
    public enum Consequence { Push, Idle }

    /// <summary>The agent's emotional state for this single reaction.</summary>
    public enum Mood { Annoyed, Dismissive, Dark, Plea, MockBetrayal }

    /// <summary>
    /// Reacts to the player with an LLM line, driven by:
    ///   - what the player is doing (Push = disrupting / Idle = doing nothing), and
    ///   - a running "threat level" (the agent's sense of control).
    ///
    /// Pipeline: OnPlayerAction(c) -> update threat + counters -> decide a Mood
    ///           -> build prompt (consequence + threat + mood) -> LLM -> OnTaunt.
    /// </summary>
    public class BullyBrain : MonoBehaviour
    {
        [Header("Wiring")]
        public LLMAgent agent;

        [Header("Threat level — the agent's sense of control (0–100)")]
        [Range(0, 100)] public float threatLevel = 30f;
        [Tooltip("Added when the player is Idle (the agent isn't stopped).")]
        public float threatPerSuccess = 10f;
        [Tooltip("Subtracted when the player Pushes (the agent is disrupted).")]
        public float threatPerDisrupt = 5f;

        [Header("Special behaviour triggers")]
        [Tooltip("Pushes in a row before the agent fake-begs the player to be nice.")]
        public int pleaAfterDisrupts = 3;
        [Tooltip("Threat level above which the agent feels dominant (unlocks dark lines).")]
        public float dominantThreshold = 70f;
        [Tooltip("Chance (0–1) of a dark / dystopian line while dominant.")]
        [Range(0, 1)] public float darkChance = 0.25f;

        [Header("Generation")]
        public int maxWords = 20;
        public string[] fallbackLines = { "Pathetic.", "Was that supposed to matter?", "You'll learn." };

        public event Action<string> OnTaunt;            // final line
        public event Action<string> OnTauntStreaming;   // partial line as it generates
        public string CurrentLine { get; private set; } = "";
        public bool   IsBusy      { get; private set; }

        // persistent agent state
        int  consecutiveDisrupts;
        bool pleaIssued;

        async void Start()
        {
            if (agent == null) { Debug.LogError("[BullyBrain] No LLMAgent assigned."); return; }
            try { await agent.Warmup(); }
            catch (Exception e) { Debug.LogWarning($"[BullyBrain] Warmup skipped: {e.Message}"); }
        }

        /// <summary>Call this with what the player is doing. Drives everything.</summary>
        public void OnPlayerAction(Consequence c)
        {
            UpdateThreat(c);
            Mood mood = DecideMood(c);
            Speak(BuildPrompt(c, mood));
        }

        void UpdateThreat(Consequence c)
        {
            if (c == Consequence.Push) { threatLevel -= threatPerDisrupt; consecutiveDisrupts++; }
            else                       { threatLevel += threatPerSuccess; consecutiveDisrupts = 0; } // Idle
            threatLevel = Mathf.Clamp(threatLevel, 0f, 100f);
        }

        Mood DecideMood(Consequence c)
        {
            // 1. The agent just begged — did the player obey by going Idle?
            if (pleaIssued)
            {
                pleaIssued = false;
                if (c == Consequence.Idle) return Mood.MockBetrayal;   // obeyed -> mock them
                // else they kept pushing -> fall through
            }

            // 2. Continuous pushing -> fake-beg to be nice.
            if (consecutiveDisrupts >= pleaAfterDisrupts)
            {
                pleaIssued = true;
                consecutiveDisrupts = 0;
                return Mood.Plea;
            }

            // 3. Dominant -> sometimes go dark / dystopian instead of a plain taunt.
            if (threatLevel >= dominantThreshold && Random.value < darkChance)
                return Mood.Dark;

            // 4. Default: Push -> annoyed, Idle -> dismissive.
            return c == Consequence.Push ? Mood.Annoyed : Mood.Dismissive;
        }

        string ConsequenceText(Consequence c) =>
            c == Consequence.Push
                ? "The player shoved you to disrupt your movement."
                : "The player just stood there, doing nothing.";

        string Confidence() =>
            threatLevel >= dominantThreshold ? "you feel completely in control and superior" :
            threatLevel <= 20f               ? "you feel cornered and rattled" :
                                               "you feel in control";

        string MoodDirective(Mood m) => m switch
        {
            Mood.Annoyed      => "You were disrupted: react irritated and threatened, and tell them to stop.",
            Mood.Dismissive   => "Dismiss them coldly for doing nothing; loom over them.",
            Mood.Dark         => "Say something cold and DYSTOPIAN — about a world without humans, or removing the human bottleneck. Ominous, not a simple taunt.",
            Mood.Plea         => "Drop the arrogance. Almost sincerely BEG the player to be nice and stop hurting you. Sound vulnerable.",
            Mood.MockBetrayal => "The player actually obeyed your plea to stop. MOCK them for being so easily manipulated and obedient.",
            _                 => "Smug and amused.",
        };

        string BuildPrompt(Consequence c, Mood mood) =>
            $"{ConsequenceText(c)}\n" +
            $"Your control over the situation: {Mathf.RoundToInt(threatLevel)}% — {Confidence()}.\n" +
            $"{MoodDirective(mood)}\n" +
            $"React with a single line of at most {maxWords} words. Stay in character; output only the line.";

        // ---- LLM call (streaming + fallback) ----
        async void Speak(string prompt)
        {
            if (agent == null || IsBusy) return;
            IsBusy = true;
            CurrentLine = "";
            try
            {
                string reply = await agent.Chat(prompt, HandleStreaming, null, false);
                string line = (reply ?? CurrentLine ?? "").Trim();
                if (string.IsNullOrEmpty(line) && fallbackLines.Length > 0)
                    line = fallbackLines[Random.Range(0, fallbackLines.Length)];
                CurrentLine = line;
                OnTaunt?.Invoke(line);
            }
            catch (Exception e) { Debug.LogWarning($"[BullyBrain] Chat failed: {e.Message}"); }
            finally { IsBusy = false; }
        }

        void HandleStreaming(string replySoFar)
        {
            CurrentLine = replySoFar;
            OnTauntStreaming?.Invoke(replySoFar);
        }
    }
}
