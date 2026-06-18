using UnityEngine;

namespace Bully
{
    /// <summary>
    /// SANDBOX ONLY — delete this when you wire the real game.
    ///
    /// Draws buttons that feed fake BullyContexts to BullyBrain and shows the
    /// current taunt, all via OnGUI — so you need zero scene/UI setup and it
    /// doesn't matter which Input system the project uses.
    ///
    /// Later, real game code builds a BullyContext and calls brain.React(ctx) the
    /// exact same way; this driver is the only throwaway piece.
    /// </summary>
    public class BullyTestDriver : MonoBehaviour
    {
        public BullyBrain brain;

        void OnGUI()
        {
            if (brain == null) return;

            GUI.skin.button.fontSize = 16;
            GUI.skin.label.fontSize  = 22;
            GUI.skin.label.wordWrap  = true;

            GUILayout.BeginArea(new Rect(20, 20, Mathf.Min(560f, Screen.width - 40f), Screen.height - 40f));

            if (GUILayout.Button("Player missed an easy jump  (mild)"))
                brain.React(new BullyContext {
                    eventSummary = "player missed an easy jump and fell into the pit",
                    playerScore = 120, lossStreak = 1, escalation = 1 });

            if (GUILayout.Button("Player lost to the AI, 3rd time  (savage)"))
                brain.React(new BullyContext {
                    eventSummary = "player lost the round to the AI yet again",
                    playerScore = 90, lossStreak = 3, agentAction = "cornered the player", escalation = 3 });

            if (GUILayout.Button("Player finally scored  (grudging)"))
                brain.React(new BullyContext {
                    eventSummary = "player finally managed to score a point",
                    playerScore = 200, lossStreak = 0, escalation = 0 });

            GUILayout.Space(20);
            GUILayout.Label(
                brain.IsBusy
                    ? "Bully (typing…): " + brain.CurrentLine
                    : string.IsNullOrEmpty(brain.CurrentLine)
                        ? "<press a button to provoke the bully>"
                        : "Bully: " + brain.CurrentLine);

            GUILayout.EndArea();
        }
    }
}
