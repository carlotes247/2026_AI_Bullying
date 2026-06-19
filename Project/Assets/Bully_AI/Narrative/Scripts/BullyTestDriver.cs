using UnityEngine;

namespace Bully
{
    /// <summary>
    /// SANDBOX ONLY. Two buttons for the two player states (Push / Idle), plus a
    /// live readout of the agent's threat level and its latest line. Replace with
    /// real gameplay triggers (call brain.OnPlayerAction(...)) once the game is wired up.
    /// </summary>
    public class BullyTestDriver : MonoBehaviour
    {
        public BullyBrain brain;

        void OnGUI()
        {
            if (brain == null) return;

            int fs = Mathf.RoundToInt(Screen.height * 0.025f);   // scales with the screen
            GUI.skin.button.fontSize = fs;
            GUI.skin.label.fontSize  = fs + 6;
            GUI.skin.label.wordWrap  = true;

            float w = Mathf.Min(Screen.width * 0.6f, Screen.width - 40f);
            GUILayout.BeginArea(new Rect(20, 20, w, Screen.height - 40f));

            GUILayout.Label($"Threat level: {Mathf.RoundToInt(brain.threatLevel)}%");
            GUILayout.Space(10);

            if (GUILayout.Button("Push — disrupt the agent")) brain.OnPlayerAction(Consequence.Push);
            if (GUILayout.Button("Idle — do nothing"))        brain.OnPlayerAction(Consequence.Idle);

            GUILayout.Space(20);
            GUILayout.Label(
                brain.IsBusy ? "Agent (typing…): " + brain.CurrentLine
              : string.IsNullOrEmpty(brain.CurrentLine) ? "<press a button>"
              : "Agent: " + brain.CurrentLine);

            GUILayout.EndArea();
        }
    }
}
