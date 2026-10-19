using System;
using UnityEngine;

namespace Blackjack
{
    public enum AvatarStyle { Solid, Gradient }

    // Compact, versioned data that can later be serialized into a lobby message.
    // The local ID identifies a saved profile; it is not an authentication credential.
    [Serializable]
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
        static string StringInfoFirst(string value) => System.Globalization.StringInfo.GetNextTextElement(value);
        public BlackjackPlayerProfile Copy() => JsonUtility.FromJson<BlackjackPlayerProfile>(JsonUtility.ToJson(this));
        public static bool TryColor(string value, out Color color)
        {
            string hex = (value ?? "").Trim().TrimStart('#');
            color = Color.white;
            return hex.Length == 6 && ColorUtility.TryParseHtmlString("#" + hex, out color);
        }
        static Color ParseColor(string value, Color fallback) => TryColor(value, out var color) ? color : fallback;
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
        public static BlackjackPlayerProfile Load()
        {
            BlackjackPlayerProfile profile = null;
            try { if (PlayerPrefs.HasKey(Key)) profile = JsonUtility.FromJson<BlackjackPlayerProfile>(PlayerPrefs.GetString(Key)); }
            catch (ArgumentException) { }
            if (profile == null) profile = new BlackjackPlayerProfile { displayName = PlayerPrefs.GetString("Blackjack.PlayerName", "") };
            profile.Normalize(); return profile;
        }
        public void Save()
        {
            Normalize(); PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.SetString("Blackjack.PlayerName", displayName); PlayerPrefs.Save();
        }
    }
}
