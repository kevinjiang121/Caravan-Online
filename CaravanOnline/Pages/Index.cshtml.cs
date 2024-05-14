using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CaravanOnline.Services;
using System.Collections.Generic;
using System.Linq;

namespace CaravanOnline.Pages
{
    public class IndexModel : PageModel
    {
        private readonly LaneManager _laneManager;

        public List<string> Player1Cards { get; set; } = new List<string>();
        public List<string> Player2Cards { get; set; } = new List<string>();
        public string Message { get; set; } = "Welcome to the game!";
        public int CurrentLane { get; set; } = 1;

        public Dictionary<int, List<string>> Lanes => _laneManager.Lanes;

        public IndexModel(LaneManager laneManager)
        {
            _laneManager = laneManager;
        }

        public void OnGet()
        {
            if (HttpContext.Session.GetString("Initialized") != "true")
            {
                Player1Cards = CardManager.GetRandomCards(); 
                Player2Cards = CardManager.GetRandomCards(); 
                HttpContext.Session.SetString("Player1Cards", string.Join(",", Player1Cards));
                HttpContext.Session.SetString("Player2Cards", string.Join(",", Player2Cards));
                HttpContext.Session.SetString("CurrentPlayer", "Player 1");
                HttpContext.Session.SetInt32("CurrentLane", CurrentLane);
                HttpContext.Session.SetString("Initialized", "true");
            }
            else
            {
                // Retrieve cards from session
                Player1Cards = HttpContext.Session.GetString("Player1Cards")?.Split(',').ToList() ?? new List<string>();
                Player2Cards = HttpContext.Session.GetString("Player2Cards")?.Split(',').ToList() ?? new List<string>();
                CurrentLane = HttpContext.Session.GetInt32("CurrentLane").GetValueOrDefault(1);
                Message = HttpContext.Session.GetString("Message") ?? "Welcome to the game!";
            }
        }

        public IActionResult OnPost(string selectedCard)
        {
            string currentPlayer = HttpContext.Session.GetString("CurrentPlayer");

            _laneManager.AddCardToLane(CurrentLane, selectedCard);

            if (_laneManager.Lanes[CurrentLane].Count == 2)
            {
                var result = _laneManager.EvaluateGame();
                ViewData["Result"] = result;
                CurrentLane++;
                Message = result;
            }
            else
            {
                Message = "Waiting for the next player to play.";
            }

            if (currentPlayer == "Player 1")
            {
                Player1Cards = HttpContext.Session.GetString("Player1Cards")?.Split(',').ToList() ?? new List<string>();
                Player1Cards.Remove(selectedCard);
                HttpContext.Session.SetString("Player1Cards", string.Join(",", Player1Cards));
                HttpContext.Session.SetString("CurrentPlayer", "Player 2");
            }
            else
            {
                Player2Cards = HttpContext.Session.GetString("Player2Cards")?.Split(',').ToList() ?? new List<string>();
                Player2Cards.Remove(selectedCard);
                HttpContext.Session.SetString("Player2Cards", string.Join(",", Player2Cards));
                HttpContext.Session.SetString("CurrentPlayer", "Player 1");
            }

            HttpContext.Session.SetInt32("CurrentLane", CurrentLane);
            HttpContext.Session.SetString("Message", Message);

            return RedirectToPage();
        }
    }
}
