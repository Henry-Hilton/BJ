using TMPro;
using UnityEngine;

namespace Blackjack
{
    public sealed partial class BlackjackMainMenu
    {
        RectTransform usernamePanel;
        TMP_InputField usernameInput;
        UnityEngine.UI.Button usernameContinue;
        bool usernameConfirmed, pendingTutorial;

        void BuildUsernamePrompt()
        {
            usernamePanel = Panel(canvas, "Player name prompt", 0, 0, 1280, 800, new Color(.025f,.075f,.065f));
            var card = Panel(usernamePanel, "Welcome panel", 170, 95, 940, 610, new Color(.055f,.13f,.10f), true, 24);
            Text(card, "YOUR SEAT IS READY", 36, 28, 508, 28, 12, gold, 3);
            Text(card, "Make it yours.", 36, 76, 430, 54, 38, cream);
            Text(card, "Your name. Your colors. Your seat.", 36, 137, 430, 30, 17, muted);
            var field = Panel(card, "Username input", 36, 188, 420, 58, new Color(.025f,.075f,.065f), true, 12);
            field.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var viewport = Panel(field, "Text viewport", 18, 0, 384, 58, Color.clear, false);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var value = Text(viewport, "", 0, 0, 384, 58, 22, cream, 0, TextAlignmentOptions.MidlineLeft);
            value.richText = false;
            var placeholder = Text(viewport, "Your username", 0, 0, 384, 58, 22, muted, 0, TextAlignmentOptions.MidlineLeft);
            usernameInput = field.gameObject.AddComponent<TMP_InputField>();
            usernameInput.textViewport = viewport; usernameInput.textComponent = value; usernameInput.placeholder = placeholder;
            usernameInput.targetGraphic = field.GetComponent<UnityEngine.UI.Image>();
            usernameInput.fontAsset = font; usernameInput.characterLimit = 16;
            usernameInput.lineType = TMP_InputField.LineType.SingleLine;
            usernameInput.customCaretColor = true; usernameInput.caretColor = gold;
            usernameInput.selectionColor = new Color(.84f,.69f,.43f,.3f);
            usernameInput.onValidateInput = (text, index, character) => char.IsLetterOrDigit(character) || character == ' ' || character == '_' || character == '-' ? character : '\0';
            usernameInput.onValueChanged.AddListener(text => RefreshProfileEditor(false));
            usernameInput.onSubmit.AddListener(text => ConfirmUsername());
            Text(card, "2–16 characters. Letters, numbers, spaces, _ or -.", 36, 252, 430, 26, 12, muted);
            Text(card, "YOUR TABLE IDENTITY", 36, 326, 420, 26, 12, gold, 2);
            Text(card, "Pick a solid color or blend two colors.\nYour initials complete the picture.\n\nYour profile is saved on this device.\nYou can edit it anytime from the menu.", 36, 363, 420, 124, 17, muted);
            usernameContinue = Button(card, "Continue", 36, 518, 264, 52, true, ConfirmUsername);
            Button(card, "Back", 316, 518, 140, 52, false, CancelUsername);
            BuildAvatarEditor(card);
            usernamePanel.gameObject.SetActive(false);
        }
        static bool ValidUsername(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            string name = value.Trim();
            if (name.Length < 2 || name.Length > 16) return false;
            foreach (char character in name)
                if (!char.IsLetterOrDigit(character) && character != ' ' && character != '_' && character != '-') return false;
            return true;
        }
        bool EnsureUsername(bool tutorial)
        {
            if (usernameConfirmed) return true;
            editingProfile = false;
            pendingTutorial = tutorial;
            OpenProfileForm();
            return false;
        }
        void ConfirmUsername()
        {
            if (!usernamePanel.gameObject.activeSelf || !ValidUsername(usernameInput.text) || !ProfileColorsValid()) return;
            string name = usernameInput.text.Trim();
            usernameConfirmed = true; usernameInput.DeactivateInputField();
            draftProfile.displayName = name; draftProfile.Save(); table.ApplyProfile(draftProfile);
            usernamePanel.gameObject.SetActive(false);
            if (editingProfile) { editingProfile = false; Open(); }
            else if (pendingTutorial) StartTutorial(); else Play();
        }
        void CancelUsername()
        {
            usernameInput.DeactivateInputField(); usernamePanel.gameObject.SetActive(false);
            editingProfile = false;
            page.gameObject.SetActive(true); group.alpha = 1; playButton.Select();
        }
        internal void UseSmokeProfile() { usernameConfirmed = true; table.SetPlayerName("Demo Player"); }
    }
}
