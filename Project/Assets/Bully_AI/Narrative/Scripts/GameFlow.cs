using UnityEngine;
using Yarn.Unity;

namespace Bully
{
    /// <summary>
    /// Keeps the gameplay / bully UI hidden during the intro, and switches it on
    /// when the Yarn intro reaches <<startGameplay>> (the DAY 5 "STOP AGENT" beat).
    ///
    /// Put this on its own GameObject (e.g. "GameFlow"). Assign the Dialogue Runner
    /// and the objects you want hidden until gameplay starts (e.g. BullyTestDriver).
    ///
    /// Tip: leave the LLM and BullyAgent OUT of the list, so the model can warm up
    /// in the background while the intro is playing.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        [Header("Wiring")]
        public DialogueRunner dialogueRunner;

        [Tooltip("Hidden during the intro, enabled when gameplay starts (e.g. BullyTestDriver).")]
        public GameObject[] gameplayObjects;

        void Awake()
        {
            SetGameplayActive(false);   // hide before anything renders
        }

        void Start()
        {
            // The <<startGameplay>> command in the Yarn intro will call StartGameplay().
            if (dialogueRunner != null)
                dialogueRunner.AddCommandHandler("startGameplay", StartGameplay);
        }

        void StartGameplay()
        {
            SetGameplayActive(true);
            // Later: this is also where you'd kick off the round, enable player
            // controls, etc. once the intro finishes.
        }

        void SetGameplayActive(bool on)
        {
            foreach (var go in gameplayObjects)
                if (go != null) go.SetActive(on);
        }
    }
}
