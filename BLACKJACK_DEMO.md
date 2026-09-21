# Blackjack demo

Open this project in Unity 6000.3.24f1, open `Assets/Scenes/SampleScene.unity`, and press Play. The table creates itself at runtime; no Inspector wiring or imported art is required. Click the Game view before using keyboard shortcuts.

- Start with 1,000 play chips and a selected bet of 50.
- Click chips to add to the next bet; Clear sets it to zero.
- Deal / Space: start a hand. Hit / H: draw. Stand / S: let the dealer play.
- Double / D: double the original wager, draw exactly one card, then stand. Available only on the first two cards with enough chips.
- Deal Again repeats your selected wager (limited to the available bank).
- Reset Bank starts a fresh session between hands.

Rules: six shuffled decks, dealer stands on soft and hard 17, aces count as 1 or 11, natural blackjack pays 3:2, ordinary wins pay 1:1, ties return the bet. Bets are multiples of 10. The dealer checks for blackjack immediately. No split, surrender, or insurance in this prototype. Bank and statistics are session-only.

Cards, the card back, and chips reference the supplied `Assets/poker_cards_chips_2d` pack through `Assets/Blackjack/Resources/BlackjackArtwork.asset`. Card faces use Set B for readable center ranks. No AI-generated artwork is used. The table is built from Unity UI rectangles and text; the short click sound is synthesized in code. The UI fits landscape windows using a 1280 x 800 reference layout.

Use **Blackjack > Run Rule Checks** for deterministic edge-case checks plus 500 simulated hands. Use **Blackjack > Build Windows Demo** to generate `Builds/Blackjack/Blackjack.exe`. Keep the entire build folder together when moving it to another computer.

Implementation: `Assets/Blackjack/Scripts/BlackjackRound.cs` holds game rules; `BlackjackTable.cs` creates the interface and handles input and dealer timing. `Assets/Blackjack/Editor/BlackjackDemoTools.cs` contains checks and the build menu.
