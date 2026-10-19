using System.Collections.Generic;

namespace Blackjack
{
    public enum DealerDifficulty { Easy, Medium, Hard }

    // Hard maximizes win/loss expectation using a fixed six-deck rank distribution.
    // It sees the completed player hand, but never receives the shoe or next card.
    public static class BlackjackDealerAI
    {
        public static string Rule(DealerDifficulty difficulty) => difficulty == DealerDifficulty.Easy
            ? "EASY / STANDS ON 15+" : difficulty == DealerDifficulty.Hard
            ? "HARD / ADAPTIVE DEALER" : "MEDIUM / STANDS ON 17+";

        public static bool ShouldHit(DealerDifficulty difficulty, IReadOnlyList<Card> dealer, int player)
        {
            int total = BlackjackRound.Total(dealer);
            if (total >= 21) return false;
            if (difficulty != DealerDifficulty.Hard) return total < (difficulty == DealerDifficulty.Easy ? 15 : 17);
            int raw = 0, aces = 0;
            foreach (var card in dealer) { raw += card.Value; if (card.Rank == 1) aces++; }
            while (raw > 21 && aces > 0) { raw -= 10; aces--; }
            bool soft = aces > 0;
            var cache = new Dictionary<int, double>();
            return DrawValue(total, soft, player, cache) > StandValue(total, player) + .000001;
        }

        static double StandValue(int total, int player) => total > 21 ? -1 : total > player ? 1 : total == player ? 0 : -1;
        static double Best(int total, bool soft, int player, Dictionary<int, double> cache)
        {
            if (total >= 21) return StandValue(total, player);
            int key = total * 2 + (soft ? 1 : 0);
            if (cache.TryGetValue(key, out double value)) return value;
            value = System.Math.Max(StandValue(total, player), DrawValue(total, soft, player, cache));
            cache[key] = value; return value;
        }
        static double DrawValue(int total, bool soft, int player, Dictionary<int, double> cache)
        {
            double value = 0;
            for (int rank = 1; rank <= 10; rank++)
            {
                int next = total + (rank == 1 ? 1 : rank);
                bool nextSoft = soft;
                if (rank == 1 && next <= 11) { next += 10; nextSoft = true; }
                if (next > 21 && nextSoft) { next -= 10; nextSoft = false; }
                value += (rank == 10 ? 4 : 1) / 13.0 * Best(next, nextSoft, player, cache);
            }
            return value;
        }
    }
}
