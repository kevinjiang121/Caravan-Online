using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CaravanOnline.Services;
using CaravanOnline.Models;
using System.Collections.Generic;
using System.Linq;

namespace CaravanOnline.Pages
{
    public class IndexModel : PageModel
    {
        private readonly LaneManager _laneManager;
        private readonly CardManager _cardManager;

        public List<Card> Player1Cards { get; set; } = new List<Card>();
        public List<Card> Player2Cards { get; set; } = new List<Card>();
        public string Message { get; set; } = "Welcome to the game!";
        public int CurrentLane { get; set; } = 1;

        public List<List<Card>> Lanes => _laneManager.Lanes;

        public IndexModel(LaneManager laneManager, CardManager cardManager)
        {
            _laneManager = laneManager;
            _cardManager = cardManager;
        }

        public void OnGet()
        {
            if (HttpContext.Session.GetString("Initialized") != "true")
            {
                Player1Cards = _cardManager.GetRandomCards();
                Player2Cards = _cardManager.GetRandomCards();
                HttpContext.Session.SetString("Player1Cards", SerializationHelper.SerializePlayerCards(Player1Cards));
                HttpContext.Session.SetString("Player2Cards", SerializationHelper.SerializePlayerCards(Player2Cards));
                HttpContext.Session.SetString("CurrentPlayer", "Player 1");
                HttpContext.Session.SetInt32("CurrentLane", CurrentLane);
                HttpContext.Session.SetString("Lanes", SerializationHelper.SerializeLanes(_laneManager.Lanes));
                HttpContext.Session.SetString("Initialized", "true");
            }
            else
            {
                Player1Cards = SerializationHelper.DeserializePlayerCards(HttpContext.Session.GetString("Player1Cards"));
                Player2Cards = SerializationHelper.DeserializePlayerCards(HttpContext.Session.GetString("Player2Cards"));
                CurrentLane = HttpContext.Session.GetInt32("CurrentLane").GetValueOrDefault(1);
                Message = HttpContext.Session.GetString("Message") ?? "Welcome to the game!";
                var serializedLanes = HttpContext.Session.GetString("Lanes");
                if (!string.IsNullOrEmpty(serializedLanes))
                {
                    _laneManager.Lanes = SerializationHelper.DeserializeLanes(serializedLanes);
                }
            }
        }

        public IActionResult OnPost(string selectedCard)
        {
            CurrentLane = HttpContext.Session.GetInt32("CurrentLane").GetValueOrDefault(1);
            string currentPlayer = HttpContext.Session.GetString("CurrentPlayer") ?? "Player 1";

            var serializedLanes = HttpContext.Session.GetString("Lanes");
            if (!string.IsNullOrEmpty(serializedLanes))
            {
                _laneManager.Lanes = SerializationHelper.DeserializeLanes(serializedLanes);
            }

            var selectedCardParts = selectedCard.Split(' ');
            if (selectedCardParts.Length < 2)
            {
                Message = "Invalid card selected.";
                return Page();
            }
            var selectedCardFace = selectedCardParts[0];
            var selectedCardSuit = selectedCardParts[1];

            Card card = null;
            if (currentPlayer == "Player 1")
            {
                Player1Cards = SerializationHelper.DeserializePlayerCards(HttpContext.Session.GetString("Player1Cards"));
                card = Player1Cards.FirstOrDefault(c => c.Face == selectedCardFace && c.Suit == selectedCardSuit);
                if (card != null)
                {
                    Player1Cards.Remove(card);
                    HttpContext.Session.SetString("Player1Cards", SerializationHelper.SerializePlayerCards(Player1Cards));
                }
            }
            else
            {
                Player2Cards = SerializationHelper.DeserializePlayerCards(HttpContext.Session.GetString("Player2Cards"));
                card = Player2Cards.FirstOrDefault(c => c.Face == selectedCardFace && c.Suit == selectedCardSuit);
                if (card != null)
                {
                    Player2Cards.Remove(card);
                    HttpContext.Session.SetString("Player2Cards", SerializationHelper.SerializePlayerCards(Player2Cards));
                }
            }

            if (card == null)
            {
                Message = "Card not found.";
                return Page();
            }

            _laneManager.AddCardToLane(CurrentLane, card);

            if (_laneManager.Lanes.All(lane => lane.Count >= 1))
            {
                var result = _laneManager.EvaluateGame();
                Message = result;
                HttpContext.Session.Clear();
                return Page();
            }

            if (currentPlayer == "Player 1")
            {
                if (CurrentLane == 1) CurrentLane = 4;
                else if (CurrentLane == 2) CurrentLane = 5;
                else if (CurrentLane == 3) CurrentLane = 6;
            }
            else
            {
                if (CurrentLane == 4) CurrentLane = 2;
                else if (CurrentLane == 5) CurrentLane = 3;
                else if (CurrentLane == 6) CurrentLane = 1;
            }

            HttpContext.Session.SetInt32("CurrentLane", CurrentLane);
            HttpContext.Session.SetString("Message", Message);
            HttpContext.Session.SetString("Lanes", SerializationHelper.SerializeLanes(_laneManager.Lanes));
            HttpContext.Session.SetString("CurrentPlayer", currentPlayer == "Player 1" ? "Player 2" : "Player 1");

            return RedirectToPage();
        }
    }
}
