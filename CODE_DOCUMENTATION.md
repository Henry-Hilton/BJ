# Blackjack code and tuning guide

The 13 project-owned C# files have XML comments for their types, methods, and method parameters, including private helpers and Unity callbacks. This guide explains how the files work together and which values need to change together. Unity packages, TextMeshPro resources, and imported artwork are dependencies.

## How a hand runs

The table loads artwork and the local profile and opens the menu. After profile confirmation, the menu opens the regular table or a separate practice round. Buttons and keyboard shortcuts go through `BlackjackTable.Act` to `BlackjackRound`, which owns the shoe, chips, hands, and turn phase. The table displays that state and advances the dealer through a coroutine. Card animations change the display only.

## Source map

| File | Responsibility |
| --- | --- |
| [BlackjackRound](Assets/Blackjack/Scripts/BlackjackRound.cs) | Card values, scoring, shuffle, legal actions and chip accounting |
| [BlackjackDealerAI](Assets/Blackjack/Scripts/BlackjackDealerAI.cs) | Easy/Medium thresholds and Hard's expected-outcome calculation |
| [BlackjackTable](Assets/Blackjack/Scripts/BlackjackTable.cs) | Table layout, keyboard and button input, dealer pacing, audio and smoke run |
| [BlackjackTutorial](Assets/Blackjack/Scripts/BlackjackTutorial.cs) | Scripted practice hands, action gating and restoration of the original session |
| [BlackjackMainMenu](Assets/Blackjack/Scripts/BlackjackMainMenu.cs) | Menu, difficulty controls, help pages and transitions |
| [BlackjackUsernamePrompt](Assets/Blackjack/Scripts/BlackjackUsernamePrompt.cs) | Profile confirmation, name validation, saving and continuation |
| [BlackjackProfileEditor](Assets/Blackjack/Scripts/BlackjackProfileEditor.cs) | Draft profile, avatar preview, swatches, hex inputs and RGB sliders |
| [BlackjackPlayerProfile](Assets/Blackjack/Scripts/BlackjackPlayerProfile.cs) | Local profile schema, validation, JSON copying and PlayerPrefs storage |
| [BlackjackArtwork](Assets/Blackjack/Scripts/BlackjackArtwork.cs) | Sprite references and suit/rank lookup |
| [BlackjackCardMotion](Assets/Blackjack/Scripts/BlackjackCardMotion.cs) | Card arrival, movement and sprite flip |
| [BlackjackMenuMotion](Assets/Blackjack/Scripts/BlackjackMenuMotion.cs) | Rounded UI geometry and hover, press, focus and idle motion |
| [BlackjackAvatarGraphic](Assets/Blackjack/Scripts/BlackjackAvatarGraphic.cs) | Solid and gradient geometry and initials contrast |
| [BlackjackDemoTools](Assets/Blackjack/Editor/BlackjackDemoTools.cs) | Editor validation and Windows build commands |

## Values you can tune

| Value | What it controls and what to check |
| --- | --- |
| `Bank = 1000` in the round | Starting balance for both normal and practice rounds. Changing it affects expected balances in rule and smoke checks. |
| Table `wager = 50` and `values = { 10, 50, 100, 250 }` | Initial selection and increments assigned to the chip sprites. Keep reset defaults, chip artwork order and the tutorial's required +50 chip consistent. |
| `wager < 10` and `wager % 10` in `Deal` | Minimum and increment for legal bets. Update UI rounding, button availability and help text together. Odd wagers can truncate integer 3:2 payouts. |
| Six decks and reshuffle below 52 cards | Deck count sets shoe size; a higher threshold reshuffles sooner. Setting the threshold too low can leave a hand without enough cards. Hard's estimated rank probabilities are fixed. |
| `Bet * 5 / 2`, `Bet * 2`, `Bet`, `0` | Gross credits for blackjack, ordinary win, push and loss. The stake was deducted already; editing the text alone does not change the payout. |
| Dealer thresholds 15/17 and Hard tolerance `0.000001` | Higher thresholds make Easy/Medium draw more often. Higher tolerance makes Hard stand when drawing offers only a small estimated improvement. Update rule labels and tests along with policy. |
| Card rank 1–13 and suit 0–3 | Rank affects scoring and artwork; suit selects a sprite group. Keep the face array in four groups of 13. Out-of-range values can select the wrong sprite or throw. |
| Dealer delay `0.7f` | Seconds between steps. Increasing it slows play and may require longer smoke-check waits. |
| Tutorial steps 0, 5, 9 and 13 | Lesson entry points. Keep step transitions, allowed actions, coaching text and scripted cards aligned. Scripted cards deal player/dealer/player/dealer first. |
| Canvas reference size 1280 × 800 | Basis for menu/table coordinates. Update scaling, centering offsets, card layout and build window size together when changing it. |
| `x`, `y`, `w`, `h` in UI helpers | Rightward position, downward position, width and height relative to the parent layout. Larger text or smaller bounds can clip or wrap. |
| Card animation `0.28f`, `0.13f`, `0.32f` | Fade-in, movement smoothing and flip duration, respectively. Larger values slow the effects. The sprite swaps halfway through a flip. |
| Menu motion `0.8f`, `4`, `3` | Float rate, float amplitude and hover lift. Higher displacement or larger hover scaling can overlap nearby controls. |
| Username limit 2–16 | Controls acceptable names. Change the field limit, validation, profile normalization and instructions together. |
| Profile keys, version and enum order | Keys determine where saved data is found. Changing keys or enum numeric values can hide or reinterpret existing saves; add migration if needed. |
| `colorA`, `colorB`, and RGB slider maximum 255 | The hex defaults affect new profiles; Solid uses A only. Changing the slider range requires adjusting channel conversions. Existing valid saved colors remain intact. |
| Avatar corner proportion `0.25f`, 12 arc steps, threshold `0.179f` | Higher proportions round corners more. More steps add vertices. A higher luminance threshold gives light initials to more backgrounds. |
| Audio 2,205 samples, 44,100 Hz and volume `0.15f` | Samples divided by rate sets duration (0.05 seconds). Changing rate also affects pitch; volume affects loudness. Match the clip and sample array sizes. |
| 500 seeded simulations per difficulty | Increasing the count covers more seeds and takes longer. Update the reported count if you change it. |

Run **Blackjack → Run Rule Checks** after changing rules or profiles. For UI changes, check the main menu, table, tutorial and help in Play mode. See [BLACKJACK_DEMO.md](BLACKJACK_DEMO.md) for gameplay and build instructions.
