using System.Collections.Generic;
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

        public int CalculateLaneScore(int lane)
        {
            if (lane >= 1 && lane <= 6)
            {
                int score = 0;
                foreach (var card in Lanes[lane - 1])
                {
                    score += card.Number;
                }
                return score;
            }
            return 0;
        }

        public string EvaluateGame()
        {
            int player1Lanes = 0;
            int player2Lanes = 0;

            bool lane1InRange = IsLaneScoreInRange(1, 21, 26);
            bool lane4InRange = IsLaneScoreInRange(4, 21, 26);
            if (lane1InRange && !lane4InRange) player1Lanes++;
            if (!lane1InRange && lane4InRange) player2Lanes++;

            bool lane2InRange = IsLaneScoreInRange(2, 21, 26);
            bool lane5InRange = IsLaneScoreInRange(5, 21, 26);
            if (lane2InRange && !lane5InRange) player1Lanes++;
            if (!lane2InRange && lane5InRange) player2Lanes++;

            bool lane3InRange = IsLaneScoreInRange(3, 21, 26);
            bool lane6InRange = IsLaneScoreInRange(6, 21, 26);
            if (lane3InRange && !lane6InRange) player1Lanes++;
            if (!lane3InRange && lane6InRange) player2Lanes++;

            if ((lane1InRange || lane4InRange) && (lane2InRange || lane5InRange) && (lane3InRange || lane6InRange))
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
