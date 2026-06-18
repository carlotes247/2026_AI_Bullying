using UnityEngine;
using TMPro;

namespace Bully
{
    /// <summary>
    /// Shows the bully's taunt in a UI bubble that follows the head's position on screen.
    /// Put this on a separate controller object (NOT on the bubble panel itself — it toggles
    /// the panel on/off, and a script can't run while its own GameObject is disabled).
    ///
    /// Requires a Screen Space - Overlay Canvas (so the bubble can be placed by screen pixels).
    /// </summary>
    public class SpeechBubble : MonoBehaviour
    {
        [Header("Wiring")]
        public BullyBrain    brain;    // the BullyAgent's BullyBrain
        public Transform     head;     // the ball standing in for the head
        public RectTransform bubble;   // the bubble PANEL (a different object than this one)
        public TMP_Text      label;    // the TMP text inside the bubble
        public Camera        cam;      // leave empty to use Camera.main

        [Header("Behaviour")]
        public Vector3 worldOffset     = new Vector3(0f, 1.2f, 0f); // how far above the head
        public float   hideAfterSeconds = 4f;

        float hideAt;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (bubble) bubble.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (!brain) return;
            brain.OnTauntStreaming += Show;  // updates live as the reply streams in
            brain.OnTaunt          += Show;  // final line
        }

        void OnDisable()
        {
            if (!brain) return;
            brain.OnTauntStreaming -= Show;
            brain.OnTaunt          -= Show;
        }

        void Show(string text)
        {
            if (label)  label.text = text;
            if (bubble) bubble.gameObject.SetActive(true);
            hideAt = Time.time + hideAfterSeconds;
        }

        void LateUpdate()
        {
            if (!bubble || !bubble.gameObject.activeSelf || !head || !cam) return;

            if (Time.time >= hideAt) { bubble.gameObject.SetActive(false); return; }

            // Place the bubble where the head is on screen, nudged upward.
            Vector3 screen = cam.WorldToScreenPoint(head.position + worldOffset);
            if (screen.z < 0f) { bubble.gameObject.SetActive(false); return; } // head is behind camera
            bubble.position = screen;
        }
    }
}
