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

        public List<List<string>> Lanes => _laneManager.Lanes;

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
                HttpContext.Session.SetString("Lanes", SerializeLanes(_laneManager.Lanes));
                HttpContext.Session.SetString("Initialized", "true");
            }
            else
            {
                Player1Cards = HttpContext.Session.GetString("Player1Cards")?.Split(',').ToList() ?? new List<string>();
                Player2Cards = HttpContext.Session.GetString("Player2Cards")?.Split(',').ToList() ?? new List<string>();
                CurrentLane = HttpContext.Session.GetInt32("CurrentLane").GetValueOrDefault(1);
                Message = HttpContext.Session.GetString("Message") ?? "Welcome to the game!";

                var serializedLanes = HttpContext.Session.GetString("Lanes");
                if (!string.IsNullOrEmpty(serializedLanes))
                {
                    _laneManager.Lanes = DeserializeLanes(serializedLanes);
                }
            }
        }

        public IActionResult OnPost(string selectedCard)
        {
            CurrentLane = HttpContext.Session.GetInt32("CurrentLane").GetValueOrDefault(1);
            string currentPlayer = HttpContext.Session.GetString("CurrentPlayer");

            var serializedLanes = HttpContext.Session.GetString("Lanes");
            if (!string.IsNullOrEmpty(serializedLanes))
            {
                _laneManager.Lanes = DeserializeLanes(serializedLanes);
            }

            _laneManager.AddCardToLane(CurrentLane, selectedCard);

            if (_laneManager.Lanes.All(lane => lane.Count >= 2))
            {
                var result = _laneManager.EvaluateGame();
                Message = result;
                HttpContext.Session.Clear(); 
                return Page();
            }

            if (_laneManager.Lanes[CurrentLane - 1].Count >= 2)
            {
                CurrentLane++;
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
            HttpContext.Session.SetString("Lanes", SerializeLanes(_laneManager.Lanes));

            return RedirectToPage();
        }

        private string SerializeLanes(List<List<string>> lanes)
        {
            return string.Join(";", lanes.Select(l => string.Join(",", l)));
        }

        private List<List<string>> DeserializeLanes(string serializedLanes)
        {
            var lanes = new List<List<string>>();
            var laneEntries = serializedLanes.Split(';');

            foreach (var laneEntry in laneEntries)
            {
                if (!string.IsNullOrWhiteSpace(laneEntry))
                {
                    lanes.Add(laneEntry.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList());
                }
                else
                {
                    lanes.Add(new List<string>());
                }
            }

            return lanes;
        }
    }
}
