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
        public Card? SelectedCardPhase2 { get; set; }

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
                Player1Cards = SerializationHelper.DeserializePlayerCards(HttpContext.Session.GetString("Player1Cards") ?? string.Empty);
                Player2Cards = SerializationHelper.DeserializePlayerCards(HttpContext.Session.GetString("Player2Cards") ?? string.Empty);
                CurrentLane = HttpContext.Session.GetInt32("CurrentLane").GetValueOrDefault(1);
                Phase = HttpContext.Session.GetInt32("Phase").GetValueOrDefault(1);
                Message = HttpContext.Session.GetString("Message") ?? "Welcome to the game!";
                var serializedLanes = HttpContext.Session.GetString("Lanes") ?? string.Empty;
                if (!string.IsNullOrEmpty(serializedLanes))
                {
                    _laneManager.Lanes = SerializationHelper.DeserializeLanes(serializedLanes);
                }
            }
        }

        public IActionResult OnPost(string? selectedCard = null, string? selectedLane = null)
        {
            CurrentLane = HttpContext.Session.GetInt32("CurrentLane").GetValueOrDefault(1);
            Phase = HttpContext.Session.GetInt32("Phase").GetValueOrDefault(1);
            string currentPlayer = HttpContext.Session.GetString("CurrentPlayer") ?? "Player 1";

            Player1Cards = SerializationHelper.DeserializePlayerCards(HttpContext.Session.GetString("Player1Cards") ?? string.Empty);
            Player2Cards = SerializationHelper.DeserializePlayerCards(HttpContext.Session.GetString("Player2Cards") ?? string.Empty);

            var serializedLanes = HttpContext.Session.GetString("Lanes") ?? string.Empty;
            if (!string.IsNullOrEmpty(serializedLanes))
            {
                _laneManager.Lanes = SerializationHelper.DeserializeLanes(serializedLanes);
            }

            if (Phase == 1)
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

                    Card? card = null;
                    if (currentPlayer == "Player 1")
                    {
                        card = Player1Cards.FirstOrDefault(c => c.Face == selectedCardFace && c.Suit == selectedCardSuit);
                        if (card != null)
                        {
                            Player1Cards = Player1Cards.Where(c => c.Face != selectedCardFace || c.Suit != selectedCardSuit).ToList();
                            AddRandomCardIfNecessary(currentPlayer);
                        }
                    }
                    else
                    {
                        card = Player2Cards.FirstOrDefault(c => c.Face == selectedCardFace && c.Suit == selectedCardSuit);
                        if (card != null)
                        {
                            Player2Cards = Player2Cards.Where(c => c.Face != selectedCardFace || c.Suit != selectedCardSuit).ToList();
                            AddRandomCardIfNecessary(currentPlayer);
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
                        HttpContext.Session.SetString("CurrentPlayer", "Player 1");
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

                    HttpContext.Session.SetString("Player1Cards", SerializationHelper.SerializePlayerCards(Player1Cards));
                    HttpContext.Session.SetString("Player2Cards", SerializationHelper.SerializePlayerCards(Player2Cards));

                    return RedirectToPage();
                }
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
                        SelectedCardPhase2 = Player1Cards.FirstOrDefault(c => c.Face == selectedCardFace && c.Suit == selectedCardSuit);
                    }
                    else
                    {
                        SelectedCardPhase2 = Player2Cards.FirstOrDefault(c => c.Face == selectedCardFace && c.Suit == selectedCardSuit);
                    }

                    if (SelectedCardPhase2 == null)
                    {
                        Message = "Card not found.";
                        return Page();
                    }

                    HttpContext.Session.SetString("SelectedCardPhase2", SerializationHelper.SerializePlayerCards(new List<Card> { SelectedCardPhase2 }));
                    Message = "Please select a lane";
                }
                else if (!string.IsNullOrEmpty(selectedLane))
                {
                    var serializedSelectedCard = HttpContext.Session.GetString("SelectedCardPhase2") ?? string.Empty;
                    if (string.IsNullOrEmpty(serializedSelectedCard))
                    {
                        Message = "Please select a card first.";
                        return Page();
                    }

                    SelectedCardPhase2 = SerializationHelper.DeserializePlayerCards(serializedSelectedCard).FirstOrDefault();
                    if (SelectedCardPhase2 == null)
                    {
                        Message = "Invalid card.";
                        return Page();
                    }

                    if (!int.TryParse(selectedLane, out int laneNumber) || laneNumber < 1 || laneNumber > 6)
                    {
                        Message = "Invalid lane selected.";
                        return Page();
                    }

                    _laneManager.AddCardToLane(laneNumber, SelectedCardPhase2);

                    if (currentPlayer == "Player 1")
                    {
                        Player1Cards = Player1Cards.Where(c => c.Face != SelectedCardPhase2.Face || c.Suit != SelectedCardPhase2.Suit).ToList();
                        AddRandomCardIfNecessary(currentPlayer);
                    }
                    else
                    {
                        Player2Cards = Player2Cards.Where(c => c.Face != SelectedCardPhase2.Face || c.Suit != SelectedCardPhase2.Suit).ToList();
                        AddRandomCardIfNecessary(currentPlayer);
                    }

                    HttpContext.Session.Remove("SelectedCardPhase2");

                    HttpContext.Session.SetString("Player1Cards", SerializationHelper.SerializePlayerCards(Player1Cards));
                    HttpContext.Session.SetString("Player2Cards", SerializationHelper.SerializePlayerCards(Player2Cards));
                    HttpContext.Session.SetString("Lanes", SerializationHelper.SerializeLanes(_laneManager.Lanes));
                    HttpContext.Session.SetString("Message", Message);
                    HttpContext.Session.SetString("CurrentPlayer", currentPlayer == "Player 1" ? "Player 2" : "Player 1");

                    if (_laneManager.Lanes.All(lane => lane.Count >= 4))
                    {
                        var result = _laneManager.EvaluateGame();
                        Message = result;
                        HttpContext.Session.Clear();
                        return Page();
                    }

                    return RedirectToPage();
                }
                else
                {
                    Message = "No card or lane selected.";
                }
            }

            return Page();
        }

        private void AddRandomCardIfNecessary(string currentPlayer)
        {
            if (currentPlayer == "Player 1")
            {
                if (Player1Cards.Count < 5)
                {
                    var newCard = _cardManager.GetRandomCard();
                    Player1Cards.Add(newCard);
                }
            }
            else if (currentPlayer == "Player 2")
            {
                if (Player2Cards.Count < 5)
                {
                    var newCard = _cardManager.GetRandomCard();
                    Player2Cards.Add(newCard);
                }
            }
        }

        public int GetLaneScore(int lane)
        {
            return _laneManager.CalculateLaneScore(lane);
        }
    }
}
