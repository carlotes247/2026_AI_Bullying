using UnityEngine;

namespace Bully
{
    /// <summary>
    /// Read-only debug HUD. The former Push / Idle controls now show the latest
    /// automatically detected gameplay status; they no longer trigger the model.
    /// </summary>
    public class BullyTestDriver : MonoBehaviour
    {
        public BullyBrain brain;
        public GameFlow gameFlow;

        void Awake()
        {
            if (brain == null)
                brain = FindFirstObjectByType<BullyBrain>(FindObjectsInactive.Include);
            if (gameFlow == null)
                gameFlow = FindFirstObjectByType<GameFlow>(FindObjectsInactive.Include);
        }

        void OnGUI()
        {
            if (gameFlow == null)
                gameFlow = FindFirstObjectByType<GameFlow>(FindObjectsInactive.Include);

            if (brain == null || gameFlow == null)
                return;

            int fs = Mathf.RoundToInt(Screen.height * 0.025f);   // scales with the screen
            GUI.skin.button.fontSize = fs;
            GUI.skin.label.fontSize  = fs + 6;
            GUI.skin.label.wordWrap  = true;

            float w = Mathf.Min(Screen.width * 0.6f, Screen.width - 40f);
            GUILayout.BeginArea(new Rect(20, 20, w, Screen.height - 40f));

            GUILayout.Label($"Threat score: {Mathf.RoundToInt(brain.threatLevel)}");

            if (!gameFlow.DebugMode)
            {
                GUILayout.EndArea();
                return;
            }

            int initialMin = Mathf.Min(brain.initialThreatRange.x, brain.initialThreatRange.y);
            int initialMax = Mathf.Max(brain.initialThreatRange.x, brain.initialThreatRange.y);

            GUILayout.Label("THREAT RULES");
            GUILayout.Label($"Start: random {initialMin}–{initialMax}");
            GUILayout.Label(
                $"Idle: +{brain.threatPerSuccess:0.#} after {gameFlow.idleAfterSeconds:0.#}s, " +
                $"then every {gameFlow.idleRepeatSeconds:0.#}s");
            GUILayout.Label($"Dynamic target reached: +{gameFlow.threatPerTargetReached:0.#}");
            GUILayout.Label($"Push / agent fall: −{brain.threatPerDisrupt:0.#}");
            GUILayout.Label($"Game over: threat ≥ {gameFlow.gameOverThreat:0.#}");
            GUILayout.Space(10);

            bool pushActive = gameFlow != null && gameFlow.HasStatus &&
                gameFlow.CurrentStatus == Consequence.Push;
            bool idleActive = gameFlow != null && gameFlow.HasStatus &&
                gameFlow.CurrentStatus == Consequence.Idle;

            bool previousEnabled = GUI.enabled;
            GUI.enabled = false;
            GUILayout.Button(pushActive ? "● PUSH — current status" : "○ PUSH");
            GUILayout.Button(idleActive ? "● IDLE — current status" : "○ IDLE");
            GUI.enabled = previousEnabled;

            if (gameFlow != null && gameFlow.IsGameplayActive)
                GUILayout.Label($"Next idle in: {gameFlow.IdleSecondsRemaining:0.0}s");

            GUILayout.Space(20);
            GUILayout.Label(
                brain.IsBusy ? "Agent (typing…): " + brain.CurrentLine
              : string.IsNullOrEmpty(brain.CurrentLine) ? "<waiting for an automatic status>"
              : "Agent: " + brain.CurrentLine);

            GUILayout.Space(16);
            GUILayout.Label(string.IsNullOrEmpty(brain.CurrentPrompt)
                ? "Prompt: <none yet>"
                : "Prompt:\n" + brain.CurrentPrompt);

            GUILayout.EndArea();
        }
    }
}
