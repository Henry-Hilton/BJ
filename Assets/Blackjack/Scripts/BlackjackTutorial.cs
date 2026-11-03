using TMPro;
using UnityEngine;

namespace Blackjack
{
    /// <summary>Tutorial portion of the table controller; owns the scripted practice state.</summary>
    public sealed partial class BlackjackTable
    {
        BlackjackRound savedRound;
        int savedWager, tutorialStep;
        RectTransform tutorialPanel;
        TextMeshProUGUI tutorialHeading, tutorialText, tutorialNote;
        UnityEngine.UI.Button tutorialNext, menuButton;
        public bool TutorialActive => savedRound != null;

        /// <summary>Creates the coach panel, practice note and lesson progression button.</summary>
        void BuildTutorial()
        {
            tutorialPanel = Box(root, "Tutorial coach", 80, 185, 380, 128, new Color(.045f,.16f,.12f), true, 15);
            tutorialHeading = Label(tutorialPanel, "", 16, 8, 348, 22, 12, gold, TextAnchor.MiddleLeft);
            tutorialText = Label(tutorialPanel, "", 16, 34, 348, 86, 16, cream, TextAnchor.MiddleLeft);
            tutorialNote = Label(root, "PRACTICE CHIPS ONLY\nYour regular session is saved.", 88, 410, 340, 44, 14, muted, TextAnchor.MiddleLeft);
            tutorialNext = MakeButton(root, "NEXT LESSON [SPACE]", 88, 468, 300, 42, gold, AdvanceTutorial, ink);
            tutorialPanel.gameObject.SetActive(false); tutorialNote.gameObject.SetActive(false); tutorialNext.gameObject.SetActive(false);
        }
        /// <summary>Adds chips to the pending bet up to the bank rounded down to a multiple of ten. The tutorial only accepts its scripted chip on the first step.</summary>
        /// <param name="value">Chip denomination added to the selected wager. The first lesson specifically expects 50.</param>
        void AddWager(int value)
        {
            if (TutorialActive && (tutorialStep != 0 || value != 50)) return;
            wager = Mathf.Min(round.Bank / 10 * 10, wager + value);
            if (TutorialActive) tutorialStep = 1;
            Refresh();
        }
        /// <summary>Saves the original round and wager, pauses its dealer and starts a separate practice round.</summary>
        public void BeginTutorial()
        {
            if (TutorialActive) return;
            savedRound = round; savedWager = wager;
            StopDealerForTutorial();
            PrepareLesson(0);
        }
        /// <summary>Stops the existing dealer coroutine so it cannot advance the wrong round.</summary>
        void StopDealerForTutorial()
        {
            if (dealerRoutine != null) { StopCoroutine(dealerRoutine); dealerRoutine = null; }
        }
        /// <summary>Creates a fresh practice round with the cards and bet expected by the lesson.</summary>
        /// <param name="step">Scripted state index; lesson entry points are 0, 5, 9 and 13. Changing it changes expected actions, cards and instructions.</param>
        void PrepareLesson(int step)
        {
            StopDealerForTutorial();
            round = new BlackjackRound(21); // An isolated practice bank and fixed teaching hands.
            tutorialStep = step; wager = step == 0 ? 0 : 50; displayedHand = -1;
            if (step == 0 || step == 9)
                round.SetTestShoe(new Card(5,1), new Card(10), new Card(6,2), new Card(7,3), new Card(10,1));
            else if (step == 5)
                round.SetTestShoe(new Card(10,1), new Card(10), new Card(8,2), new Card(7,3));
            else round.SetTestShoe(new Card(1,1), new Card(9), new Card(13,2), new Card(8,3));
            Refresh();
        }
        /// <summary>Restores the original round and wager; restarts dealer timing if the saved round was awaiting it.</summary>
        public void EndTutorial()
        {
            if (!TutorialActive) return;
            StopDealerForTutorial();
            round = savedRound; wager = savedWager; savedRound = null; displayedHand = -1;
            Refresh(); menu.Open();
            if (round.State == BlackjackRound.Phase.Dealer) dealerRoutine = StartCoroutine(DealerTurn());
        }
        /// <summary>Checks whether the requested action is allowed at the current tutorial step.</summary>
        /// <param name="action">Requested command: deal, hit, stand or double. The tutorial gates these commands according to its current step.</param>
        bool TutorialAllows(string action) => action == "deal" ? tutorialStep == 1 || tutorialStep == 5 || tutorialStep == 9 || tutorialStep == 13
            : action == "hit" ? tutorialStep == 2 : action == "stand" ? tutorialStep == 6 : action == "double" && tutorialStep == 10;
        /// <summary>Advances the tutorial step after the accepted action.</summary>
        /// <param name="action">Requested command: deal, hit, stand or double. The tutorial gates these commands according to its current step.</param>
        void TutorialAction(string action)
        {
            if (action == "deal") tutorialStep = tutorialStep == 1 ? 2 : tutorialStep == 5 ? 6 : tutorialStep == 9 ? 10 : 14;
            else tutorialStep++; // Hit: 2 -> 3; Stand: 6 -> 7; Double: 10 -> 11.
        }
        /// <summary>Starts the next scripted lesson or finishes practice when the completion button is available.</summary>
        void AdvanceTutorial()
        {
            if (!TutorialActive || !tutorialNext.interactable) return;
            if (tutorialStep == 4) PrepareLesson(5);
            else if (tutorialStep == 8) PrepareLesson(9);
            else if (tutorialStep == 12) PrepareLesson(13);
            else if (tutorialStep == 14) EndTutorial();
        }
        /// <summary>Updates coaching text and enables only the relevant actions; completed dealer turns advance to the result step.</summary>
        void RefreshTutorial()
        {
            bool active = TutorialActive;
            tutorialPanel.gameObject.SetActive(active); tutorialNote.gameObject.SetActive(active); tutorialNext.gameObject.SetActive(active);
            menuButton.GetComponentInChildren<TextMeshProUGUI>().text = active ? "EXIT TUTORIAL" : "MENU  [ESC]";
            if (!active) return;
            if (round.State == BlackjackRound.Phase.Complete && (tutorialStep == 3 || tutorialStep == 7 || tutorialStep == 11)) tutorialStep++;
            int lesson = tutorialStep <= 4 ? 1 : tutorialStep <= 8 ? 2 : tutorialStep <= 12 ? 3 : 4;
            tutorialHeading.text = "TUTORIAL " + lesson + " / 4  /  " + (lesson == 1 ? "BET & HIT" : lesson == 2 ? "STAND" : lesson == 3 ? "DOUBLE" : "BLACKJACK & ACES");
            string[] instructions = {
                "Let's place a bet. Click the +50 chip below. Only the next action is enabled.",
                "50 chips are ready. Press Deal (Space) to receive your first two cards.",
                "5 + 6 makes 11. Hit (H) to draw a card. Aim for 21 without going over.",
                "You reached 21! Your turn ends automatically. Watch the dealer reveal their hand.",
                "21 beats the dealer's 17. A normal win pays 1:1: your 50 bet returns with 50 profit.",
                "Next: knowing when to stop. Press Deal (Space) for a new practice hand.",
                "10 + 8 makes 18. Stand (S) to keep this total and let the dealer play.",
                "Standing keeps your cards. The Medium dealer stops on 17 or higher.",
                "18 beats 17. Standing avoided the risk of busting (going over 21).",
                "Next: doubling down. Press Deal (Space) for a hand totaling 11.",
                "Double (D) adds another 50 chips to your bet, draws one final card, then stands.",
                "Your bet is now 100. Double allows exactly one extra card. Watch the dealer finish.",
                "Your doubled wager won! A 100-chip bet earns 100 profit on a normal win.",
                "Final lesson: aces and blackjack. Press Deal (Space) to see a natural blackjack.",
                "Ace + King is blackjack: a two-card 21! Aces are 1 or 11; faces are 10. Your 50 bet wins 75 profit (3:2). Tutorial complete!"
            };
            tutorialText.text = instructions[tutorialStep];
            deal.interactable &= TutorialAllows("deal"); hit.interactable &= TutorialAllows("hit");
            stand.interactable &= TutorialAllows("stand"); doubleDown.interactable &= TutorialAllows("double");
            clear.interactable = reset.interactable = false;
            for (int i = 0; i < chips.Count; i++) chips[i].interactable = tutorialStep == 0 && values[i] == 50;
            tutorialNext.interactable = tutorialStep == 4 || tutorialStep == 8 || tutorialStep == 12 || tutorialStep == 14;
            tutorialNext.gameObject.SetActive(tutorialNext.interactable);
            tutorialNext.GetComponentInChildren<TextMeshProUGUI>().text = tutorialStep == 14 ? "FINISH TUTORIAL [SPACE]" : "NEXT LESSON [SPACE]";
            statsText.text = "GUIDED PRACTICE  /  LESSON " + lesson + " OF 4";
        }
    }
}
