using TMPro;
using UnityEngine;

namespace Blackjack
{
    /// <summary>Profile editor portion of the menu; edits a draft until the player saves.</summary>
    public sealed partial class BlackjackMainMenu
    {
        BlackjackPlayerProfile draftProfile;
        bool editingProfile, editSecondColor, syncingProfile;
        BlackjackAvatarGraphic avatarPreview;
        TextMeshProUGUI avatarInitials, colorHint;
        readonly TMP_InputField[] colorInputs = new TMP_InputField[2];
        readonly UnityEngine.UI.Button[] styleButtons = new UnityEngine.UI.Button[2];
        readonly UnityEngine.UI.Button[] colorButtons = new UnityEngine.UI.Button[2];
        readonly UnityEngine.UI.Slider[] colorSliders = new UnityEngine.UI.Slider[3];
        readonly TextMeshProUGUI[] channelLabels = new TextMeshProUGUI[3];
        BlackjackAvatarGraphic menuAvatar;
        TextMeshProUGUI menuInitials;

        /// <summary>Creates the small menu avatar and initials label.</summary>
        void BuildMenuAvatar()
        {
            var rect = Panel(page, "Menu avatar", 984, 34, 40, 40, Color.clear, false);
            menuAvatar = rect.gameObject.AddComponent<BlackjackAvatarGraphic>(); menuAvatar.raycastTarget = false;
            menuInitials = Text(rect, "", 0, 0, 40, 40, 16, cream, 0, TextAlignmentOptions.Midline);
            menuInitials.richText = false;
        }
        /// <summary>Redraws the menu avatar from the table's active profile.</summary>
        void RefreshMenuAvatar()
        {
            menuAvatar.Apply(table.Profile); menuInitials.text = table.Profile.Initials;
            menuInitials.color = BlackjackAvatarGraphic.InitialColor(table.Profile);
        }

