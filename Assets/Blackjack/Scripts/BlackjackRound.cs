using System;
using System.Collections.Generic;

namespace Blackjack
{
    /// <summary>Immutable card identity. Rank drives scoring; rank and suit select artwork.</summary>
    public readonly struct Card
    {
        /// <summary>Rank 1–13 and suit index 0–3. Only rank affects scoring.</summary>
        public readonly int Rank, Suit;
        /// <summary>Stores a card rank and suit; changing the rank changes scoring and artwork, while changing the suit changes only artwork.</summary>
        /// <param name="rank">Card rank 1–13: ace is 1 and J/Q/K are 11–13. Values outside this range are unsupported.</param>
        /// <param name="suit">Zero-based artwork group 0–3; the default is 0. Out-of-range values can select the wrong sprite or throw.</param>
        public Card(int rank, int suit = 0) { Rank = rank; Suit = suit; }
        /// <summary>Printable rank; editing the label mapping does not alter scoring.</summary>
        public string Label => Rank == 1 ? "A" : Rank == 11 ? "J" : Rank == 12 ? "Q" : Rank == 13 ? "K" : Rank.ToString();
        /// <summary>Initial point value before hand scoring adjusts aces to avoid a bust.</summary>
        public int Value => Rank == 1 ? 11 : Math.Min(Rank, 10);
    }

