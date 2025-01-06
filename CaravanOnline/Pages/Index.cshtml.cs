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
                    var parts = selectedCard.Split(' ');
                    if (parts.Length < 2)
                    {
                        Message = "Invalid card selected.";
                        return Page();
                    }

                    var face = parts[0];
                    var suit = parts[1];

                    Card? cardToPlay = null;
                    if (currentPlayer == "Player 1")
                    {
                        cardToPlay = Player1Cards.FirstOrDefault(c => c.Face == face && c.Suit == suit);
                        if (cardToPlay == null)
                        {
                            Message = "Card not found in Player 1's hand.";
                            return Page();
                        }
                    }
                    else
                    {
                        cardToPlay = Player2Cards.FirstOrDefault(c => c.Face == face && c.Suit == suit);
                        if (cardToPlay == null)
                        {
                            Message = "Card not found in Player 2's hand.";
                            return Page();
                        }
                    }

                    // Attempt to place the card
                    bool success = _laneManager.AddCardToLane(CurrentLane, cardToPlay);
                    if (!success)
                    {
                        Message = _laneManager.LastFailReason;
                        return Page();
                    }

                    // If successful, remove card from hand and draw new
                    if (currentPlayer == "Player 1")
                    {
                        Player1Cards.Remove(cardToPlay);
                        AddRandomCardIfNecessary("Player 1", Player1Cards);
                    }
                    else
                    {
                        Player2Cards.Remove(cardToPlay);
                        AddRandomCardIfNecessary("Player 2", Player2Cards);
                    }

                    if (_laneManager.Lanes.All(l => l.Count >= 1))
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
                    var parts = selectedCard.Split(' ');
                    if (parts.Length < 2)
                    {
                        Message = "Invalid card selected.";
                        return Page();
                    }
                    var face = parts[0];
                    var suit = parts[1];

                    if (currentPlayer == "Player 1")
                    {
                        SelectedCardPhase2 = Player1Cards.FirstOrDefault(c => c.Face == face && c.Suit == suit);
                    }
                    else
                    {
                        SelectedCardPhase2 = Player2Cards.FirstOrDefault(c => c.Face == face && c.Suit == suit);
                    }

                    if (SelectedCardPhase2 == null)
                    {
                        Message = "Card not found in hand.";
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

                    bool success = _laneManager.AddCardToLane(laneNumber, SelectedCardPhase2);
                    if (!success)
                    {
                        Message = _laneManager.LastFailReason;
                        return Page();
                    }

                    // If it was valid, remove from the player's hand
                    if (currentPlayer == "Player 1")
                    {
                        var cardInHand = Player1Cards.FirstOrDefault(c => c.Face == SelectedCardPhase2.Face && c.Suit == SelectedCardPhase2.Suit);
                        if (cardInHand != null)
                        {
                            Player1Cards.Remove(cardInHand);
                            AddRandomCardIfNecessary(currentPlayer, Player1Cards);
                        }
                    }
                    else
                    {
                        var cardInHand = Player2Cards.FirstOrDefault(c => c.Face == SelectedCardPhase2.Face && c.Suit == SelectedCardPhase2.Suit);
                        if (cardInHand != null)
                        {
                            Player2Cards.Remove(cardInHand);
                            AddRandomCardIfNecessary(currentPlayer, Player2Cards);
                        }
                    }

                    HttpContext.Session.Remove("SelectedCardPhase2");
                    HttpContext.Session.SetString("Player1Cards", SerializationHelper.SerializePlayerCards(Player1Cards));
                    HttpContext.Session.SetString("Player2Cards", SerializationHelper.SerializePlayerCards(Player2Cards));
                    HttpContext.Session.SetString("Lanes", SerializationHelper.SerializeLanes(_laneManager.Lanes));
                    HttpContext.Session.SetString("Message", Message);
                    SwitchPlayer();
                    var gameResult = _laneManager.EvaluateGame();
                    if (gameResult != "The game is still ongoing.")
                    {
                        Message = gameResult;
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

        public int CalculateLaneScore(int lane)
        {
            return _laneManager.CalculateLaneScore(lane);
        }

        public IActionResult OnPostPlaceCardNextTo([FromBody] CardPlacementData data)
        {
            var serializedLanes = HttpContext.Session.GetString("Lanes") ?? string.Empty;
            if (string.IsNullOrEmpty(serializedLanes))
            {
                return new JsonResult(new { success = false, message = "Lanes data not found." });
            }
            _laneManager.Lanes = SerializationHelper.DeserializeLanes(serializedLanes);

            var cardParts = data.Card.Split(' ');
            if (cardParts.Length < 2)
            {
                return new JsonResult(new { success = false, message = "Invalid card format." });
            }
            var cardFace = cardParts[0];
            var cardLaneNumberStr = cardParts[1];

            var attachedCardParts = data.AttachedCard.Split(' ');
            if (attachedCardParts.Length < 2)
            {
                return new JsonResult(new { success = false, message = "Invalid attached card format." });
            }
            var attachedCardFace = attachedCardParts[0];
            var attachedCardSuit = attachedCardParts[1];

            if (!int.TryParse(cardLaneNumberStr, out int laneIndex) ||
                laneIndex < 1 || laneIndex > _laneManager.Lanes.Count)
            {
                return new JsonResult(new { success = false, message = "Invalid lane number." });
            }

            var lane = _laneManager.Lanes[laneIndex - 1];
            if (data.CardIndex < 0 || data.CardIndex >= lane.Count)
            {
                return new JsonResult(new { success = false, message = "Invalid card index." });
            }

            var cardOnLane = lane[data.CardIndex];
            if (cardOnLane.Face != cardFace)
            {
                return new JsonResult(new { success = false, message = "Card not found in specified lane and index." });
            }

            var attachedCard = new Card(attachedCardFace, attachedCardSuit);
            if (attachedCardFace == "J")
            {
                lane.Remove(cardOnLane);
            }
            else
            {
                cardOnLane.AttachedCards.Add(attachedCard);
            }

            string currentPlayer = HttpContext.Session.GetString("CurrentPlayer") ?? "Player 1";
            var p1Serialized = HttpContext.Session.GetString("Player1Cards") ?? string.Empty;
            var p2Serialized = HttpContext.Session.GetString("Player2Cards") ?? string.Empty;
            var player1Hand = SerializationHelper.DeserializePlayerCards(p1Serialized);
            var player2Hand = SerializationHelper.DeserializePlayerCards(p2Serialized);

            if (currentPlayer == "Player 1")
            {
                var foundCard = player1Hand.FirstOrDefault(c => c.Face == attachedCardFace && c.Suit == attachedCardSuit);
                if (foundCard != null)
                {
                    player1Hand.Remove(foundCard);
                    AddRandomCardIfNecessary("Player 1", player1Hand);
                }
            }
            else
            {
                var foundCard = player2Hand.FirstOrDefault(c => c.Face == attachedCardFace && c.Suit == attachedCardSuit);
                if (foundCard != null)
                {
                    player2Hand.Remove(foundCard);
                    AddRandomCardIfNecessary("Player 2", player2Hand);
                }
            }

            HttpContext.Session.SetString("Player1Cards", SerializationHelper.SerializePlayerCards(player1Hand));
            HttpContext.Session.SetString("Player2Cards", SerializationHelper.SerializePlayerCards(player2Hand));
            HttpContext.Session.SetString("Lanes", SerializationHelper.SerializeLanes(_laneManager.Lanes));
            SwitchPlayer();
            Phase = 2;
            HttpContext.Session.SetInt32("Phase", Phase);

            return new JsonResult(new { success = true });
        }

        private void SwitchPlayer()
        {
            string currentPlayer = HttpContext.Session.GetString("CurrentPlayer") ?? "Player 1";
            string nextPlayer = currentPlayer == "Player 1" ? "Player 2" : "Player 1";
            HttpContext.Session.SetString("CurrentPlayer", nextPlayer);
        }

        private void AddRandomCardIfNecessary(string currentPlayer, List<Card> hand)
        {
            if (hand.Count < 5)
            {
                var newCard = _cardManager.GetRandomCard();
                hand.Add(newCard);
            }
        }
    }

    public class CardPlacementData
    {
        public string Card { get; set; }
        public string AttachedCard { get; set; }
        public int CardIndex { get; set; }
        public int Lane { get; set; }
    }
}
