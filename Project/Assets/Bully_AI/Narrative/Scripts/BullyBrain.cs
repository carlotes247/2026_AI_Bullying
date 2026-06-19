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

        [Header("Initial threat")]
        public Vector2Int initialThreatRange = new Vector2Int(10, 30);

        [Header("Threat score — the agent's sense of control (unbounded)")]
        [Min(0f)] public float threatLevel = 30f;
        [Tooltip("Added when the player is Idle (the agent isn't stopped).")]
        public float threatPerSuccess = 1f;
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
        [Tooltip("Minimum wall-clock seconds between two successive LLM responses.")]
        [Min(0f)] public float minSecondsBetweenLines = 5f;

        public event Action<string> OnTaunt;            // final line
        public event Action<string> OnTauntStreaming;   // partial line as it generates
        public event Action<float> OnThreatChanged;
        public string CurrentLine { get; private set; } = "";
        public string CurrentPrompt { get; private set; } = "";
        public bool   IsBusy      { get; private set; }

        // persistent agent state
        int  consecutiveDisrupts;
        bool pleaIssued;
        string queuedPrompt;
        float lastSpokeAt = float.NegativeInfinity;

        void Awake()
        {
            int minimum = Mathf.Min(initialThreatRange.x, initialThreatRange.y);
            int maximum = Mathf.Max(initialThreatRange.x, initialThreatRange.y);
            threatLevel = Random.Range(minimum, maximum + 1);
        }

        async void Start()
        {
            if (agent == null) { Debug.LogError("[BullyBrain] No LLMAgent assigned."); return; }
            try { await agent.Warmup(); }
            catch (Exception e) { Debug.LogWarning($"[BullyBrain] Warmup skipped: {e.Message}"); }
        }

        void Update()
        {
            if (!IsBusy && !string.IsNullOrEmpty(queuedPrompt) &&
                Time.time - lastSpokeAt >= minSecondsBetweenLines)
            {
                string nextPrompt = queuedPrompt;
                queuedPrompt = null;
                Speak(nextPrompt);
            }
        }

        /// <summary>Call this with what the player is doing. Drives everything.</summary>
        public void OnPlayerAction(Consequence c)
        {
            UpdateThreat(c);
            Mood mood = DecideMood(c);
            CurrentPrompt = BuildPrompt(c, mood);
            Speak(CurrentPrompt);
        }

        void UpdateThreat(Consequence c)
        {
            if (c == Consequence.Push)
            {
                AddThreat(-threatPerDisrupt);
                consecutiveDisrupts++;
            }
            else
            {
                AddThreat(threatPerSuccess);
                consecutiveDisrupts = 0;
            }
        }

        /// <summary>Adds to the unbounded threat score, retaining only a zero floor.</summary>
        public void AddThreat(float amount)
        {
            threatLevel = Mathf.Max(0f, threatLevel + amount);
            OnThreatChanged?.Invoke(threatLevel);
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
            $"Your control score: {Mathf.RoundToInt(threatLevel)} — {Confidence()}.\n" +
            $"{MoodDirective(mood)}\n" +
            $"React with a single line of at most {maxWords} words. Stay in character; output only the line.";

        // ---- LLM call (streaming + fallback) ----
        async void Speak(string prompt)
        {
            if (agent == null)
                return;

            if (IsBusy || Time.time - lastSpokeAt < minSecondsBetweenLines)
            {
                queuedPrompt = prompt;
                return;
            }

            IsBusy = true;
            lastSpokeAt = Time.time;
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
            finally
            {
                IsBusy = false;
                // Update() drains queuedPrompt once the cooldown also clears.
            }
        }

        void HandleStreaming(string replySoFar)
        {
            CurrentLine = replySoFar;
            OnTauntStreaming?.Invoke(replySoFar);
        }
    }
}
