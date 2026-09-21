using UnityEngine;

namespace Blackjack
{
    // References the supplied poker_cards_chips_2d pack; contains no generated artwork.
    public sealed class BlackjackArtwork : ScriptableObject
    {
        public Sprite[] cards;
        public Sprite cardBack;
        public Sprite[] chips;
        public Sprite GetCard(Card card) => cards[card.Suit * 13 + card.Rank - 1];
    }
}
