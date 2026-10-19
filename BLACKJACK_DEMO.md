# Blackjack demo

Open this project in Unity 6000.3.26f1, open `Assets/Scenes/SampleScene.unity`, and press Play. The main menu and table create themselves at runtime; no Inspector wiring is required. Choose Take a Seat to play, or How to Play for the rules. Escape or the table's Menu button opens the menu and preserves your hand; Resume returns to play. The dealer waits while the menu is open. Click the Game view before using keyboard shortcuts.

The main menu uses emerald, ivory, and gold UI geometry with cards and chips from the supplied pack. Typography uses TextMeshPro's bundled Liberation Sans (license included under `Assets/TextMesh Pro/Fonts`). No AI-generated textures or artwork are used. Arrow keys and Enter navigate the menu. Quit Game exits the player, or stops Play mode in the Editor.

- Start with 1,000 play chips and a selected bet of 50.
- Before entering regular play or tutorial for the first time each session, confirm a username (2–16 characters: letters, numbers, spaces, underscores, or hyphens). Continue or Enter starts your chosen mode; Back or Escape returns to the menu. The name appears above your hand and is saved locally, prefilled for confirmation next session. Resuming an existing session does not prompt again.
- Choose **Tutorial / Guided Practice** in the main menu for four interactive lessons: bet and Hit, Stand, Double, then aces and natural blackjack. Follow the coach panel and enabled controls; click **Next Lesson** (or Space) after each result. **Finish Tutorial**, **Exit Tutorial**, or Escape returns to the menu. Help remains available during practice.
- Tutorial uses fixed teaching hands, the Medium dealer, and a separate practice bank. Your normal bank, wager, statistics, difficulty, shoe, and any in-progress hand are restored when you finish or leave; a saved dealer turn resumes when you return to the regular table. Practice chips do not transfer.
- Click **? How to Play** in the table header for the blackjack guide (goal, card values, actions, payouts, ties, and dealer difficulty rules). **Back to Game** or Escape returns directly to the same hand. The dealer waits while help is open.
- The menu uses rounded UI geometry, eased hover/press feedback, a soft slide-in, floating cards, and fades between screens. Animation uses unscaled time; transitions block repeated actions and clicks through to the table.
- The table shares the menu's emerald/gold styling and TextMeshPro typography, with rounded hand areas, bank/status panels, and betting controls. Card views are reused: new cards slide and fade into place, hands smoothly recenter, and the dealer's hole card flips when revealed. Existing supplied sprites are unchanged.
- Click chips to add to the next bet; Clear sets it to zero.
- Deal / Space: start a hand. Hit / H: draw. Stand / S: let the dealer play.
- Double / D: double the original wager, draw exactly one card, then stand. Available only on the first two cards with enough chips.
- Deal Again repeats your selected wager (limited to the available bank).
- Reset Bank starts a fresh session between hands.

Rules: six shuffled decks, aces count as 1 or 11, natural blackjack pays 3:2, ordinary wins pay 1:1, ties return the bet. Bets are multiples of 10. The dealer checks for blackjack immediately. No split, surrender, or insurance in this prototype. Bank and statistics are session-only.

Choose Easy, Medium, or Hard in the main menu between hands. The selection is saved locally and preserved when resetting the bank; Medium is the default. These are custom dealer rules:

- Easy hits below 15 and stands on soft or hard 15 and above.
- Medium hits below 17 and stands on soft or hard 17 and above (the original behavior).
- Hard compares standing with drawing to maximize expected win/loss outcome against the player's visible final hand. It accounts for soft aces and future decisions using fixed rank probabilities (1/13 for ace through nine, 4/13 for ten-value cards). It never inspects the shoe or upcoming cards, counts remaining ranks, or changes the shuffle or payouts. It may stand below 17 when ahead or hit above 17 when behind. Difficulty describes behavior, not a guarantee for any individual hand.

The table displays the active dealer policy. Difficulty is locked during player and dealer turns, including when the menu is open. The runtime smoke check explicitly uses Medium for its fixed payout scenario.

Cards, the card back, and chips reference the supplied `Assets/poker_cards_chips_2d` pack through `Assets/Blackjack/Resources/BlackjackArtwork.asset`. Card faces use Set B for readable center ranks. No AI-generated artwork is used. The table is built from Unity UI rectangles and text; the short click sound is synthesized in code. The UI fits landscape windows using a 1280 x 800 reference layout.

Use **Blackjack > Run Rule Checks** for deterministic edge-case and AI checks plus 1,500 simulated hands across all three difficulties. Use **Blackjack > Build Windows Demo** to generate `Builds/Blackjack/Blackjack.exe`. Keep the entire build folder together when moving it to another computer.

Implementation: `Assets/Blackjack/Scripts/BlackjackRound.cs` holds game rules; `BlackjackTable.cs` creates the interface and handles input and dealer timing. `Assets/Blackjack/Editor/BlackjackDemoTools.cs` contains checks and the build menu.