        /// <summary>Builds the preview, style choices, swatches, RGB sliders and hex inputs.</summary>
        /// <param name="card">Parent profile card that owns the editor controls and their coordinates.</param>
        void BuildAvatarEditor(RectTransform card)
        {
            var editor = Panel(card, "Avatar editor", 500, 28, 404, 552, Color.clear, false);
            var avatar = Panel(editor, "Avatar preview", 138, 0, 128, 128, Color.clear, false);
            avatarPreview = avatar.gameObject.AddComponent<BlackjackAvatarGraphic>(); avatarPreview.raycastTarget = false;
            avatarInitials = Text(avatar, "?", 0, 0, 128, 128, 42, cream, 0, TextAlignmentOptions.Midline);
            avatarInitials.richText = false;
            Text(editor, "LIVE PREVIEW", 0, 139, 404, 24, 11, muted, 2, TextAlignmentOptions.Center);
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                styleButtons[i] = Button(editor, i == 0 ? "Solid" : "Gradient", i * 208, 178, 196, 36, false, () =>
                {
                    draftProfile.avatarStyle = (AvatarStyle)index;
                    if (index == 0) editSecondColor = false;
                    RefreshProfileEditor(true);
                });
                colorButtons[i] = Button(editor, i == 0 ? "Color A" : "Color B", i * 208, 226, 196, 28, false, () =>
                {
                    editSecondColor = index == 1; RefreshProfileEditor(false);
                });
                colorInputs[i] = BuildHexField(editor, i * 208, 262);
                colorInputs[i].onValueChanged.AddListener(value =>
                {
                    if (syncingProfile || draftProfile == null) return;
                    if (BlackjackPlayerProfile.TryColor(value, out Color color))
                    {
                        if (index == 0) draftProfile.colorA = ColorUtility.ToHtmlStringRGB(color);
                        else draftProfile.colorB = ColorUtility.ToHtmlStringRGB(color);
                    }
                    RefreshProfileEditor(false);
                });
                colorInputs[i].onSelect.AddListener(value => { editSecondColor = index == 1; RefreshProfileEditor(false); });
            }
            string[] colors = { "287A62", "2469B3", "764EC4", "C84E70", "D6AF6E", "D66B38", "E9E4D8", "172A24" };
            for (int i = 0; i < colors.Length; i++)
            {
                string hex = colors[i];
                var swatch = Button(editor, "", i * 51, 322, 40, 32, false, () => SetActiveColor(hex));
                ColorUtility.TryParseHtmlString("#" + hex, out var color);
                swatch.GetComponent<UnityEngine.UI.Image>().color = color;
                swatch.name = "Color preset " + hex;
            }
            colorHint = Text(editor, "", 0, 360, 404, 22, 11, muted);
            for (int i = 0; i < 3; i++)
            {
                Text(editor, new[] {"R", "G", "B"}[i], 0, 394 + i * 42, 26, 24, 13, muted);
                channelLabels[i] = Text(editor, "", 350, 394 + i * 42, 54, 24, 13, cream, 0, TextAlignmentOptions.Right);
                colorSliders[i] = BuildColorSlider(editor, 38, 394 + i * 42);
                colorSliders[i].onValueChanged.AddListener(value =>
                {
                    if (syncingProfile || draftProfile == null) return;
                    SetActiveColor(ColorUtility.ToHtmlStringRGB(new Color(colorSliders[0].value / 255f, colorSliders[1].value / 255f, colorSliders[2].value / 255f)));
                });
            }
            Text(editor, "Choose a swatch, enter a hex color, or mix RGB.", 0, 526, 404, 22, 11, muted);
        }
        /// <summary>Creates a single-line RGB hex input allowing a leading hash and six digits.</summary>
        /// <param name="parent">UI container and coordinate origin. Changing it can move the element, change draw order or affect input.</param>
        /// <param name="x">Horizontal offset in reference UI units. Larger values move right.</param>
        /// <param name="y">Downward offset in reference UI units. Larger values move down.</param>
        TMP_InputField BuildHexField(Transform parent, float x, float y)
        {
            var field = Panel(parent, "Hex color", x, y, 196, 42, new Color(.025f,.075f,.065f), true, 9);
            field.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var viewport = Panel(field, "Hex viewport", 12, 0, 172, 42, Color.clear, false);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var text = Text(viewport, "", 0, 0, 172, 42, 19, cream, 1, TextAlignmentOptions.MidlineLeft); text.richText = false;
            var input = field.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = viewport; input.textComponent = text; input.fontAsset = font;
            input.targetGraphic = field.GetComponent<UnityEngine.UI.Image>(); input.characterLimit = 7;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.onValidateInput = (value, index, c) => "0123456789abcdefABCDEF#".IndexOf(c) >= 0 ? char.ToUpperInvariant(c) : '\0';
            input.customCaretColor = true; input.caretColor = gold; return input;
        }
        /// <summary>Creates a whole-number RGB slider with a 0–255 range.</summary>
        /// <param name="parent">UI container and coordinate origin. Changing it can move the element, change draw order or affect input.</param>
        /// <param name="x">Horizontal offset in reference UI units. Larger values move right.</param>
        /// <param name="y">Downward offset in reference UI units. Larger values move down.</param>
        UnityEngine.UI.Slider BuildColorSlider(Transform parent, float x, float y)
        {
            var rect = Panel(parent, "RGB slider", x, y, 296, 24, Color.clear);
            rect.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var track = Panel(rect, "Track", 0, 8, 296, 8, new Color(.025f,.075f,.065f), true, 4);
            var fill = Panel(track, "Fill", 0, 0, 296, 8, gold, true, 4);
            fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            var area = Panel(rect, "Handle area", 8, 0, 280, 24, Color.clear, false);
            var handle = Panel(area, "Handle", 0, 0, 16, 24, cream, true, 7);
            handle.pivot = new Vector2(.5f,.5f); handle.anchorMin = handle.anchorMax = new Vector2(0,.5f);
            handle.anchoredPosition = Vector2.zero;
            var slider = rect.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
            slider.minValue = 0; slider.maxValue = 255; slider.wholeNumbers = true;
            return slider;
        }
        /// <summary>Opens the profile form in edit mode unless the menu is transitioning.</summary>
        public void ShowProfileEditor()
        {
            if (transitioning) return;
            editingProfile = true; OpenProfileForm();
        }
        /// <summary>Creates a draft copy, loads the controls and focuses the username field.</summary>
        void OpenProfileForm()
        {
            page.gameObject.SetActive(false); rules.gameObject.SetActive(false); usernamePanel.gameObject.SetActive(true);
            draftProfile = table.Profile.Copy(); editSecondColor = false;
            usernameInput.SetTextWithoutNotify(draftProfile.displayName);
            RefreshProfileEditor(true);
            usernameContinue.GetComponentInChildren<TextMeshProUGUI>().text = editingProfile ? "SAVE PROFILE   >" : "CONTINUE   >";
            usernameInput.Select(); usernameInput.ActivateInputField();
        }
        /// <summary>Requires a valid primary color and, in gradient mode, a valid secondary color.</summary>
        bool ProfileColorsValid() => BlackjackPlayerProfile.TryColor(colorInputs[0].text, out _)
            && (draftProfile.avatarStyle == AvatarStyle.Solid || BlackjackPlayerProfile.TryColor(colorInputs[1].text, out _));
        /// <summary>Changes the selected endpoint and synchronizes controls and preview.</summary>
        /// <param name="hex">Six-digit RGB string for the active endpoint. A different value changes the preview and future saved avatar.</param>
        void SetActiveColor(string hex)
        {
            if (editSecondColor) draftProfile.colorB = hex; else draftProfile.colorA = hex;
            RefreshProfileEditor(true);
        }
        /// <summary>Synchronizes draft values, valid inputs, enabled controls and avatar preview.</summary>
        /// <param name="syncFields">True rewrites hex fields from the draft; false preserves typed text, including incomplete input.</param>
        void RefreshProfileEditor(bool syncFields)
        {
            if (draftProfile == null || avatarPreview == null) return;
            syncingProfile = true;
            draftProfile.displayName = usernameInput.text;
            bool gradient = draftProfile.avatarStyle == AvatarStyle.Gradient;
            if (syncFields)
            {
                colorInputs[0].SetTextWithoutNotify("#" + draftProfile.colorA);
                colorInputs[1].SetTextWithoutNotify("#" + draftProfile.colorB);
            }
            for (int i = 0; i < 2; i++)
            {
                styleButtons[i].GetComponent<UnityEngine.UI.Image>().color = i == (int)draftProfile.avatarStyle ? gold : new Color(.06f,.13f,.11f);
                styleButtons[i].GetComponentInChildren<TextMeshProUGUI>().color = i == (int)draftProfile.avatarStyle ? inkColor : cream;
                colorButtons[i].interactable = i == 0 || gradient; colorInputs[i].interactable = i == 0 || gradient;
                colorButtons[i].GetComponent<UnityEngine.UI.Image>().color = i == (editSecondColor ? 1 : 0) ? new Color(.17f,.29f,.22f) : new Color(.06f,.13f,.11f);
            }
            Color color = editSecondColor ? draftProfile.ColorB : draftProfile.ColorA;
            float[] channels = {color.r, color.g, color.b};
            for (int i = 0; i < 3; i++)
            {
                colorSliders[i].SetValueWithoutNotify(Mathf.RoundToInt(channels[i] * 255));
                channelLabels[i].text = Mathf.RoundToInt(channels[i] * 255).ToString();
            }
            avatarPreview.Apply(draftProfile); avatarInitials.text = draftProfile.Initials;
            avatarInitials.color = BlackjackAvatarGraphic.InitialColor(draftProfile);
            bool valid = ProfileColorsValid();
            colorHint.text = valid ? "EDITING COLOR " + (editSecondColor ? "B" : "A") + " / #RRGGBB" : "Enter six hex digits for each active color.";
            colorHint.color = valid ? muted : gold;
            usernameContinue.interactable = ValidUsername(usernameInput.text) && valid;
            syncingProfile = false;
        }
        static readonly Color inkColor = new Color(.025f,.075f,.065f);
    }
}
