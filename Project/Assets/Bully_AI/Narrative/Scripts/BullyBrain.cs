using System;
using UnityEngine;
using LLMUnity;

namespace Bully
{
    /// <summary>
    /// Turns a BullyContext into an LLM-generated taunt.
    /// This is the piece that ports into the real game UNCHANGED.
    ///
    /// The seam is React(BullyContext):
    ///   • now   — the BullyTestDriver calls it with fake contexts
    ///   • later — your game code or an ML-Agents callback builds a BullyContext
    ///             from real state and calls the same method. Nothing here changes.
    /// </summary>
    public class BullyBrain : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("The LLMAgent component. Its System Prompt field holds the bully persona.")]
        public LLMAgent agent;

        [Header("Generation")]
        [Tooltip("Word cap suggested to the model. Keep taunts short.")]
        public int maxWords = 20;

        [Tooltip("Used if the model returns nothing or errors out.")]
        public string[] fallbackLines =
        {
            "Wow. Just... wow.",
            "Was that supposed to be impressive?",
            "I've seen tutorials go better than this.",
        };

        /// <summary>Fires with the final taunt — UI / voice / animation subscribe here.</summary>
        public event Action<string> OnTaunt;
        /// <summary>Fires repeatedly with the reply-so-far while it streams in.</summary>
        public event Action<string> OnTauntStreaming;

        public string CurrentLine { get; private set; } = "";
        public bool   IsBusy      { get; private set; }

        async void Start()
        {
            if (agent == null) { Debug.LogError("[BullyBrain] No LLMAgent assigned."); return; }
            // Process the persona up front so the first taunt isn't slow.
            try { await agent.Warmup(); }
            catch (Exception e) { Debug.LogWarning($"[BullyBrain] Warmup skipped: {e.Message}"); }
        }

        /// <summary>
        /// Public entry point. Call from the test driver now, from real game code later.
        /// Builds a prompt from the context and asks the LLM for one line.
        /// </summary>
        public void React(BullyContext context)
        {
            if (agent == null || context == null) return;

            // Simple policy: one taunt at a time — ignore new triggers while generating.
            // (Alternative: call agent.CancelRequests() then proceed, so the newest event wins.)
            if (IsBusy) return;

            CurrentLine = "";
            IsBusy = true;

            // addToHistory = false: each taunt is an independent reaction to game state,
            // not a running chat. Continuity (escalation, recent events) is fed explicitly
            // through the context, which keeps the model controllable and the prompt small.
            _ = agent.Chat(BuildPrompt(context), HandleStreaming, HandleCompleted, false);
        }

        string BuildPrompt(BullyContext context) =>
            $"Current game situation:\n{context.Describe()}\n\n" +
            $"React with a single taunt of at most {maxWords} words. " +
            $"Stay in character. Output only the line — no quotes, no explanation.";

        void HandleStreaming(string replySoFar)
        {
            CurrentLine = replySoFar;
            OnTauntStreaming?.Invoke(replySoFar);
        }

        void HandleCompleted()
        {
            IsBusy = false;
            string line = (CurrentLine ?? "").Trim();
            if (string.IsNullOrEmpty(line) && fallbackLines.Length > 0)
                line = fallbackLines[UnityEngine.Random.Range(0, fallbackLines.Length)];
            CurrentLine = line;
            OnTaunt?.Invoke(line);
        }
    }
}
