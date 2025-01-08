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
        private readonly GameStateHelper _gameStateHelper;
        private readonly PlayerManager _playerManager;

        public List<Card> Player1Cards { get; set; } = new();
        public List<Card> Player2Cards { get; set; } = new();
        public string Message { get; set; } = "Welcome to the game!";
        public int CurrentLane { get; set; } = 1;
        public int Phase { get; set; } = 1;

        public List<List<Card>> Lanes => _laneManager.Lanes;
        public Card? SelectedCardPhase2 { get; set; }

        public IndexModel(LaneManager laneManager, CardManager cardManager, GameStateHelper gameStateHelper, PlayerManager playerManager)
        {
            _laneManager = laneManager;
            _cardManager = cardManager;
            _gameStateHelper = gameStateHelper;
            _playerManager = playerManager;
        }

        public void OnGet()
        {
            Console.WriteLine("OnGet called.");
            if (!_gameStateHelper.IsInitialized())
            {
                Console.WriteLine("Initializing new game state...");
                Player1Cards = _cardManager.GetRandomCards(8);
                Player2Cards = _cardManager.GetRandomCards(8);

                _gameStateHelper.InitializeGameState(Player1Cards, Player2Cards, _laneManager.Lanes);
            }
            else
            {
                Console.WriteLine("Loading existing game state from session...");

                List<Card> tempPlayer1Cards;
                List<Card> tempPlayer2Cards;
                int tempCurrentLane;
                int tempPhase;
                string tempMessage;
                List<List<Card>> tempLanes;

                _gameStateHelper.LoadGameState(out tempPlayer1Cards, out tempPlayer2Cards, out tempCurrentLane, out tempPhase, out tempMessage, out tempLanes);

                Player1Cards = tempPlayer1Cards;
                Player2Cards = tempPlayer2Cards;
                CurrentLane = tempCurrentLane;
                Phase = tempPhase;
                Message = tempMessage;

                if (tempLanes.Any())
                {
                    _laneManager.Lanes = tempLanes;
                }
            }

            string currentPlayer = _playerManager.GetCurrentPlayer(HttpContext.Session);
            Console.WriteLine($"Phase={Phase}, CurrentLane={CurrentLane}, CurrentPlayer={currentPlayer}");
            Console.WriteLine($"Player1Cards={Player1Cards.Count}, Player2Cards={Player2Cards.Count}");
        }

        public IActionResult OnPost(string? selectedCard = null, string? selectedLane = null)
        {
            CurrentLane = HttpContext.Session.GetInt32("CurrentLane") ?? 1;
            Phase = HttpContext.Session.GetInt32("Phase") ?? 1;
            string currentPlayer = _playerManager.GetCurrentPlayer(HttpContext.Session);

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
                        _playerManager.AddRandomCardIfNecessary("Player 1", Player1Cards);
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
                        _playerManager.AddRandomCardIfNecessary("Player 2", Player2Cards);
                    }

                    if (_laneManager.Lanes.All(l => l.Count >= 1))
                    {
                        Phase = 2;
                        HttpContext.Session.SetInt32("Phase", Phase);
                        HttpContext.Session.SetString("CurrentPlayer", "Player 1");
                    }
                    else
                    {
                        CurrentLane = SwitchLane(currentPlayer, CurrentLane);
                        HttpContext.Session.SetInt32("CurrentLane", CurrentLane);
                        _playerManager.SwitchPlayer(HttpContext.Session);
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
                        var cardToRemove = Player1Cards.FirstOrDefault(c => c.Face == SelectedCardPhase2.Face && c.Suit == SelectedCardPhase2.Suit);
                        if (cardToRemove != null)
                        {
                            bool removed = Player1Cards.Remove(cardToRemove);
                            Console.WriteLine($"Removed from P1 (Phase2)? {removed}. P1 now has {Player1Cards.Count} cards.");
                            _playerManager.AddRandomCardIfNecessary("Player 1", Player1Cards);
                        }
                        else
                        {
                            Message = "Card not found in Player 1's hand.";
                            SaveState();
                            return Page();
                        }
                    }
                    else
                    {
                        var cardToRemove = Player2Cards.FirstOrDefault(c => c.Face == SelectedCardPhase2.Face && c.Suit == SelectedCardPhase2.Suit);
                        if (cardToRemove != null)
                        {
                            bool removed = Player2Cards.Remove(cardToRemove);
                            Console.WriteLine($"Removed from P2 (Phase2)? {removed}. P2 now has {Player2Cards.Count} cards.");
                            _playerManager.AddRandomCardIfNecessary("Player 2", Player2Cards);
                        }
                        else
                        {
                            Message = "Card not found in Player 2's hand.";
                            SaveState();
                            return Page();
                        }
                    }

                    HttpContext.Session.Remove("SelectedCardPhase2");
                    SaveState();

                    _playerManager.SwitchPlayer(HttpContext.Session);
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

        // Removed [HttpPost] attribute
        public IActionResult OnPostDiscardLaneClick([FromBody] LaneDiscardData data)
        {
            // Null check for data.Lane
            if (string.IsNullOrEmpty(data.Lane))
            {
                return new JsonResult(new { success = false, message = "Lane data is missing." });
            }

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
            _playerManager.SwitchPlayer(HttpContext.Session);

            _gameStateHelper.SaveGameState(player1Hand, player2Hand, CurrentLane, Phase, Message, _laneManager.Lanes);
            return new JsonResult(new { success = true, message = $"Discarded lane {laneNum}." });
        }

        // Removed [HttpPost] attribute
        public IActionResult OnPostDiscardCardClick([FromBody] CardDiscardData data)
        {
            // Null checks for data.Face and data.Suit
            if (string.IsNullOrEmpty(data.Face) || string.IsNullOrEmpty(data.Suit))
            {
                return new JsonResult(new { success = false, message = "Invalid card data." });
            }

            var p1Serialized = HttpContext.Session.GetString("Player1Cards") ?? "";
            var p2Serialized = HttpContext.Session.GetString("Player2Cards") ?? "";
            var player1Hand = SerializationHelper.DeserializePlayerCards(p1Serialized);
            var player2Hand = SerializationHelper.DeserializePlayerCards(p2Serialized);

            var lanesSerialized = HttpContext.Session.GetString("Lanes") ?? "";
            if (!string.IsNullOrEmpty(lanesSerialized))
            {
                _laneManager.Lanes = SerializationHelper.DeserializeLanes(lanesSerialized);
            }

            string currentPlayer = _playerManager.GetCurrentPlayer(HttpContext.Session);

            if (currentPlayer == "Player 1")
            {
                var found = player1Hand.FirstOrDefault(c => c.Face == data.Face && c.Suit == data.Suit);
                if (found == null)
                {
                    return new JsonResult(new { success = false, message = "Card not found in Player 1's hand." });
                }
                bool removed = player1Hand.Remove(found);
                Console.WriteLine($"Discarding from P1? {removed}. P1 now has {player1Hand.Count} cards.");
                _playerManager.AddRandomCardIfNecessary("Player 1", player1Hand);
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
                _playerManager.AddRandomCardIfNecessary("Player 2", player2Hand);
            }

            _playerManager.SwitchPlayer(HttpContext.Session);

            _gameStateHelper.SaveGameState(player1Hand, player2Hand, CurrentLane, Phase, Message, _laneManager.Lanes);
            return new JsonResult(new { success = true, message = $"Discarded card {data.Face} {data.Suit}." });
        }

        // Removed [HttpPost] attribute
        public IActionResult OnPostPlaceCardNextTo([FromBody] CardPlacementData data)
        {
            // Null checks for data.Card and data.AttachedCard
            if (string.IsNullOrEmpty(data.Card) || string.IsNullOrEmpty(data.AttachedCard))
            {
                return new JsonResult(new { success = false, message = "Card or AttachedCard data is missing." });
            }

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

            string currentPlayer = _playerManager.GetCurrentPlayer(HttpContext.Session);

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
                _playerManager.AddRandomCardIfNecessary("Player 1", player1Hand);
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
                _playerManager.AddRandomCardIfNecessary("Player 2", player2Hand);
            }

            if (attachedFace == "J")
            {
                lane.Remove(baseCard);
                Console.WriteLine($"Jack attached. Removed base card {baseCard.Face} {baseCard.Suit} from lane {laneIndex}.");
            }
            else
            {
                baseCard.AttachedCards.Add(cardToAttach);
                Console.WriteLine($"Attached {cardToAttach.Face} {cardToAttach.Suit} to base card {baseCard.Face} {baseCard.Suit} in lane {laneIndex}.");
            }

            _playerManager.SwitchPlayer(HttpContext.Session);

            Player1Cards = player1Hand;
            Player2Cards = player2Hand;

            _gameStateHelper.SaveGameState(Player1Cards, Player2Cards, CurrentLane, Phase, Message, _laneManager.Lanes);

            return new JsonResult(new { success = true });
        }

        private int SwitchLane(string currentPlayer, int currentLane)
        {
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
            _gameStateHelper.SaveGameState(Player1Cards, Player2Cards, CurrentLane, Phase, Message, _laneManager.Lanes);
            Console.WriteLine($"SaveState called. P1={Player1Cards.Count} cards, P2={Player2Cards.Count} cards, Lane1Count={_laneManager.Lanes[0].Count}");
        }

        public int CalculateLaneScore(int lane)
        {
            return _laneManager.CalculateLaneScore(lane);
        }
    }

    public class CardPlacementData
    {
        public string? Card { get; set; }
        public string? AttachedCard { get; set; }
        public int CardIndex { get; set; }
        public int Lane { get; set; }
    }

    public class LaneDiscardData
    {
        public string? Lane { get; set; }
    }

    public class CardDiscardData
    {
        public string? Face { get; set; }
        public string? Suit { get; set; }
    }
}
