// Services/LaneManager.cs
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

        /// <summary>
        /// Places a card onto the specified lane, respecting up/down direction rules.
        /// </summary>
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

            // If lane empty, just place the card
            if (currentLane.Count == 0)
            {
                currentLane.Add(card);
                return true;
            }

            // If only 1 card, set direction based on whether new card is bigger/smaller
            if (currentLane.Count == 1)
            {
                var firstCard = currentLane[0];
                currentLane.Add(card);
                if (card.Number > firstCard.Number) card.Direction = "up";
                else if (card.Number < firstCard.Number) card.Direction = "down";
                else card.Direction = "up"; // Default to "up" on tie
                return true;
            }

            // Lane has >= 2 cards. We look at the last card's direction
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
                // If direction somehow not set, default to "up"
                card.Direction = "up";
            }

            currentLane.Add(card);
            return true;
        }

        /// <summary>
        /// Clears (discards) all cards in the specified lane.
        /// </summary>
        public void DiscardLane(int lane)
        {
            if (lane >= 1 && lane <= 6)
            {
                Lanes[lane - 1].Clear();
            }
        }

        /// <summary>
        /// Calculates the lane's total score. 
        /// If a card has N Kings attached, that card’s base value is multiplied by 2^N.
        /// Jacks remove the card from the lane entirely, so it won't appear here.
        /// Queens or other cards attached are ignored unless you add logic for them.
        /// </summary>
        public int CalculateLaneScore(int lane)
        {
            if (lane < 1 || lane > 6) return 0;
            int score = 0;

            foreach (var card in Lanes[lane - 1])
            {
                // Skip cards that have been removed by Jacks
                if (card.Face == "RemovedByJack") continue;

                // Calculate the value considering attached Kings
                int kingCount = card.AttachedCards.Count(a => a.Face == "K");
                int cardValue = card.Number * (int)Math.Pow(2, kingCount);

                // Add to total score
                score += cardValue;
            }
            return score;
        }

        /// <summary>
        /// Evaluates the game state to determine if there's a winner or if the game is ongoing.
        /// </summary>
        public string EvaluateGame()
        {
            int player1Lanes = 0;
            int player2Lanes = 0;

            // Define lane pairings for players
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

            // Determine the winner
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
