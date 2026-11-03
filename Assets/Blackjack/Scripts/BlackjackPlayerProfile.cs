using System;
using UnityEngine;

namespace Blackjack
{
    /// <summary>Saved avatar rendering modes. Solid uses color A; Gradient blends A and B.</summary>
    public enum AvatarStyle { Solid, Gradient }

    // Compact, versioned data that can later be serialized into a lobby message.
    // The local ID identifies a saved profile; it is not an authentication credential.
    [Serializable]
    /// <summary>Serializable local identity and avatar settings; the ID is not authentication.</summary>
    public sealed class BlackjackPlayerProfile
    {
        public int version = 1;
        public string profileId = Guid.NewGuid().ToString("N");
        public string displayName = "";
        public AvatarStyle avatarStyle = AvatarStyle.Gradient;
        public string colorA = "287A62";
        public string colorB = "D6AF6E";
        const string Key = "Blackjack.Profile.v1";
        public Color ColorA => ParseColor(colorA, new Color(.157f,.478f,.384f));
        public Color ColorB => ParseColor(colorB, new Color(.839f,.686f,.431f));
        public string Initials
        {
            get
            {
                var parts = (displayName ?? "").Split(new[] {' ', '_', '-'}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return "?";
                string first = StringInfoFirst(parts[0]);
                return (first + (parts.Length > 1 ? StringInfoFirst(parts[parts.Length - 1]) : "")).ToUpperInvariant();
            }
        }
        /// <summary>Reads the first text element for initials rather than slicing one UTF-16 unit.</summary>
        /// <param name="value">Nonempty text from which to read the first displayed element.</param>
        static string StringInfoFirst(string value) => System.Globalization.StringInfo.GetNextTextElement(value);
        /// <summary>Creates an independent JSON round-trip copy. Editing the copy does not edit the original.</summary>
        public BlackjackPlayerProfile Copy() => JsonUtility.FromJson<BlackjackPlayerProfile>(JsonUtility.ToJson(this));
        /// <summary>Accepts exactly six RGB hex digits after trimming whitespace and leading hash characters. Reports success separately from the parsed color.</summary>
        /// <param name="value">RGB text, optionally prefixed by #. Different valid digits select a different color.</param>
        /// <param name="color">Output color; consume it only when the method returns true.</param>
        public static bool TryColor(string value, out Color color)
        {
            string hex = (value ?? "").Trim().TrimStart('#');
            color = Color.white;
            return hex.Length == 6 && ColorUtility.TryParseHtmlString("#" + hex, out color);
        }
        /// <summary>Returns the parsed color or a fallback if the text is invalid.</summary>
        /// <param name="value">RGB text to parse, using the fallback when invalid.</param>
        /// <param name="fallback">Color used when the input cannot be parsed; changing it affects invalid values only.</param>
        static Color ParseColor(string value, Color fallback) => TryColor(value, out var color) ? color : fallback;
        /// <summary>Repairs invalid IDs, names, styles and colors and sets the current schema version. Run it before storing externally supplied profile data.</summary>
        public void Normalize()
        {
            version = 1;
            if (!Guid.TryParseExact(profileId, "N", out _)) profileId = Guid.NewGuid().ToString("N");
            displayName = (displayName ?? "").Trim();
            if (displayName.Length > 16) displayName = displayName.Substring(0, 16);
            foreach (char c in displayName) if (!char.IsLetterOrDigit(c) && c != ' ' && c != '_' && c != '-') { displayName = ""; break; }
            if (displayName.Length < 2) displayName = "";
            if (!Enum.IsDefined(typeof(AvatarStyle), avatarStyle)) avatarStyle = AvatarStyle.Solid;
            colorA = ColorUtility.ToHtmlStringRGB(ColorA); colorB = ColorUtility.ToHtmlStringRGB(ColorB);
        }
        /// <summary>Loads the local JSON profile and falls back to the legacy username when the JSON is absent or unreadable.</summary>
        public static BlackjackPlayerProfile Load()
        {
            BlackjackPlayerProfile profile = null;
            try { if (PlayerPrefs.HasKey(Key)) profile = JsonUtility.FromJson<BlackjackPlayerProfile>(PlayerPrefs.GetString(Key)); }
            catch (ArgumentException) { }
            if (profile == null) profile = new BlackjackPlayerProfile { displayName = PlayerPrefs.GetString("Blackjack.PlayerName", "") };
            profile.Normalize(); return profile;
        }
        /// <summary>Normalizes and saves the JSON profile and legacy username to local PlayerPrefs.</summary>
        public void Save()
        {
            Normalize(); PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.SetString("Blackjack.PlayerName", displayName); PlayerPrefs.Save();
        }
    }
}
