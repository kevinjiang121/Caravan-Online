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
            new List<string>(), // Lane 1
            new List<string>(), // Lane 2
            new List<string>(), // Lane 3
            new List<string>(), // Lane 4
            new List<string>(), // Lane 5
            new List<string>()  // Lane 6
        };
    }

    public void AddCardToLane(int lane, string card)
    {
        if (lane >= 1 && lane <= Lanes.Count)
        {
            Lanes[lane - 1].Add(card);
        }
    }

    public string EvaluateGame()
    {
        int scorePlayer1 = 0, scorePlayer2 = 0;
        for (int i = 0; i < 3; i++)
        {
            if (Lanes[i].Count < 1 || Lanes[i + 3].Count < 1) continue; // Ensure both players have played in these lanes
            var winner = CardManager.CompareCards(Lanes[i][0], Lanes[i + 3][0]);
            if (winner == "Player 1") scorePlayer1++;
            else if (winner == "Player 2") scorePlayer2++;
        }

        if (scorePlayer1 > scorePlayer2) return "Player 1 wins the game!";
        else if (scorePlayer2 > scorePlayer1) return "Player 2 wins the game!";
        else return "It's a tie!";
    }
}

}
