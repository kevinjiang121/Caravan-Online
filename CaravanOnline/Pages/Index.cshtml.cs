using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;

public class IndexModel : PageModel
{
    public List<string> Player1Cards { get; set; } = new List<string>();
    public List<string> Player2Cards { get; set; } = new List<string>();
    public Dictionary<int, List<string>> Lanes { get; set; } = new Dictionary<int, List<string>>()
    {
        {1, new List<string>()}, 
        {2, new List<string>()},
        {3, new List<string>()}
    };
    public string Message { get; set; } = "Player 1, select a card for Lane 1:";
    public int CurrentLane { get; set; } = 1;

    public void OnGet()
    {
        if (!HttpContext.Session.Keys.Contains("Initialized"))
        {
            Player1Cards = GetRandomCards();
            Player2Cards = GetRandomCards();
            SaveGameState();
            HttpContext.Session.SetString("Initialized", "true");
            HttpContext.Session.SetString("CurrentPlayer", "Player 1");
        }
        else
        {
            LoadGameState();
        }
    }

    public void OnPost(string selectedCard)
    {
        LoadGameState();

        var currentPlayer = HttpContext.Session.GetString("CurrentPlayer");
        if (currentPlayer == "Player 1")
        {
            Lanes[CurrentLane].Add(selectedCard);
            Player1Cards.Remove(selectedCard); 
            HttpContext.Session.SetString("CurrentPlayer", "Player 2");
            Message = $"Player 2, select a card for Lane {CurrentLane}:";
        }
        else
        {
            Lanes[CurrentLane].Add(selectedCard); 
            Player2Cards.Remove(selectedCard);    

            if (CurrentLane < 3)
            {
                CurrentLane++;
                HttpContext.Session.SetString("CurrentPlayer", "Player 1");
                Message = $"Player 1, select a card for Lane {CurrentLane}:";
            }
            else
            {
                EvaluateGame();
                HttpContext.Session.Clear(); // End of game, clear session
                Message = "Game over. Refresh to start a new game.";
                return;
            }
        }

        SaveGameState();
    }

    private string CompareCards(string card1, string card2)
    {
        var cardValues = new Dictionary<string, int>
        {
            ["A"] = 1, ["K"] = 13, ["Q"] = 12, ["J"] = 11, ["10"] = 10,
            ["9"] = 9, ["8"] = 8, ["7"] = 7, ["6"] = 6, ["5"] = 5, ["4"] = 4, ["3"] = 3, ["2"] = 2
        };

        if (string.IsNullOrEmpty(card1) || string.IsNullOrEmpty(card2) ||
            !cardValues.ContainsKey(card1) || !cardValues.ContainsKey(card2))
        {
            return "Error: Invalid card(s) provided.";
        }

        return cardValues[card1] > cardValues[card2] ? "Player 1" : "Player 2";
    }

    private void EvaluateGame()
    {
        int scorePlayer1 = 0, scorePlayer2 = 0;
        foreach (var lane in Lanes)
        {
            if (lane.Value.Count < 2) continue; 

            var winner = CompareCards(lane.Value[0], lane.Value[1]);
            if (winner == "Player 1") scorePlayer1++;
            else if (winner == "Player 2") scorePlayer2++;
            else if (winner.Contains("Error")) 
            {
                Message = winner; 
                return;
            }
        }

        if (scorePlayer1 >= 2) Message = "Player 1 wins the game!";
        else if (scorePlayer2 >= 2) Message = "Player 2 wins the game!";
        else Message = "It's a tie!";
    }

    private List<string> GetRandomCards()
    {
        var cards = new List<string> { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
        var random = new Random();
        return cards.OrderBy(x => random.Next()).Take(5).ToList();
    }

    private void SaveGameState()
    {
        HttpContext.Session.SetString("Player1Cards", string.Join(",", Player1Cards));
        HttpContext.Session.SetString("Player2Cards", string.Join(",", Player2Cards));
        HttpContext.Session.SetInt32("CurrentLane", CurrentLane);
        foreach (var lane in Lanes.Keys)
        {
            HttpContext.Session.SetString($"Lane{lane}", string.Join(",", Lanes[lane]));
        }
    }

    private void LoadGameState()
    {
        Player1Cards = HttpContext.Session.GetString("Player1Cards").Split(',').ToList();
        Player2Cards = HttpContext.Session.GetString("Player2Cards").Split(',').ToList();
        CurrentLane = HttpContext.Session.GetInt32("CurrentLane") ?? 1;
        foreach (var lane in Lanes.Keys)
        {
            Lanes[lane] = HttpContext.Session.GetString($"Lane{lane}")?.Split(',').ToList() ?? new List<string>();
        }
    }
}
