using System;
using System.Collections.Generic;
using System.Linq;
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
                if (card.Number > firstCard.Number) card.Direction = "up";
                else if (card.Number < firstCard.Number) card.Direction = "down";
                else card.Direction = "up";
                return true;
            }

            var lastCard = currentLane.Last();
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

        public void DiscardLane(int lane)
        {
            if (lane >= 1 && lane <= 6)
            {
                Lanes[lane - 1].Clear();
            }
        }

        public int CalculateLaneScore(int lane)
        {
            if (lane < 1 || lane > 6) return 0;
            int score = 0;

            foreach (var card in Lanes[lane - 1])
            {
                if (card.Face == "RemovedByJack") continue;
                int kingCount = card.AttachedCards.Count(a => a.Face == "K");
                int cardValue = card.Number * (int)Math.Pow(2, kingCount);
                score += cardValue;
            }
            return score;
        }

        public string EvaluateGame()
        {
            int player1Lanes = 0;
            int player2Lanes = 0;
            var player1LanePairs = new List<(int, int)> { (1, 4), (2, 5), (3, 6) };

            foreach (var pair in player1LanePairs)
            {
                bool lane1InRange = IsLaneScoreInRange(pair.Item1, 21, 26);
                bool lane2InRange = IsLaneScoreInRange(pair.Item2, 21, 26);

                if (lane1InRange && !lane2InRange)
                    player1Lanes++;
                if (!lane1InRange && lane2InRange)
                    player2Lanes++;
            }

            if (player1Lanes > player2Lanes)
                return "Player 1 wins!";
            if (player2Lanes > player1Lanes)
                return "Player 2 wins!";
            if (player1Lanes == player2Lanes && player1Lanes > 0)
                return "It's a tie!";

            return "The game is still ongoing.";
        }

        private bool IsLaneScoreInRange(int lane, int min, int max)
        {
            int score = CalculateLaneScore(lane);
            return score >= min && score <= max;
        }
    }
}
