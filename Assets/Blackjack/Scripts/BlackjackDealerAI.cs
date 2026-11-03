using System.Collections.Generic;

namespace Blackjack
{
    /// <summary>Stable policy identifiers used by the menu, rules and saved preference.</summary>
    public enum DealerDifficulty { Easy, Medium, Hard }

    // Hard maximizes win/loss expectation using a fixed six-deck rank distribution.
    // It sees the completed player hand, but never receives the shoe or next card.
    /// <summary>Dealer draw policies and Hard-mode expected outcome calculations.</summary>
    public static class BlackjackDealerAI
    {
        /// <summary>Returns display text for a difficulty. Editing this text does not change dealer decisions.</summary>
        /// <param name="difficulty">Dealer policy enum. A different mode changes drawing strategy; enum numbers also back the saved preference and menu order.</param>
        public static string Rule(DealerDifficulty difficulty) => difficulty == DealerDifficulty.Easy
            ? "EASY / STANDS ON 15+" : difficulty == DealerDifficulty.Hard
            ? "HARD / ADAPTIVE DEALER" : "MEDIUM / STANDS ON 17+";

        /// <summary>Chooses whether the dealer draws. Easy hits below 15, Medium below 17, and Hard compares expected outcomes without reading the shoe.</summary>
        /// <param name="difficulty">Dealer policy enum. A different mode changes drawing strategy; enum numbers also back the saved preference and menu order.</param>
        /// <param name="dealer">Dealer's visible hand for the decision. Changing its ranks changes total and soft-ace status.</param>
        /// <param name="player">Player's final hand total. Hard uses this target; Easy and Medium ignore it.</param>
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

        /// <summary>Scores stopping now as a dealer win (+1), tie (0), or loss (-1). Changing these scores changes what Hard tries to optimize.</summary>
        /// <param name="total">Current hypothetical dealer total. A value above 21 is a bust; a larger total can improve a standing outcome but reduce drawing room.</param>
        /// <param name="player">Player's final hand total. Hard uses this target; Easy and Medium ignore it.</param>
        static double StandValue(int total, int player) => total > 21 ? -1 : total > player ? 1 : total == player ? 0 : -1;
        /// <summary>Computes the better expected outcome of standing or drawing, caching results for the current player total.</summary>
        /// <param name="total">Current hypothetical dealer total. A value above 21 is a bust; a larger total can improve a standing outcome but reduce drawing room.</param>
        /// <param name="soft">Whether one ace is currently valued at 11. True allows a prospective draw to downgrade it to 1.</param>
        /// <param name="player">Player's final hand total. Hard uses this target; Easy and Medium ignore it.</param>
        /// <param name="cache">Memo of expected outcomes keyed by total and soft status. Use a fresh cache per player total or the values may be stale.</param>
        static double Best(int total, bool soft, int player, Dictionary<int, double> cache)
        {
            if (total >= 21) return StandValue(total, player);
            int key = total * 2 + (soft ? 1 : 0);
            if (cache.TryGetValue(key, out double value)) return value;
            value = System.Math.Max(StandValue(total, player), DrawValue(total, soft, player, cache));
            cache[key] = value; return value;
        }
        /// <summary>Averages the best future outcome over possible next ranks. Ten-value cards carry four times the weight of a single rank.</summary>
        /// <param name="total">Current hypothetical dealer total. A value above 21 is a bust; a larger total can improve a standing outcome but reduce drawing room.</param>
        /// <param name="soft">Whether one ace is currently valued at 11. True allows a prospective draw to downgrade it to 1.</param>
        /// <param name="player">Player's final hand total. Hard uses this target; Easy and Medium ignore it.</param>
        /// <param name="cache">Memo of expected outcomes keyed by total and soft status. Use a fresh cache per player total or the values may be stale.</param>
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
