using System.Collections.Generic;

namespace CaravanOnline.Services
{
    public class LaneManager
    {
        public List<List<string>> Lanes { get; set; }

        public LaneManager()
        {
            Lanes = new List<List<string>>
            {
                new List<string>(), 
                new List<string>(), 
                new List<string>()  
            };
        }

        public void AddCardToLane(int lane, string card)
        {
            if (lane >= 1 && lane <= Lanes.Count)
            {
                Console.WriteLine(card);
                Lanes[lane - 1].Add(card);
            }    
        }

        public string EvaluateGame()
        {
            int scorePlayer1 = 0, scorePlayer2 = 0;
            foreach (var lane in Lanes)
            {
                if (lane.Count < 2) continue; 
                var winner = CardManager.CompareCards(lane[0], lane[1]);
                if (winner == "Player 1") scorePlayer1++;
                else if (winner == "Player 2") scorePlayer2++;
            }

            if (scorePlayer1 >= 2) return "Player 1 wins the game!";
            else if (scorePlayer2 >= 2) return "Player 2 wins the game!";
            else return "It's a tie!";
        }
    }
}
