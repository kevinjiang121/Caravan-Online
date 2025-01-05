using System.Collections.Generic;
using System.Linq;
using CaravanOnline.Models;

namespace CaravanOnline.Services
{
    public class LaneManager
    {
        public List<List<Card>> Lanes { get; set; }

        public LaneManager()
        {
            Lanes = new List<List<Card>>
            {
                new List<Card>(),
                new List<Card>(),
                new List<Card>(),
                new List<Card>(),
                new List<Card>(),
                new List<Card>()
            };
        }

        public void AddCardToLane(int lane, Card card)
        {
            if (lane >= 1 && lane <= 6)
            {
                Lanes[lane - 1].Add(card);
            }
        }

        /// <summary>
        /// Calculates the score for a lane by summing each card's "effective" value.
        /// An attached King doubles the base card’s value (no extra face-value added).
        /// Multiple Kings stack multiplicatively.
        /// Jacks & Queens have no effect yet.
        /// </summary>
        public int CalculateLaneScore(int lane)
        {
            if (lane < 1 || lane > 6) return 0;

            int score = 0;
            foreach (var card in Lanes[lane - 1])
            {
                score += GetEffectiveValue(card);
            }
            return score;
        }

        /// <summary>
        /// Returns the card's value, multiplied by 2 for each attached King.
        /// Ignores attached card face-values for now (Jacks, Queens, or normal cards).
        /// </summary>
        private int GetEffectiveValue(Card card)
        {
            // Count how many Kings are attached.
            int kingCount = card.AttachedCards.Count(a => a.Face == "K");

            // Multiply base card's value by 2^kingCount.
            // Example: 10 with 2 Kings => 10 * (2^2) = 40
            return card.Number * (int)System.Math.Pow(2, kingCount);
        }

        /// <summary>
        /// Example: Evaluates whether lanes 1-3 vs. 4-6 are within 21-26.
        /// </summary>
        public string EvaluateGame()
        {
            int player1Lanes = 0;
            int player2Lanes = 0;

            // Compare lane 1 vs. 4
            bool lane1InRange = IsLaneScoreInRange(1, 21, 26);
            bool lane4InRange = IsLaneScoreInRange(4, 21, 26);
            if (lane1InRange && !lane4InRange) player1Lanes++;
            if (!lane1InRange && lane4InRange) player2Lanes++;

            // Compare lane 2 vs. 5
            bool lane2InRange = IsLaneScoreInRange(2, 21, 26);
            bool lane5InRange = IsLaneScoreInRange(5, 21, 26);
            if (lane2InRange && !lane5InRange) player1Lanes++;
            if (!lane2InRange && lane5InRange) player2Lanes++;

            // Compare lane 3 vs. 6
            bool lane3InRange = IsLaneScoreInRange(3, 21, 26);
            bool lane6InRange = IsLaneScoreInRange(6, 21, 26);
            if (lane3InRange && !lane6InRange) player1Lanes++;
            if (!lane3InRange && lane6InRange) player2Lanes++;

            // If all three pairs have at least one lane in range, decide outcome
            if ((lane1InRange || lane4InRange) &&
                (lane2InRange || lane5InRange) &&
                (lane3InRange || lane6InRange))
            {
                if (player1Lanes > player2Lanes) return "Player 1 wins!";
                if (player2Lanes > player1Lanes) return "Player 2 wins!";
                return "It's a tie!";
            }

            return "The game is still ongoing.";
        }

        private bool IsLaneScoreInRange(int lane, int min, int max)
        {
            int score = CalculateLaneScore(lane);
            return score >= min && score <= max;
        }
    }
}
