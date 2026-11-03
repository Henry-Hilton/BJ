using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;

namespace Blackjack
{
    /// <summary>Builds the table and connects user input, rules, audio and card presentation.</summary>
    public sealed partial class BlackjackTable : MonoBehaviour
    {
        BlackjackRound round = new BlackjackRound();
        int wager = 50;
        TMP_FontAsset font;
        RectTransform root, dealerCards, playerCards;
        TextMeshProUGUI bankText, betText, statusText, dealerText, playerText, statsText;
        TextMeshProUGUI dealerHint, playerHint;
        TextMeshProUGUI dealerRule;
        public BlackjackPlayerProfile Profile { get; private set; } = new BlackjackPlayerProfile();
        public string PlayerName => string.IsNullOrEmpty(Profile.displayName) ? "Player" : Profile.displayName;
        BlackjackAvatarGraphic playerAvatar;
        TextMeshProUGUI playerInitials;
        /// <summary>Sets the displayed name on the current profile and refreshes the table without saving preferences.</summary>
        /// <param name="value">Display name to use for this session; the normal form validates and saves separately.</param>
        public void SetPlayerName(string value) { Profile.displayName = value; Refresh(); }
        /// <summary>Copies and normalizes the supplied profile before refreshing the table. It does not save to disk.</summary>
        /// <param name="value">Source profile to copy. Different data changes identity and avatar display without altering the hand.</param>
        public void ApplyProfile(BlackjackPlayerProfile value) { Profile = value.Copy(); Profile.Normalize(); Refresh(); }
        public DealerDifficulty Difficulty => round.Difficulty;
        public bool CanChangeDifficulty => !TutorialActive && round.CanChangeDifficulty;
        /// <summary>Applies and saves an allowed difficulty, then refreshes the UI. Returns false during tutorial or an active hand.</summary>
        /// <param name="difficulty">Dealer policy enum. A different mode changes drawing strategy; enum numbers also back the saved preference and menu order.</param>
        public bool SetDifficulty(DealerDifficulty difficulty)
        {
            if (TutorialActive || !round.SetDifficulty(difficulty)) return false;
            PlayerPrefs.SetInt("Blackjack.DealerDifficulty", (int)difficulty); PlayerPrefs.Save();
            Refresh(); return true;
        }
        int displayedHand = -1;
        Button deal, hit, stand, doubleDown, clear, reset;
        readonly System.Collections.Generic.List<Button> chips = new System.Collections.Generic.List<Button>();
        readonly int[] values = { 10, 50, 100, 250 };
        readonly Color gold = new Color(.84f, .69f, .43f);
        readonly Color ink = new Color(.025f, .075f, .065f);
        readonly Color muted = new Color(.57f, .68f, .63f);
        readonly Color cream = new Color(.96f, .93f, .85f);
        Coroutine dealerRoutine;
        BlackjackArtwork artwork;
        AudioSource audioSource;
        AudioClip click;
        BlackjackMainMenu menu;
        public bool MenuOpen => menu != null && menu.IsOpen;
        /// <summary>Shows or hides the table canvas without replacing the game state.</summary>
        /// <param name="show">True activates the table canvas; false hides it. The round is retained either way.</param>
        public void ShowTable(bool show) { root.gameObject.SetActive(show); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        /// <summary>Creates a runtime table when the loaded scene has no active table.</summary>
        static void Bootstrap()
        {
            if (FindFirstObjectByType<BlackjackTable>() == null) new GameObject("Blackjack Table").AddComponent<BlackjackTable>();
        }
        /// <summary>Loads fonts, profile, preferences and artwork; validates art and builds the table and menu.</summary>
        void Start()
        {
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            Profile = BlackjackPlayerProfile.Load();
            round.SetDifficulty((DealerDifficulty)PlayerPrefs.GetInt("Blackjack.DealerDifficulty", (int)DealerDifficulty.Medium));
            artwork = Resources.Load<BlackjackArtwork>("BlackjackArtwork");
            if (artwork == null || artwork.cards.Length != 52 || artwork.chips.Length != 4 || artwork.cardBack == null)
                throw new System.InvalidOperationException("BlackjackArtwork must reference the supplied card and chip sprites.");
            foreach (var sprite in artwork.cards) if (sprite == null) throw new System.InvalidOperationException("A supplied card sprite is missing.");
            foreach (var sprite in artwork.chips) if (sprite == null) throw new System.InvalidOperationException("A supplied chip sprite is missing.");
            Build(); MakeAudio(); Refresh();
            menu = gameObject.AddComponent<BlackjackMainMenu>();
            menu.Initialize(this, artwork);
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--blackjack-smoke") >= 0) StartCoroutine(SmokeCheck());
        }
        /// <summary>Routes keyboard actions. The open menu intercepts table shortcuts, and Escape closes or opens the appropriate view.</summary>
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { if (TutorialActive && !MenuOpen) EndTutorial(); else menu.Toggle(); return; }
            if (MenuOpen) return;
            if (keyboard.hKey.wasPressedThisFrame) Act("hit");
            else if (keyboard.sKey.wasPressedThisFrame) Act("stand");
            else if (keyboard.dKey.wasPressedThisFrame) Act("double");
            else if (keyboard.spaceKey.wasPressedThisFrame) { if (TutorialActive && tutorialNext.interactable) AdvanceTutorial(); else Act("deal"); }
        }
        /// <summary>Creates the table canvas, status labels, chips, action buttons and tutorial UI.</summary>
        void Build()
        {
            var canvasObject = new GameObject("Blackjack Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root = canvasObject.GetComponent<RectTransform>();
            var background = Box(root, "Background", 0, 0, 1280, 800, ink);
            // Stretch only the background; the centered reference layout stays readable at other aspect ratios.
            background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one; background.pivot = new Vector2(.5f,.5f); background.anchoredPosition = Vector2.zero; background.sizeDelta = Vector2.zero;
            Box(root, "Table border", 40, 110, 1200, 484, new Color(.84f,.69f,.43f,.4f), true, 30);
            Box(root, "Emerald felt", 42, 112, 1196, 480, new Color(.045f,.19f,.145f), true, 28);
            Box(root, "Dealer zone", 64, 132, 1152, 190, new Color(.025f,.11f,.085f,.5f), true, 22);
            Label(root, "B / J", 44, 26, 110, 38, 28, gold, TextAnchor.MiddleLeft);
            Label(root, "THE PRACTICE TABLE", 170, 27, 370, 30, 16, cream, TextAnchor.MiddleLeft);
            Label(root, "SIX DECKS  /  PLAY CHIPS ONLY", 170, 58, 370, 20, 11, muted, TextAnchor.MiddleLeft);
            Box(root, "Bank panel", 916, 20, 324, 70, new Color(.055f,.13f,.10f), true, 16);
            bankText = Label(root, "", 936, 23, 284, 36, 25, gold, TextAnchor.MiddleRight);
            statsText = Label(root, "", 936, 62, 284, 18, 11, muted, TextAnchor.MiddleRight);
            menuButton = MakeButton(root, "MENU  [ESC]", 730, 35, 150, 40, new Color(.07f,.15f,.12f), () => { if (TutorialActive) EndTutorial(); else menu.Open(); });
            MakeButton(root, "?  HOW TO PLAY", 552, 35, 162, 40, new Color(.07f,.15f,.12f), () => menu.OpenHelp());
            dealerText = Label(root, "", 88, 148, 420, 28, 18, gold, TextAnchor.MiddleLeft);
            dealerRule = Label(root, "", 840, 151, 350, 22, 11, muted, TextAnchor.MiddleRight);
            dealerCards = Box(root, "Dealer hand", 0, 181, 1280, 132, Color.clear);
            dealerHint = Label(root, "THE NEXT HAND IS WAITING", 440, 226, 400, 32, 13, muted);
            Label(root, "BLACKJACK PAYS 3:2", 440, 328, 400, 26, 13, gold);
            Box(root, "Left divider", 100, 342, 310, 1, new Color(.84f,.69f,.43f,.2f));
            Box(root, "Right divider", 870, 342, 310, 1, new Color(.84f,.69f,.43f,.2f));
            var avatarRect = Box(root, "Player avatar", 88, 356, 44, 44, Color.clear, false);
            playerAvatar = avatarRect.gameObject.AddComponent<BlackjackAvatarGraphic>(); playerAvatar.raycastTarget = false;
            playerInitials = Label(avatarRect, "", 0, 0, 44, 44, 17, cream);
            playerText = Label(root, "", 146, 364, 590, 28, 18, cream, TextAnchor.MiddleLeft);
            playerText.enableAutoSizing = true; playerText.fontSizeMin = 12; playerText.fontSizeMax = 18;
            Label(root, "ACE = 1 OR 11", 890, 367, 300, 22, 11, muted, TextAnchor.MiddleRight);
            playerCards = Box(root, "Player hand", 0, 405, 1280, 132, Color.clear);
            playerHint = Label(root, "Choose your wager below, then deal.", 400, 452, 480, 32, 17, muted);
            Label(root, "YOUR NEXT MOVE STARTS HERE", 440, 557, 400, 18, 10, muted);
            Box(root, "Round status", 246, 605, 788, 42, new Color(.055f,.13f,.10f), true, 13);
            statusText = Label(root, "", 260, 609, 760, 34, 17, cream);
            Box(root, "Bet tray", 40, 662, 390, 116, new Color(.055f,.13f,.10f), true, 18);
            betText = Label(root, "", 60, 671, 260, 25, 16, gold, TextAnchor.MiddleLeft);
            for (int i = 0; i < values.Length; i++)
            {
                int value = values[i];
                var chip = MakeButton(root, "+" + value, 59 + i * 69, 705, 56, 56, Color.white, () => AddWager(value), cream, false);
                chip.GetComponent<Image>().sprite = artwork.chips[i];
                chip.GetComponent<Image>().preserveAspect = true;
                chips.Add(chip);
            }
            clear = MakeButton(root, "CLEAR", 340, 714, 72, 36, new Color(.09f,.19f,.15f), () => { wager = 0; Refresh(); });
            deal = MakeButton(root, "DEAL  [SPACE]", 454, 678, 216, 62, gold, () => Act("deal"), ink);
            hit = MakeButton(root, "HIT  [H]", 686, 678, 154, 62, new Color(.12f,.29f,.22f), () => Act("hit"));
            stand = MakeButton(root, "STAND  [S]", 856, 678, 166, 62, new Color(.12f,.29f,.22f), () => Act("stand"));
            doubleDown = MakeButton(root, "DOUBLE  [D]", 1038, 678, 202, 62, new Color(.12f,.29f,.22f), () => Act("double"));
            reset = MakeButton(root, "RESET BANK", 1100, 755, 140, 27, new Color(.055f,.13f,.10f), ResetBank);
            Label(root, "BET IN MULTIPLES OF 10   /   DOUBLE TAKES ONE FINAL CARD", 454, 751, 630, 28, 10, muted, TextAnchor.MiddleLeft);
            BuildTutorial();
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }
        /// <summary>Dispatches a table command, respects tutorial restrictions, refreshes the UI and starts dealer timing if needed.</summary>
        /// <param name="action">Requested command: deal, hit, stand or double. The tutorial gates these commands according to its current step.</param>
        void Act(string action)
        {
            if (TutorialActive && !TutorialAllows(action)) return;
            if (action == "deal") { if (!round.Deal(wager)) return; }
            else if (action == "hit") round.Hit();
            else if (action == "stand") round.Stand();
            else if (action == "double") round.Double();
            if (TutorialActive) TutorialAction(action);
            Refresh();
            if (round.State == BlackjackRound.Phase.Dealer && dealerRoutine == null) dealerRoutine = StartCoroutine(DealerTurn());
        }
        /// <summary>Waits between dealer steps and waits while the menu is open. Changing the delay changes pacing only.</summary>
        IEnumerator DealerTurn()
        {
            while (round.State == BlackjackRound.Phase.Dealer)
            {
                yield return new WaitForSeconds(.7f);
                while (MenuOpen) yield return null;
                round.DealerStep(); PlayClick(); Refresh();
            }
            dealerRoutine = null;
        }
        /// <summary>Creates a fresh round outside active hands, retaining difficulty but resetting bank, stats, shoe and selected wager.</summary>
        void ResetBank() { if (!round.CanChangeDifficulty) return; var difficulty = round.Difficulty; round = new BlackjackRound(); round.SetDifficulty(difficulty); wager = 50; Refresh(); }
        /// <summary>Updates the display and control availability from the rules model. Clears stale card views when the hand changes.</summary>
        void Refresh()
        {
            bool betting = round.State == BlackjackRound.Phase.Betting || round.State == BlackjackRound.Phase.Complete;
            bool playing = round.State == BlackjackRound.Phase.Player;
            if (betting) wager = Mathf.Min(wager, round.Bank / 10 * 10);
            bankText.text = "BANK   " + round.Bank.ToString("N0");
            dealerRule.text = BlackjackDealerAI.Rule(round.Difficulty);
            statsText.text = "HANDS  " + round.Hands + "     WINS  " + round.Wins;
            betText.text = (betting ? "NEXT BET   " + wager : "ON TABLE   " + round.Bet);
            statusText.text = betting && round.Bank < 10 ? round.Message + "  Reset bank to play again." : round.Message;
            dealerText.text = "DEALER" + (round.Dealer.Count == 0 ? "" : playing ? "   /   " + round.Dealer[0].Value + " + ?" : "   /   " + BlackjackRound.Total(round.Dealer));
            playerText.richText = false;
            playerAvatar.Apply(Profile); playerInitials.text = Profile.Initials; playerInitials.color = BlackjackAvatarGraphic.InitialColor(Profile);
            playerText.text = PlayerName + " / YOUR HAND" + (round.Player.Count == 0 ? "" : "   /   " + BlackjackRound.Total(round.Player));
            deal.interactable = betting && wager >= 10 && wager <= round.Bank;
            deal.GetComponentInChildren<TextMeshProUGUI>().text = round.Hands == 0 ? "DEAL  [SPACE]" : "DEAL AGAIN";
            hit.interactable = stand.interactable = playing;
            doubleDown.interactable = round.CanDouble; clear.interactable = reset.interactable = betting;
            for (int i = 0; i < chips.Count; i++) chips[i].interactable = betting && wager < round.Bank / 10 * 10;
            if (displayedHand != round.Hands)
            {
                foreach (Transform child in dealerCards) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
                foreach (Transform child in playerCards) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
                // Detach immediately so the next render does not reuse objects pending destruction.
                dealerCards.DetachChildren(); playerCards.DetachChildren(); displayedHand = round.Hands;
            }
            statusText.color = round.State == BlackjackRound.Phase.Complete ? gold : cream;
            dealerHint.gameObject.SetActive(round.Dealer.Count == 0);
            playerHint.gameObject.SetActive(round.Player.Count == 0);
            DrawHand(dealerCards, round.Dealer, playing); DrawHand(playerCards, round.Player, false);
            RefreshTutorial();
        }
        /// <summary>Lays out the cards and reuses existing card views. Hiding a card changes its sprite, not the stored rank.</summary>
        /// <param name="parent">UI container and coordinate origin. Changing it can move the element, change draw order or affect input.</param>
        /// <param name="cards">Cards to render in hand order. Changing the list changes the displayed cards and spacing.</param>
        /// <param name="hidden">True shows the card back at index 1; false shows all faces.</param>
        void DrawHand(RectTransform parent, System.Collections.Generic.List<Card> cards, bool hidden)
        {
            if (cards.Count == 0) return;
            float spacing = Mathf.Min(110, 990f / Mathf.Max(1, cards.Count - 1));
            float left = (1280 - (cards.Count - 1) * spacing - 94) / 2;
            for (int i = 0; i < cards.Count; i++)
            {
                float x = left + i * spacing;
                Sprite sprite = hidden && i == 1 ? artwork.cardBack : artwork.GetCard(cards[i]);
                BlackjackCardMotion motion;
                if (i >= parent.childCount)
                {
                    var card = Box(parent, "Card " + i, x, 0, 90, 130, Color.clear);
                    Box(card, "Soft shadow", 4, 7, 90, 130, new Color(0,0,0,.28f), true, 7);
                    var face = Box(card, "Supplied card", 0, 0, 90, 130, Color.white).GetComponent<Image>();
                    face.sprite = sprite; face.preserveAspect = true;
                    motion = card.gameObject.AddComponent<BlackjackCardMotion>();
                    motion.Initialize(face, new Vector2(x, 0), i * .055f);
                }
                else motion = parent.GetChild(i).GetComponent<BlackjackCardMotion>();
                motion.Set(new Vector2(x, 0), sprite);
            }
        }
        /// <summary>Creates a positioned rectangular UI object with an optional background.</summary>
        /// <param name="parent">UI container and coordinate origin. Changing it can move the element, change draw order or affect input.</param>
        /// <param name="name">Hierarchy object name. Changing it primarily affects debugging and name-based lookups.</param>
        /// <param name="x">Horizontal offset in reference UI units. Larger values move right.</param>
        /// <param name="y">Downward offset in reference UI units. Larger values move down.</param>
        /// <param name="w">Width in reference UI units. Smaller widths can crop content or wrap labels.</param>
        /// <param name="h">Height in reference UI units. Smaller heights can clip text or reduce the click target.</param>
        /// <param name="color">Background tint for a shape or button, or glyph tint for a label. Alpha controls opacity.</param>
        /// <param name="image">Whether to add a background graphic. False leaves a layout-only rectangle.</param>
        /// <param name="radius">Corner radius in reference UI units. Zero uses a normal rectangle; positive values round the corners.</param>
        RectTransform Box(Transform parent, string name, float x, float y, float w, float h, Color color, bool image = true, float radius = 0)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f); rect.pivot = new Vector2(0,1);
            // Root uses a fixed 1280x800 layout. Child panels use their own local upper left.
            if (parent == root) rect.anchoredPosition = new Vector2(x - 640, 400 - y);
            else { rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.anchoredPosition = new Vector2(x,-y); }
            rect.sizeDelta = new Vector2(w,h);
            if (image)
            {
                Image img = radius > 0 ? go.AddComponent<RoundedMenuGraphic>() : go.AddComponent<Image>();
                if (img is RoundedMenuGraphic rounded) rounded.Radius = radius;
                img.color = color; img.raycastTarget = false;
            }
            return rect;
        }
        /// <summary>Creates a TextMeshPro label with a specific alignment and bounds.</summary>
        /// <param name="parent">UI container and coordinate origin. Changing it can move the element, change draw order or affect input.</param>
        /// <param name="text">Text displayed to the player. Longer text can wrap or clip in fixed bounds.</param>
        /// <param name="x">Horizontal offset in reference UI units. Larger values move right.</param>
        /// <param name="y">Downward offset in reference UI units. Larger values move down.</param>
        /// <param name="w">Width in reference UI units. Smaller widths can crop content or wrap labels.</param>
        /// <param name="h">Height in reference UI units. Smaller heights can clip text or reduce the click target.</param>
        /// <param name="size">Font size in reference UI units. Increasing it can reduce fit within the label bounds.</param>
        /// <param name="color">Background tint for a shape or button, or glyph tint for a label. Alpha controls opacity.</param>
        /// <param name="align">Text alignment within the label; changing it shifts the glyphs, not the label bounds.</param>
        TextMeshProUGUI Label(Transform parent, string text, float x, float y, float w, float h, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rect = Box(parent, text, x,y,w,h,Color.clear,false);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = text; label.fontSize = size; label.color = color;
            label.alignment = align == TextAnchor.MiddleLeft ? TextAlignmentOptions.MidlineLeft : align == TextAnchor.MiddleRight ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.Midline;
            label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.Normal; return label;
        }
        /// <summary>Creates a clickable table button, its label and feedback; plays a click before invoking its action.</summary>
        /// <param name="parent">UI container and coordinate origin. Changing it can move the element, change draw order or affect input.</param>
        /// <param name="text">Text displayed to the player. Longer text can wrap or clip in fixed bounds.</param>
        /// <param name="x">Horizontal offset in reference UI units. Larger values move right.</param>
        /// <param name="y">Downward offset in reference UI units. Larger values move down.</param>
        /// <param name="w">Width in reference UI units. Smaller widths can crop content or wrap labels.</param>
        /// <param name="h">Height in reference UI units. Smaller heights can clip text or reduce the click target.</param>
        /// <param name="color">Background tint for a shape or button, or glyph tint for a label. Alpha controls opacity.</param>
        /// <param name="action">Callback invoked after the click sound; changing it changes what the button does.</param>
        /// <param name="textColor">Optional label tint. Null uses the table default.</param>
        /// <param name="rounded">True uses rounded geometry; false uses a standard Image, as needed for sprite chips.</param>
        Button MakeButton(Transform parent, string text, float x, float y, float w, float h, Color color, UnityEngine.Events.UnityAction action, Color? textColor = null, bool rounded = true)
        {
            var rect = Box(parent,text,x,y,w,h,color,true,rounded ? 12 : 0); rect.GetComponent<Image>().raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(1.15f,1.15f,1.15f); colors.disabledColor = new Color(.65f,.65f,.65f,.8f); colors.fadeDuration = .15f; button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label(rect, text, 2, 0, w-4,h,h < 42 ? 11 : 15,textColor ?? cream);
            rect.gameObject.AddComponent<BlackjackMenuMotion>();
            button.onClick.AddListener(() => { PlayClick(); action(); }); return button;
        }
        /// <summary>Creates a short synthesized click and configures playback.</summary>
        void MakeAudio()
        {
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.volume = .15f;
            click = AudioClip.Create("Chip tap",2205,1,44100,false); var samples = new float[2205];
            for (int i=0;i<samples.Length;i++) samples[i] = Mathf.Sin(i * .16f) * Mathf.Exp(-i / 280f);
            click.SetData(samples,0);
        }
        /// <summary>Plays the existing click clip if audio has been initialized.</summary>
        void PlayClick() { if (audioSource != null) audioSource.PlayOneShot(click); }
        /// <summary>Runs the fixed scripted scenario, captures screenshots and exits with success or failure.</summary>
        IEnumerator SmokeCheck()
        {
            // Let the player splash screen finish before collecting rendered evidence.
            yield return new WaitForSeconds(5f);
            menu.UseSmokeProfile();
            menu.Play();
            yield return new WaitForSeconds(.4f);
            round.SetDifficulty(DealerDifficulty.Medium);
            round.SetTestShoe(new Card(5,1),new Card(10,0),new Card(6,2),new Card(7,3),new Card(10,1));
            deal.onClick.Invoke();
            yield return new WaitForSeconds(.3f);
            ScreenCapture.CaptureScreenshot("blackjack-table.png");
            yield return new WaitForSeconds(.5f);
            doubleDown.onClick.Invoke();
            yield return new WaitForSeconds(1.5f);
            bool passed = round.State == BlackjackRound.Phase.Complete && round.Bank == 1100 && deal.interactable && !hit.interactable;
            ScreenCapture.CaptureScreenshot("blackjack-result.png");
            Debug.Log(passed ? "BLACKJACK RUNTIME SMOKE PASS: deal, double, timed dealer, payout, UI states." : "BLACKJACK RUNTIME SMOKE FAILED");
            yield return new WaitForSeconds(.5f);
            Application.Quit(passed ? 0 : 1);
        }
        /// <summary>Releases the generated audio clip.</summary>
        void OnDestroy() { if (click != null) Destroy(click); }
    }
}
