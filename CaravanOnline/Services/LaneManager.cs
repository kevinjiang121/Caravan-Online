using System.Collections.Generic;
using CaravanOnline.Models;

namespace CaravanOnline.Services
{
    public class LaneManager
    {
        public List<List<Card>> Lanes { get; set; }
        public string LastFailReason { get; private set; } = string.Empty;

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

        public bool AddCardToLane(int lane, Card card)
        {
            LastFailReason = string.Empty;
            if (lane < 1 || lane > 6)
            {
                LastFailReason = "Invalid lane number.";
                return false;
            }

            var laneIndex = lane - 1;
            var currentLane = Lanes[laneIndex];

            if (currentLane.Count == 0)
            {
                currentLane.Add(card);
                return true;
            }

            if (currentLane.Count == 1)
            {
                var firstCard = currentLane[0];
                currentLane.Add(card);
                if (card.Number > firstCard.Number)
                {
                    card.Direction = "up";
                }
                else if (card.Number < firstCard.Number)
                {
                    card.Direction = "down";
                }
                else
                {
                    card.Direction = "up";
                }
                return true;
            }

            var lastCard = currentLane[currentLane.Count - 1];
            if (lastCard.Direction == "up")
            {
                if (card.Number <= lastCard.Number)
                {
                    LastFailReason = $"Invalid move: {card.Face} must be higher than {lastCard.Face} in 'up' lane.";
                    return false;
                }
                card.Direction = "up";
            }
            else if (lastCard.Direction == "down")
            {
                if (card.Number >= lastCard.Number)
                {
                    LastFailReason = $"Invalid move: {card.Face} must be lower than {lastCard.Face} in 'down' lane.";
                    return false;
                }
                card.Direction = "down";
            }
            else
            {
                card.Direction = "up";
            }

            currentLane.Add(card);
            return true;
        }

        public int CalculateLaneScore(int lane)
        {
            if (lane < 1 || lane > 6) return 0;
            int score = 0;
            foreach (var card in Lanes[lane - 1])
            {
                score += card.Number;
            }
            return score;
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
