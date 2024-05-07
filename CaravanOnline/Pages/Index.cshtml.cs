using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;

public class IndexModel : PageModel
{
    // Initialize properties directly to ensure they are never null
    public List<string> PlayerCards { get; set; } = new List<string>();
    public string Message { get; set; } = "Select a card:"; // Default message

    public void OnGet()
    {
        PlayerCards = GetRandomCards();
    }

    public void OnPost(string selectedCard)
    {
        // Example of handling a selection and determining a result
        Message = $"You selected {selectedCard}. Now it's your opponent's turn.";
        // Here, you could implement additional logic to handle opponent's turn and determine the winner
    }

    private List<string> GetRandomCards()
    {
        var cards = new List<string> { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
        var random = new Random();
        return cards.OrderBy(x => random.Next()).Take(5).ToList();
    }
}
