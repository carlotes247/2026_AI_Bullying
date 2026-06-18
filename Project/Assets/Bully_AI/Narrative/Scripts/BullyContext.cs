using System.Text;
using UnityEngine;

namespace Bully
{
    /// <summary>
    /// The data contract between the game / RL side and the narrative side.
    /// Game code fills one of these in and hands it to BullyBrain.React().
    ///
    /// 'eventSummary' is the main, flexible field — almost any situation can be
    /// described in words, which keeps this robust to whatever the final game
    /// ends up needing. The numeric fields are just convenient extras.
    /// </summary>
    [System.Serializable]
    public class BullyContext
    {
        [TextArea] public string eventSummary = "";  // e.g. "player missed the jump and fell"
        public int    playerScore;
        public int    lossStreak;                     // rounds the player just lost in a row
        public string agentAction = "";               // what the RL agent just did, if relevant
        public int    escalation;                     // 0 = mild, higher = more savage

        /// <summary>Compact, model-friendly description of the current state.</summary>
        public string Describe()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(eventSummary)) sb.Append($"Event: {eventSummary}. ");
            sb.Append($"Player score: {playerScore}. ");
            if (lossStreak > 0) sb.Append($"Player has lost {lossStreak} in a row. ");
            if (!string.IsNullOrWhiteSpace(agentAction)) sb.Append($"You just {agentAction}. ");
            sb.Append($"Savagery level: {escalation}.");
            return sb.ToString().Trim();
        }
    }
}
