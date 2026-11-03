# Blackjack

A single-player blackjack game built in Unity. Practice with guided lessons, choose a dealer difficulty, and customize your player profile with a solid or gradient avatar. The game uses play chips.

![Blackjack table](Assets/DemoEvidence/table-polished.png)

## Getting started

1. Clone the repository with Git LFS enabled, then run `git lfs pull` to download the artwork and screenshots.
2. Open the project in **Unity 6000.3.26f1**.
3. Open `Assets/Scenes/SampleScene.unity` and press **Play**.
4. Choose **Take a Seat** for a regular game or **Tutorial / Guided Practice** for four interactive lessons.

## Controls

| Action | Shortcut |
| --- | --- |
| Deal | Space |
| Hit | H |
| Stand | S |
| Double | D |
| Menu or back | Escape |

The in-game **How to Play** guide explains scoring, payouts, and the three dealer modes. **Medium** uses the standard stand-on-17 policy; **Easy** stands on 15 or higher, and **Hard** adapts its play to the visible hands.

## Development

- **Blackjack → Run Rule Checks** runs deterministic checks and 1,500 simulated hands in the Unity Editor.
- **Blackjack → Build Windows Demo** creates a Windows build at `Builds/Blackjack/Blackjack.exe`.
- See [BLACKJACK_DEMO.md](BLACKJACK_DEMO.md) for detailed rules, profile storage, and implementation notes.

WLAN multiplayer is planned. Player profiles are saved locally, and the current game runs offline.
