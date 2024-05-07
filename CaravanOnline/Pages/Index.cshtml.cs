using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

public class IndexModel : PageModel
{
    public List<string>? Player1Cards { get; set; } = new List<string>();
    public List<string>? Player2Cards { get; set; } = new List<string>();
    public string Message { get; set; } = "Player 1, select a card:";

    public void OnGet()
    {
        HttpContext.Session.Clear();
        Player1Cards = GetRandomCards();
        Player2Cards = GetRandomCards();  
        HttpContext.Session.SetString("Player1Cards", string.Join(",", Player1Cards));
        HttpContext.Session.SetString("Player2Cards", string.Join(",", Player2Cards));
        HttpContext.Session.SetString("CurrentPlayer", "Player 1");
    }

    public void OnPost(string selectedCard)
    {
        var currentPlayer = HttpContext.Session.GetString("CurrentPlayer");
        if (currentPlayer == "Player 1")
        {
            HttpContext.Session.SetString("Player1Card", selectedCard);
            HttpContext.Session.SetString("CurrentPlayer", "Player 2");
            Message = "Player 2, select a card:";

            var player2CardsCsv = HttpContext.Session.GetString("Player2Cards");
            Player2Cards = player2CardsCsv.Split(',').ToList();

            Player1Cards = null;
        }
        else
        {
            var player1Card = HttpContext.Session.GetString("Player1Card");
            var player2CardsCsv = HttpContext.Session.GetString("Player2Cards");
            Player2Cards = player2CardsCsv.Split(',').ToList();

            var winner = CompareCards(player1Card, selectedCard);
            Message = $"Player 1 selected {player1Card}, Player 2 selected {selectedCard}. {winner} wins!";
            HttpContext.Session.Clear(); 
        }
    }


    private List<string> GetRandomCards()
    {
        var cards = new List<string> { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
        var random = new Random();
        return cards.OrderBy(x => random.Next()).Take(5).ToList();
    }

    private string CompareCards(string? card1, string? card2)
    {
        if (card1 == null || card2 == null)
            return "Error: a player's card was not found";

        var cardValues = new Dictionary<string, int>
        {
            ["A"] = 1, ["K"] = 13, ["Q"] = 12, ["J"] = 11, ["10"] = 10,
            ["9"] = 9, ["8"] = 8, ["7"] = 7, ["6"] = 6, ["5"] = 5, ["4"] = 4, ["3"] = 3, ["2"] = 2
        };

        var value1 = cardValues[card1];
        var value2 = cardValues[card2];

        if (value1 > value2)
            return "Player 1";
        else if (value2 > value1)
            return "Player 2";
        else
            return "No one"; 
    }
}
