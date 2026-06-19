using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgentsExamples;
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
        public BullyBrain brain;

        [Tooltip("The moving target. Leave empty to find the scene's DynamicTarget automatically.")]
        public TargetController dynamicTarget;
        public GameManager gameManager;

        [Tooltip("Hidden during the intro, enabled when gameplay starts (e.g. BullyTestDriver).")]
        public GameObject[] gameplayObjects;

        [Header("Target success")]
        [Min(0f)] public float threatPerTargetReached = 10f;

        [Header("Automatic player status")]
        [Min(0.1f)] public float idleAfterSeconds = 3f;
        [Tooltip("Repeat interval after the first idle event, while inactivity continues.")]
        [Min(0.1f)] public float idleRepeatSeconds = 1.5f;
        [Min(0f)] public float pushDebounceSeconds = 0.5f;

        [Header("Game over")]
        [Min(0f)] public float gameOverThreat = 100f;

        [Header("Gameplay timer")]
        public bool showTimer = true;
        public Vector2 timerSize = new Vector2(230f, 64f);
        public Vector2 timerMargin = new Vector2(24f, 24f);

        [Header("Debug mode")]
        [SerializeField] bool debugMode;
        public bool showDebugToggleDuringIntro = true;
        public Vector2 debugToggleSize = new Vector2(240f, 56f);
        public Vector2 debugToggleMargin = new Vector2(24f, 24f);

        public bool IsGameplayActive { get; private set; }
        public bool IsGameOver { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public bool HasStatus { get; private set; }
        public Consequence CurrentStatus { get; private set; }
        public bool DebugMode => debugMode;
        public float IdleSecondsRemaining => IsGameplayActive
            ? Mathf.Max(0f, nextIdleAt - Time.time)
            : idleAfterSeconds;

        float lastTargetReachedAt = float.NegativeInfinity;
        float lastPushAt = float.NegativeInfinity;
        float nextIdleAt;
        readonly HashSet<Interactable> activeInteractions = new HashSet<Interactable>();
        float timeScaleBeforeGameOver = 1f;
        GUIStyle timerStyle;
        GUIStyle gameOverStyle;
        GUIStyle debugToggleStyle;

        void Awake()
        {
            ResolveReferences();
            SetGameplayActive(false);   // hide before anything renders
        }

        void OnEnable()
        {
            ResolveReferences();
            SubscribeToGameplayEvents();
        }

        void OnDisable()
        {
            UnsubscribeFromGameplayEvents();
        }

        void Start()
        {
            ResolveReferences();
            SubscribeToGameplayEvents();

            // The <<startGameplay>> command in the Yarn intro will call StartGameplay().
            if (dialogueRunner != null)
                dialogueRunner.AddCommandHandler("startGameplay", StartGameplay);
        }

        void StartGameplay()
        {
            if (IsGameplayActive || IsGameOver)
                return;

            IsGameplayActive = true;
            ElapsedSeconds = 0f;
            nextIdleAt = Time.time + idleAfterSeconds;
            activeInteractions.Clear();
            gameManager?.ResetFallState();
            SetGameplayActive(true);

            if (brain != null && brain.threatLevel >= gameOverThreat)
                EndGame();
        }

        void Update()
        {
            if (!IsGameplayActive)
                return;

            ElapsedSeconds += Time.deltaTime;
            activeInteractions.RemoveWhere(item => item == null);

            if (activeInteractions.Count > 0)
            {
                nextIdleAt = Time.time + idleAfterSeconds;
                return;
            }

            if (Time.time >= nextIdleAt)
            {
                nextIdleAt = Time.time + idleRepeatSeconds;
                ReportConsequence(Consequence.Idle);
            }
        }

        void SetGameplayActive(bool on)
        {
            foreach (var go in gameplayObjects)
                if (go != null) go.SetActive(on);
        }

        void ResolveReferences()
        {
            if (brain == null)
                brain = FindFirstObjectByType<BullyBrain>(FindObjectsInactive.Include);

            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);

            if (dynamicTarget == null)
            {
                TargetController fallback = null;
                foreach (TargetController candidate in FindObjectsByType<TargetController>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    fallback ??= candidate;
                    if (candidate.name == "DynamicTarget")
                    {
                        dynamicTarget = candidate;
                        break;
                    }
                }

                dynamicTarget ??= fallback;
            }
        }

        void SubscribeToGameplayEvents()
        {
            Interactable.InteractionStarted -= HandleInteractionStarted;
            Interactable.InteractionStarted += HandleInteractionStarted;
            Interactable.InteractionEnded -= HandleInteractionEnded;
            Interactable.InteractionEnded += HandleInteractionEnded;
            Interactable.AgentInterrupted -= HandleAgentInterrupted;
            Interactable.AgentInterrupted += HandleAgentInterrupted;
            PointerPhysics.PlayerFired -= HandlePlayerFired;
            PointerPhysics.PlayerFired += HandlePlayerFired;
            AgentSeekingProjectile.AgentHit -= HandleProjectileHit;
            AgentSeekingProjectile.AgentHit += HandleProjectileHit;

            if (brain != null)
            {
                brain.OnThreatChanged -= HandleThreatChanged;
                brain.OnThreatChanged += HandleThreatChanged;
            }

            if (gameManager != null)
            {
                gameManager.AgentFell -= HandleAgentFell;
                gameManager.AgentFell += HandleAgentFell;
            }

            if (dynamicTarget == null)
                return;

            // Remove first so Awake/OnEnable/Start cannot accidentally double-subscribe.
            dynamicTarget.onCollisionEnterEvent.RemoveListener(HandleTargetReached);
            dynamicTarget.onCollisionEnterEvent.AddListener(HandleTargetReached);
        }

        void UnsubscribeFromGameplayEvents()
        {
            Interactable.InteractionStarted -= HandleInteractionStarted;
            Interactable.InteractionEnded -= HandleInteractionEnded;
            Interactable.AgentInterrupted -= HandleAgentInterrupted;
            PointerPhysics.PlayerFired -= HandlePlayerFired;
            AgentSeekingProjectile.AgentHit -= HandleProjectileHit;

            if (brain != null)
                brain.OnThreatChanged -= HandleThreatChanged;
            if (gameManager != null)
                gameManager.AgentFell -= HandleAgentFell;
            if (dynamicTarget != null)
                dynamicTarget.onCollisionEnterEvent.RemoveListener(HandleTargetReached);
        }

        void HandleInteractionStarted(Interactable interactable)
        {
            if (!IsGameplayActive)
                return;

            activeInteractions.Add(interactable);
            nextIdleAt = Time.time + idleAfterSeconds;
        }

        void HandleInteractionEnded(Interactable interactable)
        {
            activeInteractions.Remove(interactable);
            if (!IsGameplayActive)
                return;

            nextIdleAt = Time.time + idleAfterSeconds;
        }

        void HandleAgentInterrupted(Interactable interactable)
        {
            ReportConsequence(Consequence.Push);
        }

        void HandlePlayerFired()
        {
            if (IsGameplayActive)
                nextIdleAt = Time.time + idleAfterSeconds;
        }

        void HandleProjectileHit(AgentSeekingProjectile projectile)
        {
            ReportConsequence(Consequence.Push);
        }

        void HandleAgentFell()
        {
            ReportConsequence(Consequence.Push);
        }

        void ReportConsequence(Consequence consequence)
        {
            if (!IsGameplayActive || IsGameOver || brain == null)
                return;

            if (consequence == Consequence.Push)
            {
                if (Time.time - lastPushAt < pushDebounceSeconds)
                    return;
                lastPushAt = Time.time;
                nextIdleAt = Mathf.Max(nextIdleAt, Time.time + idleRepeatSeconds);
            }

            CurrentStatus = consequence;
            HasStatus = true;
            brain.OnPlayerAction(consequence);
        }

        void HandleThreatChanged(float threat)
        {
            if (IsGameplayActive && threat >= gameOverThreat)
                EndGame();
        }

        void HandleTargetReached(Collision collision)
        {
            if (!IsGameplayActive || brain == null)
                return;

            // A ragdoll can touch with several limbs during the same physics moment.
            if (Time.time - lastTargetReachedAt < 0.25f)
                return;

            lastTargetReachedAt = Time.time;
            brain.AddThreat(threatPerTargetReached);
        }

        void EndGame()
        {
            if (IsGameOver)
                return;

            IsGameOver = true;
            IsGameplayActive = false;
            activeInteractions.Clear();
            timeScaleBeforeGameOver = Time.timeScale;
            Time.timeScale = 0f;
        }

        void OnDestroy()
        {
            if (IsGameOver)
                Time.timeScale = timeScaleBeforeGameOver;
        }

        void OnGUI()
        {
            DrawDebugToggle();

            if (showTimer && (IsGameplayActive || IsGameOver))
            {
                int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(ElapsedSeconds));
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;

                timerStyle ??= new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.18f, 0.11f, 0.08f) }
                };
                timerStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.032f), 22, 42);

                Rect timerRect = new Rect(
                    Screen.width - timerSize.x - timerMargin.x,
                    timerMargin.y,
                    timerSize.x,
                    timerSize.y);
                GUI.Box(timerRect, $"TIME  {minutes:00}:{seconds:00}", timerStyle);
            }

            if (!IsGameOver)
                return;

            gameOverStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.55f, 0.05f, 0.04f) }
            };
            gameOverStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.055f), 34, 72);
            Rect gameOverRect = new Rect(
                Screen.width * 0.5f - 240f,
                Screen.height * 0.5f - 90f,
                480f,
                180f);
            GUI.Box(gameOverRect,
                $"GAME OVER\nTHREAT {Mathf.RoundToInt(brain != null ? brain.threatLevel : gameOverThreat)}",
                gameOverStyle);
        }

        void DrawDebugToggle()
        {
            if (!showDebugToggleDuringIntro || IsGameplayActive || IsGameOver)
                return;

            debugToggleStyle ??= new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            debugToggleStyle.fontSize = Mathf.Clamp(
                Mathf.RoundToInt(Screen.height * 0.026f), 20, 34);

            Rect toggleRect = new Rect(
                Screen.width - debugToggleSize.x - debugToggleMargin.x,
                debugToggleMargin.y,
                debugToggleSize.x,
                debugToggleSize.y);

            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = debugMode
                ? new Color(0.45f, 0.9f, 0.5f)
                : new Color(0.75f, 0.75f, 0.75f);
            if (GUI.Button(toggleRect, $"DEBUG MODE: {(debugMode ? "ON" : "OFF")}",
                    debugToggleStyle))
            {
                debugMode = !debugMode;
            }
            GUI.backgroundColor = previousColor;
        }
    }
}
