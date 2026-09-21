using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Blackjack
{
    public sealed class BlackjackTable : MonoBehaviour
    {
        BlackjackRound round = new BlackjackRound();
        int wager = 50;
        Font font;
        RectTransform root, dealerCards, playerCards;
        Text bankText, betText, statusText, dealerText, playerText, statsText;
        Button deal, hit, stand, doubleDown, clear, reset;
        readonly System.Collections.Generic.List<Button> chips = new System.Collections.Generic.List<Button>();
        readonly int[] values = { 10, 50, 100, 250 };
        readonly Color gold = new Color(.94f, .76f, .39f);
        readonly Color ink = new Color(.06f, .11f, .14f);
        Coroutine dealerRoutine;
        BlackjackArtwork artwork;
        AudioSource audioSource;
        AudioClip click;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindFirstObjectByType<BlackjackTable>() == null) new GameObject("Blackjack Table").AddComponent<BlackjackTable>();
        }
        void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            artwork = Resources.Load<BlackjackArtwork>("BlackjackArtwork");
            if (artwork == null || artwork.cards.Length != 52 || artwork.chips.Length != 4 || artwork.cardBack == null)
                throw new System.InvalidOperationException("BlackjackArtwork must reference the supplied card and chip sprites.");
            foreach (var sprite in artwork.cards) if (sprite == null) throw new System.InvalidOperationException("A supplied card sprite is missing.");
            foreach (var sprite in artwork.chips) if (sprite == null) throw new System.InvalidOperationException("A supplied chip sprite is missing.");
            Build(); MakeAudio(); Refresh();
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--blackjack-smoke") >= 0) StartCoroutine(SmokeCheck());
        }
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.hKey.wasPressedThisFrame) Act("hit");
            else if (keyboard.sKey.wasPressedThisFrame) Act("stand");
            else if (keyboard.dKey.wasPressedThisFrame) Act("double");
            else if (keyboard.spaceKey.wasPressedThisFrame) Act("deal");
        }
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
            Box(root, "Wood rail", 30, 105, 1220, 505, new Color(.24f, .16f, .10f));
            Box(root, "Gold trim", 40, 115, 1200, 485, gold);
            Box(root, "Green felt", 44, 119, 1192, 477, new Color(.045f, .28f, .22f));
            Label(root, "BLACKJACK", 44, 25, 340, 44, 32, gold, TextAnchor.MiddleLeft);
            Label(root, "THE PRACTICE TABLE  /  PLAY CHIPS", 46, 69, 450, 20, 13, Color.white, TextAnchor.MiddleLeft);
            bankText = Label(root, "", 850, 25, 380, 40, 26, gold, TextAnchor.MiddleRight);
            statsText = Label(root, "", 850, 69, 380, 20, 14, Color.white, TextAnchor.MiddleRight);
            dealerText = Label(root, "", 80, 133, 550, 32, 19, gold, TextAnchor.MiddleLeft);
            dealerCards = Box(root, "Dealer hand", 0, 174, 1280, 143, Color.clear);
            Label(root, "BLACKJACK PAYS 3:2    /    DEALER STANDS ON ALL 17s", 280, 326, 720, 24, 15, new Color(.56f, .72f, .62f));
            playerText = Label(root, "", 80, 364, 600, 30, 19, gold, TextAnchor.MiddleLeft);
            playerCards = Box(root, "Player hand", 0, 405, 1280, 143, Color.clear);
            Label(root, "6 DECKS  •  ACE = 1 OR 11  •  NO SPLIT / INSURANCE", 320, 566, 640, 18, 12, new Color(.65f, .77f, .69f));
            statusText = Label(root, "", 40, 618, 1200, 34, 22, Color.white);
            betText = Label(root, "", 45, 666, 220, 28, 18, gold, TextAnchor.MiddleLeft);
            for (int i = 0; i < values.Length; i++)
            {
                int value = values[i];
                var chip = MakeButton(root, "+" + value, 46 + i * 69, 705, 58, 58, Color.white, () => { wager = Mathf.Min(round.Bank / 10 * 10, wager + value); Refresh(); });
                chip.GetComponent<Image>().sprite = artwork.chips[i];
                chip.GetComponent<Image>().preserveAspect = true;
                chips.Add(chip);
            }
            clear = MakeButton(root, "CLEAR", 330, 715, 95, 42, new Color(.2f,.27f,.3f), () => { wager = 0; Refresh(); });
            deal = MakeButton(root, "DEAL  [SPACE]", 459, 684, 185, 64, gold, () => Act("deal"), ink);
            hit = MakeButton(root, "HIT  [H]", 660, 684, 148, 64, new Color(.18f,.43f,.37f), () => Act("hit"));
            stand = MakeButton(root, "STAND  [S]", 820, 684, 162, 64, new Color(.18f,.43f,.37f), () => Act("stand"));
            doubleDown = MakeButton(root, "DOUBLE  [D]", 994, 684, 185, 64, new Color(.18f,.43f,.37f), () => Act("double"));
            reset = MakeButton(root, "RESET BANK", 1070, 757, 145, 27, new Color(.2f,.27f,.3f), ResetBank);
            Label(root, "Bet in multiples of 10. Double: one final card for twice the bet.", 450, 755, 605, 26, 12, new Color(.65f,.72f,.75f));
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }
        void Act(string action)
        {
            if (action == "deal") { if (!round.Deal(wager)) return; }
            else if (action == "hit") round.Hit();
            else if (action == "stand") round.Stand();
            else if (action == "double") round.Double();
            Refresh();
            if (round.State == BlackjackRound.Phase.Dealer && dealerRoutine == null) dealerRoutine = StartCoroutine(DealerTurn());
        }
        IEnumerator DealerTurn()
        {
            while (round.State == BlackjackRound.Phase.Dealer)
            {
                yield return new WaitForSeconds(.7f);
                round.DealerStep(); PlayClick(); Refresh();
            }
            dealerRoutine = null;
        }
        void ResetBank() { if (round.State == BlackjackRound.Phase.Player || round.State == BlackjackRound.Phase.Dealer) return; round = new BlackjackRound(); wager = 50; Refresh(); }
        void Refresh()
        {
            bool betting = round.State == BlackjackRound.Phase.Betting || round.State == BlackjackRound.Phase.Complete;
            bool playing = round.State == BlackjackRound.Phase.Player;
            if (betting) wager = Mathf.Min(wager, round.Bank / 10 * 10);
            bankText.text = "BANK   " + round.Bank.ToString("N0");
            statsText.text = "HANDS  " + round.Hands + "     WINS  " + round.Wins;
            betText.text = (betting ? "NEXT BET   " + wager : "ON TABLE   " + round.Bet);
            statusText.text = betting && round.Bank < 10 ? round.Message + "  Reset bank to play again." : round.Message;
            dealerText.text = "DEALER" + (round.Dealer.Count == 0 ? "" : playing ? "   /   " + round.Dealer[0].Value + " + ?" : "   /   " + BlackjackRound.Total(round.Dealer));
            playerText.text = "YOUR HAND" + (round.Player.Count == 0 ? "" : "   /   " + BlackjackRound.Total(round.Player));
            deal.interactable = betting && wager >= 10 && wager <= round.Bank;
            deal.GetComponentInChildren<Text>().text = round.Hands == 0 ? "DEAL  [SPACE]" : "DEAL AGAIN";
            hit.interactable = stand.interactable = playing;
            doubleDown.interactable = round.CanDouble; clear.interactable = reset.interactable = betting;
            for (int i = 0; i < chips.Count; i++) chips[i].interactable = betting && wager < round.Bank / 10 * 10;
            DrawHand(dealerCards, round.Dealer, playing); DrawHand(playerCards, round.Player, false);
        }
        void DrawHand(RectTransform parent, System.Collections.Generic.List<Card> cards, bool hidden)
        {
            foreach (Transform child in parent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            if (cards.Count == 0) { Label(parent, "—", 580, 40, 120, 50, 30, new Color(.35f,.55f,.44f)); return; }
            float spacing = Mathf.Min(110, 990f / Mathf.Max(1, cards.Count - 1));
            float left = (1280 - (cards.Count - 1) * spacing - 94) / 2;
            for (int i = 0; i < cards.Count; i++)
            {
                float x = left + i * spacing;
                var shadow = Box(parent, "Card shadow", x + 4, 5, 94, 136, new Color(0,0,0,.25f)).GetComponent<Image>();
                shadow.sprite = artwork.cardBack; shadow.preserveAspect = true;
                var face = Box(parent, "Card " + i, x, 0, 94, 136, Color.white);
                var cardImage = face.GetComponent<Image>();
                cardImage.sprite = hidden && i == 1 ? artwork.cardBack : artwork.GetCard(cards[i]);
                cardImage.preserveAspect = true;
            }
        }
        RectTransform Box(Transform parent, string name, float x, float y, float w, float h, Color color, bool image = true)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f); rect.pivot = new Vector2(0,1);
            // Root uses a fixed 1280x800 layout. Child panels use their own local upper left.
            if (parent == root) rect.anchoredPosition = new Vector2(x - 640, 400 - y);
            else { rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.anchoredPosition = new Vector2(x,-y); }
            rect.sizeDelta = new Vector2(w,h); if (image) { var img = go.AddComponent<Image>(); img.color = color; img.raycastTarget = false; } return rect;
        }
        Text Label(Transform parent, string text, float x, float y, float w, float h, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rect = Box(parent, text, x,y,w,h,Color.clear,false);
            var label = rect.gameObject.AddComponent<Text>(); label.font = font; label.text = text; label.fontSize = size; label.color = color; label.alignment = align;
            label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Wrap; return label;
        }
        Button MakeButton(Transform parent, string text, float x, float y, float w, float h, Color color, UnityEngine.Events.UnityAction action, Color? textColor = null)
        {
            var rect = Box(parent,text,x,y,w,h,color); rect.GetComponent<Image>().raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(1.15f,1.15f,1.15f); colors.disabledColor = new Color(.4f,.4f,.4f,.6f); button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label(rect, text, 2, 0, w-4,h,16,textColor ?? Color.white);
            button.onClick.AddListener(() => { PlayClick(); action(); }); return button;
        }
        void MakeAudio()
        {
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.volume = .15f;
            click = AudioClip.Create("Chip tap",2205,1,44100,false); var samples = new float[2205];
            for (int i=0;i<samples.Length;i++) samples[i] = Mathf.Sin(i * .16f) * Mathf.Exp(-i / 280f);
            click.SetData(samples,0);
        }
        void PlayClick() { if (audioSource != null) audioSource.PlayOneShot(click); }
        IEnumerator SmokeCheck()
        {
            // Let the player splash screen finish before collecting rendered evidence.
            yield return new WaitForSeconds(5f);
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
        void OnDestroy() { if (click != null) Destroy(click); }
    }
}
