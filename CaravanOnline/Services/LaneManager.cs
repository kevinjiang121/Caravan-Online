using System.Collections.Generic;

namespace CaravanOnline.Services
{
    public class LaneManager
    {
        public Dictionary<int, List<string>> Lanes { get; private set; }

        public LaneManager()
        {
            Lanes = new Dictionary<int, List<string>>
            {
                {1, new List<string>()},
                {2, new List<string>()},
                {3, new List<string>()}
            };
        }

        public void AddCardToLane(int lane, string card)
        {
            if (Lanes.ContainsKey(lane))
            {
                Lanes[lane].Add(card);
            }
        }

        public string EvaluateGame()
        {
            int scorePlayer1 = 0, scorePlayer2 = 0;
            foreach (var lane in Lanes)
            {
                if (lane.Value.Count < 2) continue; // Ensure both players have played in this lane
                var winner = CardManager.CompareCards(lane.Value[0], lane.Value[1]);
                if (winner == "Player 1") scorePlayer1++;
                else if (winner == "Player 2") scorePlayer2++;
            }

            if (scorePlayer1 >= 2) return "Player 1 wins the game!";
            else if (scorePlayer2 >= 2) return "Player 2 wins the game!";
            else return "It's a tie!";
        }
    }
}
