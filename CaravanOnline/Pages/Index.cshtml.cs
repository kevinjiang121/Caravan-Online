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
        public int Phase { get; set; } = 1;

        public List<List<Card>> Lanes => _laneManager.Lanes;
        public Card SelectedCardPhase2 { get; set; }

        public IndexModel(LaneManager laneManager, CardManager cardManager)
        {
            _laneManager = laneManager;
            _cardManager = cardManager;
        }

        public void OnGet()
        {
            if (HttpContext.Session.GetString("Initialized") != "true")
            {
                Player1Cards = _cardManager.GetRandomCards(8); 
                Player2Cards = _cardManager.GetRandomCards(8); 
                HttpContext.Session.SetString("Player1Cards", SerializationHelper.SerializePlayerCards(Player1Cards));
                HttpContext.Session.SetString("Player2Cards", SerializationHelper.SerializePlayerCards(Player2Cards));
                HttpContext.Session.SetString("CurrentPlayer", "Player 1");
                HttpContext.Session.SetInt32("CurrentLane", CurrentLane);
                HttpContext.Session.SetInt32("Phase", Phase);
                HttpContext.Session.SetString("Lanes", SerializationHelper.SerializeLanes(_laneManager.Lanes));
                HttpContext.Session.SetString("Initialized", "true");
            }
            else
            {
                var player1CardsSerialized = HttpContext.Session.GetString("Player1Cards") ?? string.Empty;
                var player2CardsSerialized = HttpContext.Session.GetString("Player2Cards") ?? string.Empty;
                var serializedLanes = HttpContext.Session.GetString("Lanes") ?? string.Empty;

                Player1Cards = SerializationHelper.DeserializePlayerCards(player1CardsSerialized);
                Player2Cards = SerializationHelper.DeserializePlayerCards(player2CardsSerialized);
                CurrentLane = HttpContext.Session.GetInt32("CurrentLane").GetValueOrDefault(1);
                Phase = HttpContext.Session.GetInt32("Phase").GetValueOrDefault(1);
                Message = HttpContext.Session.GetString("Message") ?? "Welcome to the game!";
                if (!string.IsNullOrEmpty(serializedLanes))
                {
                    _laneManager.Lanes = SerializationHelper.DeserializeLanes(serializedLanes);
                }
            }
        }

        public IActionResult OnPost(string selectedCard = null, string selectedLane = null)
        {
            CurrentLane = HttpContext.Session.GetInt32("CurrentLane").GetValueOrDefault(1);
            Phase = HttpContext.Session.GetInt32("Phase").GetValueOrDefault(1);
            string currentPlayer = HttpContext.Session.GetString("CurrentPlayer") ?? "Player 1";

            var serializedLanes = HttpContext.Session.GetString("Lanes") ?? string.Empty;
            if (!string.IsNullOrEmpty(serializedLanes))
            {
                _laneManager.Lanes = SerializationHelper.DeserializeLanes(serializedLanes);
            }

            if (Phase == 1)
            {
                var selectedCardParts = selectedCard?.Split(' ') ?? new string[0];
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
                    var player1CardsSerialized = HttpContext.Session.GetString("Player1Cards") ?? string.Empty;
                    Player1Cards = SerializationHelper.DeserializePlayerCards(player1CardsSerialized);
                    card = Player1Cards.FirstOrDefault(c => c.Face == selectedCardFace && c.Suit == selectedCardSuit);
                    if (card != null)
                    {
                        Player1Cards.Remove(card);
                        HttpContext.Session.SetString("Player1Cards", SerializationHelper.SerializePlayerCards(Player1Cards));
                    }
                }
                else
                {
                    var player2CardsSerialized = HttpContext.Session.GetString("Player2Cards") ?? string.Empty;
                    Player2Cards = SerializationHelper.DeserializePlayerCards(player2CardsSerialized);
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
                    Phase = 2; 
                    HttpContext.Session.SetInt32("Phase", Phase);
                }
                else
                {
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
                    HttpContext.Session.SetString("CurrentPlayer", currentPlayer == "Player 1" ? "Player 2" : "Player 1");
                }

                HttpContext.Session.SetString("Lanes", SerializationHelper.SerializeLanes(_laneManager.Lanes));
                HttpContext.Session.SetString("Message", Message);

                return RedirectToPage();
            }
            else if (Phase == 2)
            {
                if (!string.IsNullOrEmpty(selectedCard))
                {
                    var selectedCardParts = selectedCard.Split(' ');
                    if (selectedCardParts.Length < 2)
                    {
                        Message = "Invalid card selected.";
                        return Page();
                    }
                    var selectedCardFace = selectedCardParts[0];
                    var selectedCardSuit = selectedCardParts[1];

                    if (currentPlayer == "Player 1")
                    {
                        var player1CardsSerialized = HttpContext.Session.GetString("Player1Cards") ?? string.Empty;
                        Player1Cards = SerializationHelper.DeserializePlayerCards(player1CardsSerialized);
                        SelectedCardPhase2 = Player1Cards.FirstOrDefault(c => c.Face == selectedCardFace && c.Suit == selectedCardSuit);
                    }
                    else
                    {
                        var player2CardsSerialized = HttpContext.Session.GetString("Player2Cards") ?? string.Empty;
                        Player2Cards = SerializationHelper.DeserializePlayerCards(player2CardsSerialized);
                        SelectedCardPhase2 = Player2Cards.FirstOrDefault(c => c.Face == selectedCardFace && c.Suit == selectedCardSuit);
                    }

                    if (SelectedCardPhase2 == null)
                    {
                        Message = "Card not found.";
                        return Page();
                    }

                    HttpContext.Session.SetString("SelectedCardPhase2", SerializationHelper.SerializePlayerCards(new List<Card> { SelectedCardPhase2 }));
                }
                else if (!string.IsNullOrEmpty(selectedLane))
                {
                    var serializedSelectedCard = HttpContext.Session.GetString("SelectedCardPhase2");
                    if (string.IsNullOrEmpty(serializedSelectedCard))
                    {
                        Message = "No card selected.";
                        return Page();
                    }

                    SelectedCardPhase2 = SerializationHelper.DeserializePlayerCards(serializedSelectedCard).FirstOrDefault();
                    if (SelectedCardPhase2 == null)
                    {
                        Message = "Invalid card.";
                        return Page();
                    }

                    int laneNumber;
                    if (!int.TryParse(selectedLane, out laneNumber) || laneNumber < 1 || laneNumber > 6)
                    {
                        Message = "Invalid lane selected.";
                        return Page();
                    }

                    _laneManager.AddCardToLane(laneNumber, SelectedCardPhase2);

                    if (currentPlayer == "Player 1")
                    {
                        Player1Cards.Remove(SelectedCardPhase2);
                        HttpContext.Session.SetString("Player1Cards", SerializationHelper.SerializePlayerCards(Player1Cards));
                    }
                    else
                    {
                        Player2Cards.Remove(SelectedCardPhase2);
                        HttpContext.Session.SetString("Player2Cards", SerializationHelper.SerializePlayerCards(Player2Cards));
                    }

                    if (_laneManager.Lanes.All(lane => lane.Count >= 4))
                    {
                        var result = _laneManager.EvaluateGame();
                        Message = result;
                        HttpContext.Session.Clear();
                        return Page();
                    }

                    HttpContext.Session.SetString("Lanes", SerializationHelper.SerializeLanes(_laneManager.Lanes));
                    HttpContext.Session.SetString("Message", Message);
                    HttpContext.Session.SetString("CurrentPlayer", currentPlayer == "Player 1" ? "Player 2" : "Player 1");
                    HttpContext.Session.Remove("SelectedCardPhase2");

                    return RedirectToPage();
                }
                else
                {
                    Message = "No card or lane selected.";
                }
            }

            return Page();
        }
    }
}
