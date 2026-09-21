using System;
using System.Collections.Generic;

namespace Blackjack
{
    public readonly struct Card
    {
        public readonly int Rank, Suit;
        public Card(int rank, int suit = 0) { Rank = rank; Suit = suit; }
        public string Label => Rank == 1 ? "A" : Rank == 11 ? "J" : Rank == 12 ? "Q" : Rank == 13 ? "K" : Rank.ToString();
        public int Value => Rank == 1 ? 11 : Math.Min(Rank, 10);
    }

    // Pure rules: no Unity dependency, so payouts and edge cases can be tested directly.
    public sealed class BlackjackRound
    {
        public enum Phase { Betting, Player, Dealer, Complete }
        public Phase State { get; private set; } = Phase.Betting;
        public int Bank { get; private set; } = 1000;
        public int Bet { get; private set; }
        public int Wins { get; private set; }
        public int Hands { get; private set; }
        public string Message { get; private set; } = "Choose your chips, then deal.";
        public readonly List<Card> Player = new List<Card>();
        public readonly List<Card> Dealer = new List<Card>();
        readonly List<Card> shoe = new List<Card>();
        readonly Random random;
        public bool CanDouble => State == Phase.Player && Player.Count == 2 && Bank >= Bet;
        public BlackjackRound(int? seed = null) { random = seed.HasValue ? new Random(seed.Value) : new Random(); }
        public static int Total(IReadOnlyList<Card> hand)
        {
            int sum = 0, aces = 0;
            foreach (var card in hand) { sum += card.Value; if (card.Rank == 1) aces++; }
            while (sum > 21 && aces-- > 0) sum -= 10;
            return sum;
        }
        public static bool Natural(IReadOnlyList<Card> hand) => hand.Count == 2 && Total(hand) == 21;
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
        public void Hit()
        {
            if (State != Phase.Player) return;
            Player.Add(Draw());
            if (Total(Player) > 21) Finish("Bust! Dealer wins.", 0);
            else if (Total(Player) == 21) Stand();
        }
        public void Stand() { if (State == Phase.Player) { State = Phase.Dealer; Message = "Dealer plays. Stands on all 17s."; } }
        public void Double()
        {
            if (!CanDouble) return;
            Bank -= Bet; Bet *= 2; Hit(); if (State == Phase.Player) Stand();
        }
        public void DealerStep()
        {
            if (State != Phase.Dealer) return;
            if (Total(Dealer) < 17) { Dealer.Add(Draw()); if (Total(Dealer) < 17) return; }
            int player = Total(Player), dealer = Total(Dealer);
            if (dealer > 21 || player > dealer) { Wins++; Finish(dealer > 21 ? "Dealer busts. You win!" : "You win!", Bet * 2); }
            else if (player == dealer) Finish("Push. Your bet is returned.", Bet);
            else Finish("Dealer wins.", 0);
        }
        void Finish(string message, int returned)
        {
            Bank += returned; State = Phase.Complete;
            int net = returned - Bet;
            Message = message + "   " + (net > 0 ? "+" : "") + net + " chips";
        }
        Card Draw() { var card = shoe[shoe.Count - 1]; shoe.RemoveAt(shoe.Count - 1); return card; }
        void Shuffle()
        {
            shoe.Clear();
            for (int deck = 0; deck < 6; deck++) for (int suit = 0; suit < 4; suit++) for (int rank = 1; rank <= 13; rank++) shoe.Add(new Card(rank, suit));
            for (int i = shoe.Count - 1; i > 0; i--) { int j = random.Next(i + 1); var temp = shoe[i]; shoe[i] = shoe[j]; shoe[j] = temp; }
        }
        // Deterministic shoe for editor rule checks; first supplied card is dealt first.
        public void SetTestShoe(params Card[] first)
        {
            Shuffle(); for (int i = first.Length - 1; i >= 0; i--) shoe.Add(first[i]);
        }
    }
}
