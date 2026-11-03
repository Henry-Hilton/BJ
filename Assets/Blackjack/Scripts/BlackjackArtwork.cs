using UnityEngine;

namespace Blackjack
{
    // References the supplied poker_cards_chips_2d pack; contains no generated artwork.
    /// <summary>Serialized sprite references loaded from Resources/BlackjackArtwork.</summary>
    public sealed class BlackjackArtwork : ScriptableObject
    {
        /// <summary>Face sprites grouped by suit and ordered ace through king in each group.</summary>
        public Sprite[] cards;
        /// <summary>Sprite shown for the dealer's hidden card. Replacing it changes only appearance.</summary>
        public Sprite cardBack;
        /// <summary>Chip sprites ordered to match the table's 10, 50, 100 and 250 denominations.</summary>
        public Sprite[] chips;
        /// <summary>Looks up a face sprite by suit group and rank. Reordering the sprites without updating the indexing shows the wrong artwork.</summary>
        /// <param name="card">Card identity whose suit and rank select the sprite in the configured array.</param>
        public Sprite GetCard(Card card) => cards[card.Suit * 13 + card.Rank - 1];
    }
}
