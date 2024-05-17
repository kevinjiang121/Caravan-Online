using System;
using System.Collections.Generic;
using System.Linq;

namespace CaravanOnline.Services
{
    public class CardManager
    {
        public static List<string> GetRandomCards()
        {
            var cards = new List<string> { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
            var random = new Random();
            return cards.OrderBy(x => random.Next()).Take(5).ToList();
        }

        public static string CompareCards(string card1, string card2)
        {
            var cardValues = new Dictionary<string, int>
            {
                ["A"] = 1, ["K"] = 13, ["Q"] = 12, ["J"] = 11, ["10"] = 10,
                ["9"] = 9, ["8"] = 8, ["7"] = 7, ["6"] = 6, ["5"] = 5, ["4"] = 4, ["3"] = 3, ["2"] = 2
            };
            Console.WriteLine(card1);

            if (string.IsNullOrEmpty(card1) || string.IsNullOrEmpty(card2) ||
                !cardValues.ContainsKey(card1) || !cardValues.ContainsKey(card2))
            {
                return "Error: Invalid card(s) provided.";
            }

            return cardValues[card1] > cardValues[card2] ? "Player 1" : "Player 2";
        }
    }
}
