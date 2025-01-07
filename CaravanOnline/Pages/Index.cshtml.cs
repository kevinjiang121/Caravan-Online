﻿// Pages/Index.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CaravanOnline.Services;
using CaravanOnline.Models;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CaravanOnline.Pages
{
    public class IndexModel : PageModel
    {
        private readonly LaneManager _laneManager;
        private readonly CardManager _cardManager;

        public List<Card> Player1Cards { get; set; } = new();
        public List<Card> Player2Cards { get; set; } = new();
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
            Console.WriteLine("OnGet called.");
            if (HttpContext.Session.GetString("Initialized") != "true")
            {
                Console.WriteLine("Initializing new game state...");
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
                Console.WriteLine("Loading existing game state from session...");
                Player1Cards = SerializationHelper.DeserializePlayerCards(HttpContext.Session.GetString("Player1Cards") ?? "");
                Player2Cards = SerializationHelper.DeserializePlayerCards(HttpContext.Session.GetString("Player2Cards") ?? "");
                CurrentLane = HttpContext.Session.GetInt32("CurrentLane") ?? 1;
                Phase = HttpContext.Session.GetInt32("Phase") ?? 1;
                Message = HttpContext.Session.GetString("Message") ?? "Welcome to the game!";

                var serializedLanes = HttpContext.Session.GetString("Lanes") ?? "";
                if (!string.IsNullOrEmpty(serializedLanes))
                {
                    _laneManager.Lanes = SerializationHelper.DeserializeLanes(serializedLanes);
                }
            }

            Console.WriteLine($"Phase={Phase}, CurrentLane={CurrentLane}, CurrentPlayer={HttpContext.Session.GetString("CurrentPlayer")}");
            Console.WriteLine($"Player1Cards={Player1Cards.Count}, Player2Cards={Player2Cards.Count}");
        }

        public IActionResult OnPost(string? selectedCard = null, string? selectedLane = null)
        {
            CurrentLane = HttpContext.Session.GetInt32("CurrentLane") ?? 1;
            Phase = HttpContext.Session.GetInt32("Phase") ?? 1;
            string currentPlayer = HttpContext.Session.GetString("CurrentPlayer") ?? "Player 1";

            Player1Cards = SerializationHelper.DeserializePlayerCards(HttpContext.Session.GetString("Player1Cards") ?? "");
            Player2Cards = SerializationHelper.DeserializePlayerCards(HttpContext.Session.GetString("Player2Cards") ?? "");

            var lanesSerialized = HttpContext.Session.GetString("Lanes") ?? "";
            if (!string.IsNullOrEmpty(lanesSerialized))
            {
                _laneManager.Lanes = SerializationHelper.DeserializeLanes(lanesSerialized);
            }

            Console.WriteLine("OnPost called.");
            Console.WriteLine($"SelectedCard={selectedCard}, SelectedLane={selectedLane}, Phase={Phase}, CurrentPlayer={currentPlayer}");
            Console.WriteLine($"P1 hand count before action={Player1Cards.Count}, P2 hand count before action={Player2Cards.Count}");

            if (Phase == 1)
            {
                if (!string.IsNullOrEmpty(selectedCard))
                {
                    var parts = selectedCard.Split(' ');
                    if (parts.Length < 2)
                    {
                        Message = "Invalid card selected.";
                        SaveState();
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
                            SaveState();
                            return Page();
                        }

                        bool success = _laneManager.AddCardToLane(CurrentLane, cardToPlay);
                        if (!success)
                        {
                            Message = _laneManager.LastFailReason;
                            SaveState();
                            return Page();
                        }

                        bool removed = Player1Cards.Remove(cardToPlay);
                        Console.WriteLine($"Removed from P1? {removed}. P1 now has {Player1Cards.Count} cards.");
                        AddRandomCardIfNecessary("Player 1", Player1Cards);
                    }
                    else
                    {
                        cardToPlay = Player2Cards.FirstOrDefault(c => c.Face == face && c.Suit == suit);
                        if (cardToPlay == null)
                        {
                            Message = "Card not found in Player 2's hand.";
                            SaveState();
                            return Page();
                        }

                        bool success = _laneManager.AddCardToLane(CurrentLane, cardToPlay);
                        if (!success)
                        {
                            Message = _laneManager.LastFailReason;
                            SaveState();
                            return Page();
                        }

                        bool removed = Player2Cards.Remove(cardToPlay);
                        Console.WriteLine($"Removed from P2? {removed}. P2 now has {Player2Cards.Count} cards.");
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
                        // Switch lane and player
                        CurrentLane = SwitchLane(currentPlayer, CurrentLane);
                        HttpContext.Session.SetInt32("CurrentLane", CurrentLane);
                        HttpContext.Session.SetString("CurrentPlayer", currentPlayer == "Player 1" ? "Player 2" : "Player 1");
                    }

                    SaveState();
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
                        SaveState();
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
                        Message = "Card not found.";
                        SaveState();
                        return Page();
                    }

                    HttpContext.Session.SetString("SelectedCardPhase2", SerializationHelper.SerializePlayerCards(new List<Card> { SelectedCardPhase2 }));
                    Message = "Please select a lane";
                    SaveState();
                }
                else if (!string.IsNullOrEmpty(selectedLane))
                {
                    var serializedSelected = HttpContext.Session.GetString("SelectedCardPhase2") ?? "";
                    if (string.IsNullOrEmpty(serializedSelected))
                    {
                        Message = "Please select a card first.";
                        SaveState();
                        return Page();
                    }
                    SelectedCardPhase2 = SerializationHelper.DeserializePlayerCards(serializedSelected).FirstOrDefault();
                    if (SelectedCardPhase2 == null)
                    {
                        Message = "Invalid card.";
                        SaveState();
                        return Page();
                    }

                    if (!int.TryParse(selectedLane, out int laneNumber) || laneNumber < 1 || laneNumber > 6)
                    {
                        Message = "Invalid lane selected.";
                        SaveState();
                        return Page();
                    }

                    bool success = _laneManager.AddCardToLane(laneNumber, SelectedCardPhase2);
                    if (!success)
                    {
                        Message = _laneManager.LastFailReason;
                        SaveState();
                        return Page();
                    }

                    if (currentPlayer == "Player 1")
                    {
                        bool removed = Player1Cards.Remove(SelectedCardPhase2);
                        Console.WriteLine($"Removed from P1 (Phase2)? {removed}. P1 now has {Player1Cards.Count} cards.");
                        AddRandomCardIfNecessary("Player 1", Player1Cards);
                    }
                    else
                    {
                        bool removed = Player2Cards.Remove(SelectedCardPhase2);
                        Console.WriteLine($"Removed from P2 (Phase2)? {removed}. P2 now has {Player2Cards.Count} cards.");
                        AddRandomCardIfNecessary("Player 2", Player2Cards);
                    }

                    HttpContext.Session.Remove("SelectedCardPhase2");
                    SaveState();

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

            SaveState();
            return Page();
        }

        [HttpPost]
        public IActionResult OnPostDiscardLaneClick([FromBody] LaneDiscardData data)
        {
            var serializedLanes = HttpContext.Session.GetString("Lanes") ?? "";
            if (string.IsNullOrEmpty(serializedLanes))
            {
                return new JsonResult(new { success = false, message = "Lanes data not found." });
            }
            _laneManager.Lanes = SerializationHelper.DeserializeLanes(serializedLanes);

            var p1Serialized = HttpContext.Session.GetString("Player1Cards") ?? "";
            var p2Serialized = HttpContext.Session.GetString("Player2Cards") ?? "";
            var player1Hand = SerializationHelper.DeserializePlayerCards(p1Serialized);
            var player2Hand = SerializationHelper.DeserializePlayerCards(p2Serialized);

            if (!int.TryParse(data.Lane, out int laneNum) || laneNum < 1 || laneNum > 6)
            {
                return new JsonResult(new { success = false, message = "Invalid lane number." });
            }

            _laneManager.DiscardLane(laneNum);
            SwitchPlayer();

            HttpContext.Session.SetString("Player1Cards", SerializationHelper.SerializePlayerCards(player1Hand));
            HttpContext.Session.SetString("Player2Cards", SerializationHelper.SerializePlayerCards(player2Hand));
            HttpContext.Session.SetString("Lanes", SerializationHelper.SerializeLanes(_laneManager.Lanes));
            return new JsonResult(new { success = true, message = $"Discarded lane {laneNum}." });
        }

        [HttpPost]
        public IActionResult OnPostDiscardCardClick([FromBody] CardDiscardData data)
        {
            var p1Serialized = HttpContext.Session.GetString("Player1Cards") ?? "";
            var p2Serialized = HttpContext.Session.GetString("Player2Cards") ?? "";
            var player1Hand = SerializationHelper.DeserializePlayerCards(p1Serialized);
            var player2Hand = SerializationHelper.DeserializePlayerCards(p2Serialized);

            var lanesSerialized = HttpContext.Session.GetString("Lanes") ?? "";
            if (!string.IsNullOrEmpty(lanesSerialized))
            {
                _laneManager.Lanes = SerializationHelper.DeserializeLanes(lanesSerialized);
            }

            string currentPlayer = HttpContext.Session.GetString("CurrentPlayer") ?? "Player 1";
            if (string.IsNullOrEmpty(data.Face) || string.IsNullOrEmpty(data.Suit))
            {
                return new JsonResult(new { success = false, message = "Invalid card data." });
            }

            if (currentPlayer == "Player 1")
            {
                var found = player1Hand.FirstOrDefault(c => c.Face == data.Face && c.Suit == data.Suit);
                if (found == null)
                {
                    return new JsonResult(new { success = false, message = "Card not found in Player 1's hand." });
                }
                bool removed = player1Hand.Remove(found);
                Console.WriteLine($"Discarding from P1? {removed}. P1 now has {player1Hand.Count} cards.");
                AddRandomCardIfNecessary("Player 1", player1Hand);
            }
            else
            {
                var found = player2Hand.FirstOrDefault(c => c.Face == data.Face && c.Suit == data.Suit);
                if (found == null)
                {
                    return new JsonResult(new { success = false, message = "Card not found in Player 2's hand." });
                }
                bool removed = player2Hand.Remove(found);
                Console.WriteLine($"Discarding from P2? {removed}. P2 now has {player2Hand.Count} cards.");
                AddRandomCardIfNecessary("Player 2", player2Hand);
            }

            SwitchPlayer();

            HttpContext.Session.SetString("Player1Cards", SerializationHelper.SerializePlayerCards(player1Hand));
            HttpContext.Session.SetString("Player2Cards", SerializationHelper.SerializePlayerCards(player2Hand));
            HttpContext.Session.SetString("Lanes", SerializationHelper.SerializeLanes(_laneManager.Lanes));
            return new JsonResult(new { success = true, message = $"Discarded card {data.Face} {data.Suit}." });
        }

        [HttpPost]
        public IActionResult OnPostPlaceCardNextTo([FromBody] CardPlacementData data)
        {
            var lanesSerialized = HttpContext.Session.GetString("Lanes") ?? "";
            if (string.IsNullOrEmpty(lanesSerialized))
            {
                return new JsonResult(new { success = false, message = "Lanes data not found." });
            }
            _laneManager.Lanes = SerializationHelper.DeserializeLanes(lanesSerialized);

            var p1Serialized = HttpContext.Session.GetString("Player1Cards") ?? "";
            var p2Serialized = HttpContext.Session.GetString("Player2Cards") ?? "";
            var player1Hand = SerializationHelper.DeserializePlayerCards(p1Serialized);
            var player2Hand = SerializationHelper.DeserializePlayerCards(p2Serialized);

            string currentPlayer = HttpContext.Session.GetString("CurrentPlayer") ?? "Player 1";

            var cardParts = data.Card.Split(' ');
            if (cardParts.Length < 2)
            {
                return new JsonResult(new { success = false, message = "Invalid card format." });
            }
            var baseCardFace = cardParts[0];
            var baseCardSuit = cardParts[1];
            if (!int.TryParse(data.Lane.ToString(), out int laneIndex) ||
                laneIndex < 1 || laneIndex > _laneManager.Lanes.Count)
            {
                return new JsonResult(new { success = false, message = "Invalid lane number." });
            }

            var lane = _laneManager.Lanes[laneIndex - 1];
            if (data.CardIndex < 0 || data.CardIndex >= lane.Count)
            {
                return new JsonResult(new { success = false, message = "Invalid card index." });
            }

            var baseCard = lane[data.CardIndex];
            if (baseCard.Face != baseCardFace || baseCard.Suit != baseCardSuit)
            {
                return new JsonResult(new { success = false, message = "Card not found in specified lane and index." });
            }

            var attachedParts = data.AttachedCard.Split(' ');
            if (attachedParts.Length < 2)
            {
                return new JsonResult(new { success = false, message = "Invalid attached card format." });
            }
            var attachedFace = attachedParts[0];
            var attachedSuit = attachedParts[1];

            Card? cardToAttach = null;
            if (currentPlayer == "Player 1")
            {
                cardToAttach = player1Hand.FirstOrDefault(c => c.Face == attachedFace && c.Suit == attachedSuit);
                if (cardToAttach == null)
                {
                    return new JsonResult(new { success = false, message = "Attached card not found in Player 1's hand." });
                }
                bool removed = player1Hand.Remove(cardToAttach);
                Console.WriteLine($"Attaching from P1? {removed}. P1 now has {player1Hand.Count} cards.");
                AddRandomCardIfNecessary("Player 1", player1Hand);
            }
            else
            {
                cardToAttach = player2Hand.FirstOrDefault(c => c.Face == attachedFace && c.Suit == attachedSuit);
                if (cardToAttach == null)
                {
                    return new JsonResult(new { success = false, message = "Attached card not found in Player 2's hand." });
                }
                bool removed = player2Hand.Remove(cardToAttach);
                Console.WriteLine($"Attaching from P2? {removed}. P2 now has {player2Hand.Count} cards.");
                AddRandomCardIfNecessary("Player 2", player2Hand);
            }

            if (attachedFace == "J")
            {
                // Jack removes the base card from lane
                lane.Remove(baseCard);
                Console.WriteLine($"Jack attached. Removed base card {baseCard.Face} {baseCard.Suit} from lane {laneIndex}.");
            }
            else
            {
                // King or Queen or other face card attaches
                baseCard.AttachedCards.Add(cardToAttach);
                Console.WriteLine($"Attached {cardToAttach.Face} {cardToAttach.Suit} to base card {baseCard.Face} {baseCard.Suit} in lane {laneIndex}.");
            }

            SwitchPlayer();

            // Update the model's hand lists to reflect changes
            Player1Cards = player1Hand;
            Player2Cards = player2Hand;

            // Save updated hands and lanes back to session
            HttpContext.Session.SetString("Player1Cards", SerializationHelper.SerializePlayerCards(player1Hand));
            HttpContext.Session.SetString("Player2Cards", SerializationHelper.SerializePlayerCards(player2Hand));
            HttpContext.Session.SetString("Lanes", SerializationHelper.SerializeLanes(_laneManager.Lanes));

            return new JsonResult(new { success = true });
        }

        private void SwitchPlayer()
        {
            string current = HttpContext.Session.GetString("CurrentPlayer") ?? "Player 1";
            string next = (current == "Player 1") ? "Player 2" : "Player 1";
            HttpContext.Session.SetString("CurrentPlayer", next);
            Console.WriteLine($"Switched from {current} to {next}.");
        }

        private void AddRandomCardIfNecessary(string currentPlayer, List<Card> hand)
        {
            // If the player's hand is below 5, draw a new card
            if (hand.Count < 5)
            {
                try
                {
                    var newCard = _cardManager.GetRandomCard();
                    hand.Add(newCard);
                    Console.WriteLine($"Drew new card for {currentPlayer}: {newCard.Face} {newCard.Suit}. Hand size now {hand.Count}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error drawing new card for {currentPlayer}: {ex.Message}");
                    Message = $"Error drawing new card for {currentPlayer}.";
                }
            }
        }

        private int SwitchLane(string currentPlayer, int currentLane)
        {
            // Logic to switch to the corresponding lane
            if (currentPlayer == "Player 1")
            {
                return currentLane switch
                {
                    1 => 4,
                    2 => 5,
                    3 => 6,
                    _ => 1
                };
            }
            else
            {
                return currentLane switch
                {
                    4 => 2,
                    5 => 3,
                    6 => 1,
                    _ => 4
                };
            }
        }

        private void SaveState()
        {
            HttpContext.Session.SetString("Player1Cards", SerializationHelper.SerializePlayerCards(Player1Cards));
            HttpContext.Session.SetString("Player2Cards", SerializationHelper.SerializePlayerCards(Player2Cards));
            HttpContext.Session.SetString("Lanes", SerializationHelper.SerializeLanes(_laneManager.Lanes));
            HttpContext.Session.SetString("Message", Message);

            Console.WriteLine($"SaveState called. P1={Player1Cards.Count} cards, P2={Player2Cards.Count} cards, Lane1Count={_laneManager.Lanes[0].Count}");
        }

        public int CalculateLaneScore(int lane)
        {
            return _laneManager.CalculateLaneScore(lane);
        }
    }

    // Supporting Data Classes
    public class CardPlacementData
    {
        public string Card { get; set; } // "Face Suit"
        public string AttachedCard { get; set; } // "Face Suit"
        public int CardIndex { get; set; }
        public int Lane { get; set; }
    }

    public class LaneDiscardData
    {
        public string Lane { get; set; }
    }

    public class CardDiscardData
    {
        public string Face { get; set; }
        public string Suit { get; set; }
    }
}
