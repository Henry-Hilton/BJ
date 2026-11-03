using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Blackjack
{
    // All decoration is UI geometry; card artwork comes from the project's supplied pack.
    /// <summary>Builds the menu and handles navigation, transitions and profile confirmation.</summary>
    public sealed partial class BlackjackMainMenu : MonoBehaviour
    {
        readonly Color gold = new Color(.84f, .69f, .43f);
        readonly Color cream = new Color(.96f, .93f, .85f);
        readonly Color muted = new Color(.57f, .68f, .63f);
        BlackjackTable table;
        RectTransform canvas, page, rules;
        CanvasGroup group, rulesGroup, canvasGroup;
        TMP_FontAsset font;
        TextMeshProUGUI playLabel;
        UnityEngine.UI.Button playButton;
        bool hasPlayed;
        Coroutine entrance;
        bool transitioning;
        readonly UnityEngine.UI.Button[] difficultyButtons = new UnityEngine.UI.Button[3];
        TextMeshProUGUI difficultyHint;
        bool helpFromTable;
        TextMeshProUGUI rulesBackLabel;
        public bool IsOpen => canvas != null && canvas.gameObject.activeSelf;

        /// <summary>Creates the menu canvas and its pages, then opens the menu.</summary>
        /// <param name="owner">Table controller used for profile, difficulty and mode changes.</param>
        /// <param name="art">Artwork asset providing the menu's card and chip sprites.</param>
        public void Initialize(BlackjackTable owner, BlackjackArtwork art)
        {
            table = owner;
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            var go = new GameObject("Main Menu Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            go.transform.SetParent(transform, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            go.GetComponent<Canvas>().sortingOrder = 20;
            var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            canvas = go.GetComponent<RectTransform>();
            canvasGroup = go.AddComponent<CanvasGroup>();
            var background = Panel(canvas, "Emerald background", 0, 0, 1280, 800, new Color(.025f,.075f,.065f));
            background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one;
            background.offsetMin = background.offsetMax = Vector2.zero;
            background.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            page = Panel(canvas, "Menu composition", 0, 0, 1280, 800, Color.clear);
            group = page.gameObject.AddComponent<CanvasGroup>();

            // Fine framing and diagonal linework give depth without bitmap textures.
            Panel(page, "Top rule", 56, 97, 1168, 1, new Color(.84f,.69f,.43f,.24f));
            Panel(page, "Bottom rule", 56, 713, 1168, 1, new Color(.84f,.69f,.43f,.24f));
            Text(page, "B / J", 58, 38, 100, 36, 25, gold);
            Text(page, "THE PRACTICE COLLECTION", 180, 44, 420, 24, 12, muted, 3);
            Text(page, "PLAY CHIPS ONLY", 600, 44, 340, 24, 12, muted, 2, TextAlignmentOptions.Right);
            BuildMenuAvatar();
            Button(page, "Edit profile", 1040, 36, 184, 36, false, ShowProfileEditor);
            for (int i = 0; i < 12; i++)
            {
                var line = Panel(page, "Architectural line", 760 + i * 43, 125, 1, 530, new Color(.84f,.69f,.43f,.035f));
                line.localRotation = Quaternion.Euler(0, 0, -24);
            }
            Text(page, "TAKE A SEAT. FIND YOUR EDGE.", 80, 163, 560, 26, 13, gold, 3);
            Text(page, "BLACK\nJACK", 73, 202, 600, 228, 108, cream, 5);
            Panel(page, "Title accent", 80, 450, 56, 3, gold);
            Text(page, "A classic hand. A little instinct.", 80, 474, 520, 30, 21, muted);
            difficultyHint = Text(page, "DEALER AI / CUSTOM RULES", 80, 511, 540, 18, 10, muted, 1);
            for (int i = 0; i < 3; i++)
            {
                var difficulty = (DealerDifficulty)i;
                difficultyButtons[i] = Button(page, difficulty.ToString(), 80 + i * 116, 534, 110, 30, false, () =>
                {
                    if (table.SetDifficulty(difficulty)) RefreshDifficulty();
                });
            }
            playButton = Button(page, "Take a seat", 80, 574, 342, 62, true, Play);
            playLabel = playButton.GetComponentInChildren<TextMeshProUGUI>();
            Button(page, "Tutorial / guided practice", 80, 646, 342, 28, false, StartTutorial);
            Button(page, "How to play", 80, 682, 166, 24, false, ShowRules);
            Button(page, "Quit game", 262, 682, 160, 24, false, Quit);

            Text(page, "THE PERFECT PAIR", 752, 163, 410, 24, 12, gold, 4, TextAlignmentOptions.Center);
            Card(page, art.GetCard(new Card(13, 1)), 758, 286, -16);
            Card(page, art.GetCard(new Card(1, 0)), 925, 263, 13);
            for (int i = 0; i < 3; i++)
            {
                var chip = Panel(page, "Supplied chip", 1055 - i * 44, 535 + i * 4, 60, 60, Color.white);
                var img = chip.GetComponent<UnityEngine.UI.Image>(); img.sprite = art.chips[i]; img.preserveAspect = true;
            }
            Text(page, "21", 758, 610, 110, 54, 44, gold);
            Text(page, "SIX DECKS. ONE CLASSIC.", 874, 618, 330, 24, 12, cream, 2);
            Text(page, "Blackjack pays 3:2  /  Custom dealer AI", 874, 645, 342, 24, 13, muted);
            Text(page, "NO PRESSURE. JUST THE NEXT HAND.", 58, 739, 720, 25, 11, muted, 2);
            Text(page, "1,000 CHIPS TO BEGIN", 880, 739, 344, 25, 11, gold, 2, TextAlignmentOptions.Right);
            BuildRules(); BuildUsernamePrompt(); Open();
        }

        /// <summary>Places an animated decorative card and shadow on the menu.</summary>
        /// <param name="parent">UI container and coordinate origin. Changing it can move the element, change draw order or affect input.</param>
        /// <param name="sprite">Image to display. A different sprite triggers a flip in a card view, or changes the artwork on the decorative menu card.</param>
        /// <param name="x">Horizontal offset in reference UI units. Larger values move right.</param>
        /// <param name="y">Downward offset in reference UI units. Larger values move down.</param>
        /// <param name="angle">Z rotation in degrees. Changing it tilts the card and shifts its idle animation phase.</param>
        void Card(Transform parent, Sprite sprite, float x, float y, float angle)
        {
            var pivot = Panel(parent, "Card presentation", x, y, 174, 252, Color.clear);
            pivot.pivot = new Vector2(.5f, .5f);
            pivot.anchoredPosition += new Vector2(87, -126);
            var shadow = Panel(pivot, "Card shadow", 10, 14, 174, 252, new Color(0,0,0,.4f));
            shadow.GetComponent<UnityEngine.UI.Image>().sprite = sprite;
            var face = Panel(pivot, "Supplied card face", 0, 0, 174, 252, Color.white);
            face.GetComponent<UnityEngine.UI.Image>().sprite = sprite;
            face.GetComponent<UnityEngine.UI.Image>().preserveAspect = true;
            pivot.localRotation = Quaternion.Euler(0,0,angle);
            var motion = pivot.gameObject.AddComponent<BlackjackMenuMotion>(); motion.Floating = true; motion.Phase = angle;
        }
        /// <summary>Builds the shared help page with rules, controls and return button.</summary>
        void BuildRules()
        {
            rules = Panel(canvas, "How to play", 0, 0, 1280, 800, new Color(.025f,.075f,.065f));
            rulesGroup = rules.gameObject.AddComponent<CanvasGroup>();
            Text(rules, "THE TABLE GUIDE", 150, 110, 980, 30, 14, gold, 4);
            Text(rules, "A good hand starts here.", 150, 165, 980, 70, 44, cream);
            string[] titles = { "01   THE BASICS", "02   YOUR TURN", "03   RESULTS & DEALER" };
            string[] bodies = {
                "Beat the dealer's total without going over 21. Going over is a bust and loses your bet.\n\nCards 2–10 have their face value. J, Q and K count as 10. An ace counts as 1 or 11, whichever keeps you under 22.",
                "Add chips to your bet, then Deal. Hit draws a card; Stand keeps your hand and lets the dealer play.\n\nDouble doubles your wager, draws one final card, then stands. Only available on your first two cards with enough chips.",
                "A two-card 21 is blackjack and pays 3:2. Other wins pay 1:1. A tie (push) returns your bet; dealer busts also win.\n\nEasy stands on 15+. Medium stands on 17+. Hard adapts to visible hands, without peeking at upcoming cards."
            };
            for (int i = 0; i < 3; i++)
            {
                float x = 150 + i * 340;
                Panel(rules, "Rounded guide panel", x - 18, 254, 322, 324, new Color(.055f,.125f,.10f), true, 18);
                Panel(rules, "Column rule", x, 270, 292, 1, gold);
                Text(rules, titles[i], x, 286, 292, 28, 14, gold, 1);
                Text(rules, bodies[i], x, 326, 286, 236, 18, cream);
            }
            Text(rules, "Start with 1,000 chips / Bets in multiples of 10 / No split, surrender, or insurance", 150, 582, 980, 28, 14, muted);
            Text(rules, "SPACE  Deal     H  Hit     S  Stand     D  Double     ESC  Menu / back", 150, 615, 980, 28, 14, muted);
            rulesBackLabel = Button(rules, "Back to menu", 150, 656, 280, 58, true, HideRules).GetComponentInChildren<TextMeshProUGUI>();
            rules.gameObject.SetActive(false);
        }
        /// <summary>Opens the main menu, hides the table and refreshes profile and difficulty displays.</summary>
        public void Open()
        {
            if (transitioning) return;
            usernamePanel.gameObject.SetActive(false);
            helpFromTable = false; rulesBackLabel.text = "BACK TO MENU   >";
            table.ShowTable(false); canvas.gameObject.SetActive(true);
            canvasGroup.alpha = 1; canvasGroup.blocksRaycasts = true;
            rules.gameObject.SetActive(false); page.gameObject.SetActive(true); playButton.Select();
            RefreshDifficulty();
            RefreshMenuAvatar();
            playLabel.text = hasPlayed ? "RESUME YOUR HAND   >" : "TAKE A SEAT   >";
            if (entrance != null) StopCoroutine(entrance);
            entrance = StartCoroutine(Reveal());
        }
        /// <summary>Updates the selected difficulty and disables changes while the round or tutorial forbids them.</summary>
        void RefreshDifficulty()
        {
            difficultyHint.text = table.CanChangeDifficulty ? "DEALER AI / CUSTOM RULES" : "DEALER AI / FINISH THIS HAND TO CHANGE";
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                var button = difficultyButtons[i];
                bool selected = i == (int)table.Difficulty;
                button.interactable = table.CanChangeDifficulty;
                button.GetComponent<UnityEngine.UI.Image>().color = selected ? gold : new Color(.06f,.13f,.11f);
                button.GetComponentInChildren<TextMeshProUGUI>().color = selected ? new Color(.035f,.085f,.065f) : cream;
            }
        }
        /// <summary>Opens the username prompt if needed, otherwise enters or resumes the table.</summary>
        public void Play()
        {
            if (transitioning) return;
            if (!EnsureUsername(false)) return;
            entrance = StartCoroutine(EnterTable());
        }
        /// <summary>Confirms the profile and enters isolated guided practice.</summary>
        public void StartTutorial()
        {
            if (transitioning) return;
            if (!EnsureUsername(true)) return;
            table.BeginTutorial();
            entrance = StartCoroutine(EnterTable());
        }
        /// <summary>Handles Escape according to the current view and ignores it during transitions.</summary>
        public void Toggle() { if (transitioning) return; if (usernamePanel.gameObject.activeSelf) { CancelUsername(); return; } if (!IsOpen) Open(); else if (rules.gameObject.activeSelf) HideRules(); else Play(); }
        /// <summary>Transitions from the main menu to the rules page.</summary>
        public void ShowRules() { if (!transitioning) entrance = StartCoroutine(SwitchPage(true)); }
        /// <summary>Returns to the menu or directly to the table depending on where help was opened.</summary>
        public void HideRules() { if (transitioning) return; if (helpFromTable) Play(); else entrance = StartCoroutine(SwitchPage(false)); }
        /// <summary>Opens the rules page from gameplay without resetting the hand.</summary>
        public void OpenHelp()
        {
            if (transitioning || IsOpen) return;
            helpFromTable = true; rulesBackLabel.text = "BACK TO GAME   >";
            table.ShowTable(false); canvas.gameObject.SetActive(true);
            canvasGroup.alpha = 1; canvasGroup.blocksRaycasts = true;
            page.gameObject.SetActive(false); rules.gameObject.SetActive(true);
            entrance = StartCoroutine(RevealHelp());
        }
        /// <summary>Fades in the help screen and restores interaction and selection.</summary>
        IEnumerator RevealHelp()
        {
            transitioning = true; canvasGroup.interactable = false;
            yield return Fade(rulesGroup, 0, 1, .25f);
            canvasGroup.interactable = true; transitioning = false;
            rules.GetComponentInChildren<UnityEngine.UI.Button>().Select(); entrance = null;
        }
        /// <summary>Transitions between the main menu and rules page with input blocked.</summary>
        /// <param name="guide">True transitions to rules; false transitions back to the menu.</param>
        IEnumerator SwitchPage(bool guide)
        {
            transitioning = true; canvasGroup.interactable = false;
            EventSystem.current?.SetSelectedGameObject(null);
            var outgoing = guide ? group : rulesGroup;
            var incoming = guide ? rulesGroup : group;
            yield return Fade(outgoing, 1, 0, .16f);
            outgoing.gameObject.SetActive(false); incoming.gameObject.SetActive(true);
            yield return Fade(incoming, 0, 1, .28f);
            canvasGroup.interactable = true; transitioning = false;
            if (guide) rules.GetComponentInChildren<UnityEngine.UI.Button>().Select(); else playButton.Select();
            entrance = null;
        }
        /// <summary>Shows the table, clears menu focus and fades out the menu canvas.</summary>
        IEnumerator EnterTable()
        {
            transitioning = true; if (!table.TutorialActive) hasPlayed = true; canvasGroup.interactable = false;
            EventSystem.current?.SetSelectedGameObject(null);
            table.ShowTable(true);
            yield return Fade(canvasGroup, 1, 0, .3f);
            canvas.gameObject.SetActive(false); transitioning = false; entrance = null;
        }
        /// <summary>Interpolates canvas opacity over unscaled seconds.</summary>
        /// <param name="target">Canvas group whose opacity is animated. Its children fade together.</param>
        /// <param name="from">Starting alpha from 0 (transparent) to 1 (opaque).</param>
        /// <param name="to">Final alpha from 0 (transparent) to 1 (opaque).</param>
        /// <param name="duration">Fade duration in unscaled seconds. Larger positive values slow the transition.</param>
        static IEnumerator Fade(CanvasGroup target, float from, float to, float duration)
        {
            target.alpha = from;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                target.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t / duration)); yield return null;
            }
            target.alpha = to;
        }
        /// <summary>Fades and slides the main page into position while blocking input.</summary>
        IEnumerator Reveal()
        {
            transitioning = true; canvasGroup.interactable = false; group.alpha = 0;
            Vector2 origin = new Vector2(-640, 400);
            for (float t = 0; t < .45f; t += Time.unscaledDeltaTime)
            {
                float progress = 1 - Mathf.Pow(1 - t / .45f, 3);
                group.alpha = progress; page.anchoredPosition = origin + Vector2.down * (16 * (1 - progress)); yield return null;
            }
            page.anchoredPosition = origin; group.alpha = 1; canvasGroup.interactable = true; transitioning = false; playButton.Select(); entrance = null;
        }
        /// <summary>Stops Play mode in the Unity Editor or quits a standalone build.</summary>
        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        /// <summary>Creates a positioned menu rectangle with optional background artwork.</summary>
        /// <param name="parent">UI container and coordinate origin. Changing it can move the element, change draw order or affect input.</param>
        /// <param name="name">Hierarchy object name. Changing it primarily affects debugging and name-based lookups.</param>
        /// <param name="x">Horizontal offset in reference UI units. Larger values move right.</param>
        /// <param name="y">Downward offset in reference UI units. Larger values move down.</param>
        /// <param name="w">Width in reference UI units. Smaller widths can crop content or wrap labels.</param>
        /// <param name="h">Height in reference UI units. Smaller heights can clip text or reduce the click target.</param>
        /// <param name="color">Background tint for a shape or button, or glyph tint for a label. Alpha controls opacity.</param>
        /// <param name="addImage">True creates a background graphic; false creates a layout-only rectangle.</param>
        /// <param name="radius">Corner radius in reference UI units. Zero uses a normal rectangle; positive values round the corners.</param>
        RectTransform Panel(Transform parent, string name, float x, float y, float w, float h, Color color, bool addImage = true, float radius = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = parent == canvas ? new Vector2(.5f,.5f) : new Vector2(0,1);
            rect.pivot = new Vector2(0,1);
            rect.anchoredPosition = parent == canvas ? new Vector2(x - 640, 400 - y) : new Vector2(x,-y);
            rect.sizeDelta = new Vector2(w,h);
            if (addImage)
            {
                UnityEngine.UI.Image image = radius > 0 ? go.AddComponent<RoundedMenuGraphic>() : go.AddComponent<UnityEngine.UI.Image>();
                if (image is RoundedMenuGraphic rounded) rounded.Radius = radius;
                image.color = color; image.raycastTarget = false;
            }
            return rect;
        }
        /// <summary>Creates a TextMeshPro label at a menu-relative location.</summary>
        /// <param name="parent">UI container and coordinate origin. Changing it can move the element, change draw order or affect input.</param>
        /// <param name="value">Displayed string; changing it affects the label only and longer strings may wrap.</param>
        /// <param name="x">Horizontal offset in reference UI units. Larger values move right.</param>
        /// <param name="y">Downward offset in reference UI units. Larger values move down.</param>
        /// <param name="w">Width in reference UI units. Smaller widths can crop content or wrap labels.</param>
        /// <param name="h">Height in reference UI units. Smaller heights can clip text or reduce the click target.</param>
        /// <param name="size">Font size in reference UI units. Increasing it can reduce fit within the label bounds.</param>
        /// <param name="color">Background tint for a shape or button, or glyph tint for a label. Alpha controls opacity.</param>
        /// <param name="spacing">Extra spacing between glyphs. Larger values spread the text and can cause wrapping.</param>
        /// <param name="align">Text alignment within the label; changing it shifts the glyphs, not the label bounds.</param>
        TextMeshProUGUI Text(Transform parent, string value, float x, float y, float w, float h, float size, Color color, float spacing = 0, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var rect = Panel(parent, value, x,y,w,h,Color.clear,false);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color;
            text.characterSpacing = spacing; text.alignment = align; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal; return text;
        }
        /// <summary>Creates a styled menu button that invokes the supplied action.</summary>
        /// <param name="parent">UI container and coordinate origin. Changing it can move the element, change draw order or affect input.</param>
        /// <param name="label">Button caption; changing it does not change the callback.</param>
        /// <param name="x">Horizontal offset in reference UI units. Larger values move right.</param>
        /// <param name="y">Downward offset in reference UI units. Larger values move down.</param>
        /// <param name="w">Width in reference UI units. Smaller widths can crop content or wrap labels.</param>
        /// <param name="h">Height in reference UI units. Smaller heights can clip text or reduce the click target.</param>
        /// <param name="primary">True selects the prominent gold style and arrow; false selects the secondary style.</param>
        /// <param name="action">Callback invoked when the button is clicked; changing it changes the menu operation.</param>
        UnityEngine.UI.Button Button(Transform parent, string label, float x, float y, float w, float h, bool primary, UnityEngine.Events.UnityAction action)
        {
            var rect = Panel(parent, label, x,y,w,h,primary ? gold : new Color(.06f,.13f,.11f), true, primary ? 14 : 9);
            var image = rect.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1.15f,1.15f,1.15f); colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(.75f,.75f,.75f); colors.fadeDuration = .15f; button.colors = colors;
            Text(rect, label.ToUpperInvariant() + (primary ? "   >" : ""), 16,0,w-32,h,primary ? 17 : 13,primary ? new Color(.035f,.085f,.065f) : cream,2,TextAlignmentOptions.MidlineLeft);
            button.onClick.AddListener(action);
            rect.gameObject.AddComponent<BlackjackMenuMotion>(); return button;
        }
    }
}