    // Pure rules: no Unity dependency, so payouts and edge cases can be tested directly.
    /// <summary>Unity-independent rules and chip accounting for one player against the dealer.</summary>
    public sealed class BlackjackRound
    {
        /// <summary>Round lifecycle states. Transitions control when bets, player actions and settlement are legal.</summary>
        public enum Phase { Betting, Player, Dealer, Complete }
        public Phase State { get; private set; } = Phase.Betting;
        public int Bank { get; private set; } = 1000;
        public int Bet { get; private set; }
        public int Wins { get; private set; }
        public int Hands { get; private set; }
        public DealerDifficulty Difficulty { get; private set; } = DealerDifficulty.Medium;
        public bool CanChangeDifficulty => State == Phase.Betting || State == Phase.Complete;
        /// <summary>Changes dealer policy between hands. Invalid values or a change during a hand are rejected.</summary>
        /// <param name="difficulty">Dealer policy enum. A different mode changes drawing strategy; enum numbers also back the saved preference and menu order.</param>
        public bool SetDifficulty(DealerDifficulty difficulty)
        {
            if (!CanChangeDifficulty || !Enum.IsDefined(typeof(DealerDifficulty), difficulty)) return false;
            Difficulty = difficulty; return true;
        }
        public string Message { get; private set; } = "Choose your chips, then deal.";
        public readonly List<Card> Player = new List<Card>();
        public readonly List<Card> Dealer = new List<Card>();
        readonly List<Card> shoe = new List<Card>();
        readonly Random random;
        public bool CanDouble => State == Phase.Player && Player.Count == 2 && Bank >= Bet;
        /// <summary>Creates a fresh round model with its own shuffle generator and a starting bank of 1,000 chips.</summary>
        /// <param name="seed">Optional shuffle seed. Use the same seed for reproducible card order in the same runtime; null uses a fresh generator.</param>
        public BlackjackRound(int? seed = null) { random = seed.HasValue ? new Random(seed.Value) : new Random(); }
        /// <summary>Sums the hand, downgrading aces from 11 to 1 as needed. Changing the hand can change which aces count as 11.</summary>
        /// <param name="hand">Cards to score or check for a natural. Card values matter; suit and order do not.</param>
        public static int Total(IReadOnlyList<Card> hand)
        {
            int sum = 0, aces = 0;
            foreach (var card in hand) { sum += card.Value; if (card.Rank == 1) aces++; }
            while (sum > 21 && aces-- > 0) sum -= 10;
            return sum;
        }
        /// <summary>Checks for a two-card 21. Adding a third card makes the result false even if the total remains 21.</summary>
        /// <param name="hand">Cards to score or check for a natural. Card values matter; suit and order do not.</param>
        public static bool Natural(IReadOnlyList<Card> hand) => hand.Count == 2 && Total(hand) == 21;
        /// <summary>Validates the bet, deducts chips, deals two cards each and immediately resolves natural blackjack. Returns false without dealing when the bet or phase is invalid.</summary>
        /// <param name="wager">Chips to put at risk, at least 10, in multiples of 10, and no more than Bank. Higher bets increase possible gains and losses.</param>
        public bool Deal(int wager)
        {
            if ((State != Phase.Betting && State != Phase.Complete) || wager < 10 || wager % 10 != 0 || wager > Bank) return false;
            if (shoe.Count < 52) Shuffle();
            Bank -= wager; Bet = wager; Hands++; Player.Clear(); Dealer.Clear();
            Player.Add(Draw()); Dealer.Add(Draw()); Player.Add(Draw()); Dealer.Add(Draw());
            State = Phase.Player; Message = "Your move. Hit, stand, or double down.";
            if (Natural(Player) || Natural(Dealer))
            {
                if (Natural(Player) && Natural(Dealer)) Finish("Both have blackjack. Push.", Bet);
                else if (Natural(Player)) { Wins++; Finish("BLACKJACK! Pays 3:2.", Bet * 5 / 2); }
                else Finish("Dealer blackjack.", 0);
            }
            return true;
        }
        /// <summary>Draws one card on the player's turn. Busting settles the hand; reaching 21 passes play to the dealer.</summary>
        public void Hit()
        {
            if (State != Phase.Player) return;
            Player.Add(Draw());
            if (Total(Player) > 21) Finish("Bust! Dealer wins.", 0);
            else if (Total(Player) == 21) Stand();
        }
        /// <summary>Ends the player's turn and starts the dealer phase. Calling this in another phase does nothing.</summary>
        public void Stand() { if (State == Phase.Player) { State = Phase.Dealer; Message = "Dealer plays. " + BlackjackDealerAI.Rule(Difficulty); } }
        /// <summary>Deducts a second wager, draws exactly one card and ends the player's turn. Requires two cards and enough chips.</summary>
        public void Double()
        {
            if (!CanDouble) return;
            Bank -= Bet; Bet *= 2; Hit(); if (State == Phase.Player) Stand();
        }
        /// <summary>Advances the dealer by at most one card, then settles if no more cards are needed. Repeated calls after settlement do nothing.</summary>
        public void DealerStep()
        {
            if (State != Phase.Dealer) return;
            if (BlackjackDealerAI.ShouldHit(Difficulty, Dealer, Total(Player)))
            {
                Dealer.Add(Draw());
                if (BlackjackDealerAI.ShouldHit(Difficulty, Dealer, Total(Player))) return;
            }
            int player = Total(Player), dealer = Total(Dealer);
            if (dealer > 21 || player > dealer) { Wins++; Finish(dealer > 21 ? "Dealer busts. You win!" : "You win!", Bet * 2); }
            else if (player == dealer) Finish("Push. Your bet is returned.", Bet);
            else Finish("Dealer wins.", 0);
        }
        /// <summary>Credits the return, marks the round complete and displays the net result.</summary>
        /// <param name="message">Result text shown to the player. Changing it does not change settlement.</param>
        /// <param name="returned">Total chips credited, including any returned stake. Zero loses the wager; Bet pushes; 2 × Bet wins even money.</param>
        void Finish(string message, int returned)
        {
            Bank += returned; State = Phase.Complete;
            int net = returned - Bet;
            Message = message + "   " + (net > 0 ? "+" : "") + net + " chips";
        }
        /// <summary>Removes and returns the last card of the shoe. The caller must ensure the shoe is nonempty.</summary>
        Card Draw() { var card = shoe[shoe.Count - 1]; shoe.RemoveAt(shoe.Count - 1); return card; }
        /// <summary>Rebuilds and randomly shuffles six decks. Changing the deck count changes shoe size and may require adjusting the reshuffle threshold.</summary>
        void Shuffle()
        {
            shoe.Clear();
            for (int deck = 0; deck < 6; deck++) for (int suit = 0; suit < 4; suit++) for (int rank = 1; rank <= 13; rank++) shoe.Add(new Card(rank, suit));
            for (int i = shoe.Count - 1; i > 0; i--) { int j = random.Next(i + 1); var temp = shoe[i]; shoe[i] = shoe[j]; shoe[j] = temp; }
        }
        // Deterministic shoe for editor rule checks; first supplied card is dealt first.
        /// <summary>Shuffles a normal shoe and appends scripted cards in reverse storage order. Only tutorials and checks should use this override.</summary>
        /// <param name="first">Scripted cards in draw order: player, dealer, player, dealer, then subsequent draws. Reordering them changes the teaching or test outcome.</param>
        public void SetTestShoe(params Card[] first)
        {
            Shuffle(); for (int i = first.Length - 1; i >= 0; i--) shoe.Add(first[i]);
        }
    }
}
