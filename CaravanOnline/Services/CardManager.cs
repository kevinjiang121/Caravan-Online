using System;
using System.Collections.Generic;
using System.Linq;
using CaravanOnline.Models;

namespace CaravanOnline.Services
{
    public class CardManager 
    {
        private static readonly List<string> Faces = new List<string> { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
        private static readonly List<string> Suits = new List<string> { "Spades", "Diamonds", "Hearts", "Clubs" };

        public List<Card> GetRandomCards()
        {
            return GetRandomCards(8);
        }

        public List<Card> GetRandomCards(int count)
        {
            var random = new Random();
            var deck = Faces.SelectMany(face => Suits, (face, suit) => new Card(face, suit)).ToList();
            return deck.OrderBy(x => random.Next()).Take(count).ToList();
        }

        public static string CompareCards(Card card1, Card card2)
        {
            if (card1 == null || card2 == null)
            {
                return "Error: Invalid card(s) provided.";
            }

            return card1.Number > card2.Number ? "Player 1" : "Player 2";
        }
    }
}
