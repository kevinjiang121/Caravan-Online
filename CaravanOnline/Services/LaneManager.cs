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
            return "";
        }
    }
}
