using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bully
{
    /// <summary>
    /// Shows the bully's streamed response in a playful, Animal Crossing-inspired UI bubble
    /// that snaps to the agent's head and follows it around the screen.
    ///
    /// If no UI references are supplied, the component builds its own rounded bubble at
    /// runtime. It can therefore be dropped straight onto the moving head.
    /// </summary>
    public class SpeechBubble : MonoBehaviour
    {
        [Header("Wiring")]
        public BullyBrain brain;
        [Tooltip("The real animated/physics head transform. Leave empty to find WalkerRagdoll/head automatically.")]
        public Transform head;
        [Tooltip("Optional root to search beneath when the agent hierarchy is known at edit time.")]
        public Transform agentRoot;
        public RectTransform bubble;
        public TMP_Text label;
        public Camera cam;
        public GameFlow gameFlow;

        [Header("Automatic head lookup")]
        public bool autoFindHead = true;
        public string agentRootName = "WalkerRagdoll";
        public string headTransformName = "head";

        [Header("Behaviour")]
        public Vector3 worldOffset = new Vector3(0f, -2.5f, 0f);
        [Min(0f)] public float hideAfterSeconds = 4f;
        public bool keepOnScreen = true;
        public bool onlyDuringGameplay = true;

        [Header("Automatic Animal Crossing-style UI")]
        public bool autoBuildUI = true;
        public string speakerName = "AGENT";
        public Vector2 bubbleSize = new Vector2(430f, 150f);
        [Min(100f)] public float maximumBubbleHeight = 230f;
        public Color bubbleColor = new Color(1f, 0.95f, 0.78f, 1f);
        public Color outlineColor = new Color(0.25f, 0.15f, 0.10f, 1f);
        public Color nameColor = new Color(0.96f, 0.52f, 0.21f, 1f);

        float hideAt;
        float popStartedAt;
        Canvas bubbleCanvas;
        CanvasGroup canvasGroup;
        RectTransform outerTail;
        RectTransform innerTail;
        static Sprite roundedSprite;
        float nextHeadSearchAt;

        void Awake()
        {
            ResolveHead();
            if (cam == null)
                cam = Camera.main;
            if (gameFlow == null)
                gameFlow = FindFirstObjectByType<GameFlow>(FindObjectsInactive.Include);

            EnsureUI();
            if (bubble != null)
                bubble.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            ResolveHead();
            if (!brain) return;
            brain.OnTauntStreaming += Show;  // updates live as the reply streams in
            brain.OnTaunt          += Show;  // final line
        }

        void OnDisable()
        {
            if (brain != null)
            {
                brain.OnTauntStreaming -= Show;
                brain.OnTaunt -= Show;
            }

            if (bubble != null)
                bubble.gameObject.SetActive(false);
        }

        void Show(string text)
        {
            if (!CanShowBubble())
            {
                HideBubble();
                return;
            }

            if (string.IsNullOrWhiteSpace(text) && (bubble == null || !bubble.gameObject.activeSelf))
                return;

            EnsureUI();
            if (bubble == null)
                return;

            bool opening = !bubble.gameObject.activeSelf;
            if (label != null)
            {
                label.text = text;
                ResizeForText();
            }

            bubble.gameObject.SetActive(true);
            if (opening)
            {
                popStartedAt = Time.unscaledTime;
                bubble.localScale = Vector3.one * 0.72f;
                if (canvasGroup != null)
                    canvasGroup.alpha = 0f;
            }

            hideAt = hideAfterSeconds > 0f
                ? Time.unscaledTime + hideAfterSeconds
                : float.PositiveInfinity;
        }

        void LateUpdate()
        {
            if (!CanShowBubble())
            {
                HideBubble();
                return;
            }

            if (head == null && autoFindHead && Time.unscaledTime >= nextHeadSearchAt)
            {
                ResolveHead();
                nextHeadSearchAt = Time.unscaledTime + 0.5f;
            }

            if (bubble == null || !bubble.gameObject.activeSelf || head == null)
                return;

            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return;

            if (Time.unscaledTime >= hideAt)
            {
                bubble.gameObject.SetActive(false);
                return;
            }

            Vector3 headScreen = cam.WorldToScreenPoint(head.position);
            Vector3 screen = cam.WorldToScreenPoint(head.position + worldOffset);
            if (screen.z < 0f)
            {
                bubble.gameObject.SetActive(false);
                return;
            }

            if (keepOnScreen)
            {
                float scale = bubbleCanvas != null ? bubbleCanvas.scaleFactor : 1f;
                Vector2 half = bubble.rect.size * scale * 0.5f;
                const float margin = 14f;
                screen.x = Mathf.Clamp(screen.x, half.x + margin, Screen.width - half.x - margin);
                screen.y = Mathf.Clamp(screen.y, half.y + margin, Screen.height - half.y - margin);
            }

            bubble.position = screen;

            // When edge clamping moves the panel, slide its tail so it still points at the head.
            if (outerTail != null && innerTail != null)
            {
                float scale = bubbleCanvas != null ? bubbleCanvas.scaleFactor : 1f;
                float localHeadX = (headScreen.x - screen.x) / Mathf.Max(0.01f, scale);
                float limit = Mathf.Max(0f, bubble.rect.width * 0.5f - 38f);
                float tailX = Mathf.Clamp(localHeadX, -limit, limit);
                outerTail.anchoredPosition = new Vector2(tailX, 4f);
                innerTail.anchoredPosition = new Vector2(tailX, 7f);
            }

            AnimatePop();
        }

        bool CanShowBubble()
        {
            return !onlyDuringGameplay || gameFlow == null || gameFlow.IsGameplayActive;
        }

        void HideBubble()
        {
            if (bubble != null && bubble.gameObject.activeSelf)
                bubble.gameObject.SetActive(false);
        }

        void ResolveHead()
        {
            if (head != null || !autoFindHead)
                return;

            if (agentRoot != null)
            {
                head = FindDescendant(agentRoot, headTransformName);
                if (head != null)
                    return;
            }

            Transform fallback = null;
            Transform[] sceneTransforms = FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Transform candidate in sceneTransforms)
            {
                if (!string.Equals(candidate.name, headTransformName,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                fallback ??= candidate;
                if (HasNamedAncestor(candidate, agentRootName))
                {
                    head = candidate;
                    return;
                }
            }

            // A plain name match keeps the component useful with a different agent prefab.
            head = fallback;
        }

        static Transform FindDescendant(Transform root, string transformName)
        {
            foreach (Transform descendant in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(descendant.name, transformName,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return descendant;
                }
            }

            return null;
        }

        static bool HasNamedAncestor(Transform candidate, string ancestorName)
        {
            if (string.IsNullOrWhiteSpace(ancestorName))
                return true;

            for (Transform current = candidate.parent; current != null; current = current.parent)
            {
                if (string.Equals(current.name, ancestorName,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        void AnimatePop()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - popStartedAt) / 0.24f);
            float scale;
            if (t < 0.65f)
                scale = Mathf.Lerp(0.72f, 1.07f, Mathf.SmoothStep(0f, 1f, t / 0.65f));
            else
                scale = Mathf.Lerp(1.07f, 1f, Mathf.SmoothStep(0f, 1f, (t - 0.65f) / 0.35f));

            bubble.localScale = Vector3.one * scale;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 2.5f));
        }

        void EnsureUI()
        {
            if (bubble != null && label != null)
            {
                bubbleCanvas = bubble.GetComponentInParent<Canvas>();
                canvasGroup = bubble.GetComponent<CanvasGroup>();
                if (autoBuildUI && bubbleCanvas != null &&
                    bubbleCanvas.name == "Agent Speech Bubble Canvas" &&
                    bubbleCanvas.transform.parent != transform)
                {
                    bubbleCanvas.transform.SetParent(transform, false);
                }
                return;
            }

            if (!autoBuildUI)
                return;

            Sprite panelSprite = GetRoundedSprite();

            var canvasObject = new GameObject("Agent Speech Bubble Canvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.layer = 5;
            canvasObject.transform.SetParent(transform, false);
            bubbleCanvas = canvasObject.GetComponent<Canvas>();
            bubbleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            bubbleCanvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var bubbleObject = new GameObject("Agent Speech Bubble", typeof(RectTransform), typeof(CanvasGroup));
            bubbleObject.layer = 5;
            bubble = bubbleObject.GetComponent<RectTransform>();
            bubble.SetParent(canvasObject.transform, false);
            bubble.anchorMin = bubble.anchorMax = new Vector2(0.5f, 0.5f);
            bubble.pivot = new Vector2(0.5f, 0.5f);
            bubble.sizeDelta = bubbleSize;
            canvasGroup = bubbleObject.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            CreateTail("Tail Shadow", new Vector2(47f, 35f), new Vector2(3f, -1f),
                new Color(0.08f, 0.04f, 0.02f, 0.20f), out _);
            CreateTail("Tail Outline", new Vector2(43f, 32f), new Vector2(0f, 4f),
                outlineColor, out outerTail);
            CreateTail("Tail Fill", new Vector2(31f, 25f), new Vector2(0f, 7f),
                bubbleColor, out innerTail);

            CreatePanelImage("Shadow", panelSprite, new Vector2(7f, -8f), Vector2.zero,
                new Color(0.08f, 0.04f, 0.02f, 0.24f));
            CreatePanelImage("Outline", panelSprite, Vector2.zero, Vector2.zero, outlineColor);
            CreatePanelImage("Cream Fill", panelSprite, Vector2.zero, new Vector2(-10f, -10f), bubbleColor);

            RectTransform namePill = CreateUIObject("Speaker Name", bubble, new Vector2(142f, 43f));
            namePill.anchorMin = namePill.anchorMax = new Vector2(0f, 1f);
            namePill.pivot = new Vector2(0f, 0.5f);
            namePill.anchoredPosition = new Vector2(22f, 0f);
            Image pillOutline = namePill.gameObject.AddComponent<Image>();
            pillOutline.sprite = panelSprite;
            pillOutline.type = Image.Type.Sliced;
            pillOutline.color = outlineColor;
            pillOutline.raycastTarget = false;

            RectTransform pillFillRect = CreateUIObject("Name Fill", namePill, Vector2.zero);
            Stretch(pillFillRect, new Vector2(4f, 4f));
            Image pillFill = pillFillRect.gameObject.AddComponent<Image>();
            pillFill.sprite = panelSprite;
            pillFill.type = Image.Type.Sliced;
            pillFill.color = nameColor;
            pillFill.raycastTarget = false;

            RectTransform nameTextRect = CreateUIObject("Name", namePill, Vector2.zero);
            Stretch(nameTextRect, new Vector2(8f, 4f));
            TextMeshProUGUI nameText = nameTextRect.gameObject.AddComponent<TextMeshProUGUI>();
            nameText.text = string.IsNullOrWhiteSpace(speakerName) ? "AGENT" : speakerName.ToUpperInvariant();
            nameText.fontSize = 22f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            nameText.raycastTarget = false;

            RectTransform textRect = CreateUIObject("Response", bubble, Vector2.zero);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(28f, 18f);
            textRect.offsetMax = new Vector2(-28f, -35f);
            label = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = string.Empty;
            label.fontSize = 27f;
            label.color = outlineColor;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
        }

        void CreatePanelImage(string objectName, Sprite sprite, Vector2 positionOffset,
            Vector2 sizeOffset, Color color)
        {
            RectTransform rect = CreateUIObject(objectName, bubble, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = positionOffset - sizeOffset * 0.5f;
            rect.offsetMax = positionOffset + sizeOffset * 0.5f;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
        }

        void CreateTail(string objectName, Vector2 size, Vector2 position, Color color,
            out RectTransform tailRect)
        {
            tailRect = CreateUIObject(objectName, bubble, size);
            tailRect.anchorMin = tailRect.anchorMax = new Vector2(0.5f, 0f);
            tailRect.pivot = new Vector2(0.5f, 1f);
            tailRect.anchoredPosition = position;
            BubbleTailGraphic graphic = tailRect.gameObject.AddComponent<BubbleTailGraphic>();
            graphic.color = color;
            graphic.raycastTarget = false;
        }

        void ResizeForText()
        {
            if (label == null || bubble == null)
                return;

            float availableWidth = Mathf.Max(100f, bubbleSize.x - 56f);
            float preferredHeight = label.GetPreferredValues(label.text, availableWidth, 0f).y;
            float height = Mathf.Clamp(preferredHeight + 68f, bubbleSize.y, maximumBubbleHeight);
            bubble.sizeDelta = new Vector2(bubbleSize.x, height);
        }

        static RectTransform CreateUIObject(string objectName, Transform parent, Vector2 size)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.layer = 5;
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            return rect;
        }

        static void Stretch(RectTransform rect, Vector2 inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = inset;
            rect.offsetMax = -inset;
        }

        static Sprite GetRoundedSprite()
        {
            if (roundedSprite != null)
                return roundedSprite;

            const int size = 64;
            const float radius = 19f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Generated Rounded Speech Bubble",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - half) - (half - radius);
                    float qy = Mathf.Abs(y + 0.5f - half) - (half - radius);
                    float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) +
                                               Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                    float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                    float distance = outside + inside - radius;
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f - distance) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            roundedSprite.name = "Generated Rounded Speech Bubble";
            roundedSprite.hideFlags = HideFlags.HideAndDontSave;
            return roundedSprite;
        }
    }

    /// <summary>A tiny procedural triangle used by the automatic speech bubble.</summary>
    sealed class BubbleTailGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            Rect rect = GetPixelAdjustedRect();
            vertexHelper.Clear();

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = new Vector3(rect.xMin, rect.yMax);
            vertexHelper.AddVert(vertex);
            vertex.position = new Vector3(rect.xMax, rect.yMax);
            vertexHelper.AddVert(vertex);
            vertex.position = new Vector3(rect.center.x, rect.yMin);
            vertexHelper.AddVert(vertex);
            vertexHelper.AddTriangle(0, 1, 2);
        }
    }
}
