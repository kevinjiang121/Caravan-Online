using CaravanOnline.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaravanOnline.Services
{
    public static class SerializationHelper
    {
        public static string SerializeCards(List<Card> cards)
        {
            string serializedCards = string.Join(";", cards.Select(c => {
                string attachedCardsSerialized = SerializeAttachedCards(c.AttachedCards);
                Console.WriteLine($"Serializing Card: {c.Face}, {c.Suit} with Attached: {attachedCardsSerialized}");
                return $"{c.Face},{c.Suit},{c.Number},{c.Direction},{c.Effect ?? string.Empty},{attachedCardsSerialized}";
            }));
            return serializedCards;
        }

        public static List<Card> DeserializeCards(string serializedCards)
        {
            if (string.IsNullOrEmpty(serializedCards)) return new List<Card>();

            var deserializedCards = serializedCards.Split(';').Select(c =>
            {
                var parts = c.Split(',');
                if (parts.Length < 5)
                {
                    throw new InvalidOperationException($"Invalid card format: {c}");
                }

                var attachedCards = new List<Card>();
                if (parts.Length > 6)
                {
                    var attachedCardsData = string.Join(",", parts.Skip(5));
                    attachedCards = DeserializeAttachedCards(attachedCardsData);
                }

                return new Card(parts[0], parts[1])
                {
                    Number = int.Parse(parts[2]),
                    Direction = parts[3],
                    Effect = parts.Length > 4 ? parts[4] : null,
                    AttachedCards = attachedCards
                };
            }).ToList();

            return deserializedCards;
        }

        public static string SerializeAttachedCards(List<Card> attachedCards)
        {
            Console.WriteLine($"Serializing Attached Cards: {attachedCards.Count}");
            string serializedAttachedCards = string.Join("|", attachedCards.Select(ac => $"{ac.Face}^{ac.Suit}"));
            return serializedAttachedCards;
        }

        public static List<Card> DeserializeAttachedCards(string serializedAttachedCards)
        {
            if (string.IsNullOrEmpty(serializedAttachedCards)) return new List<Card>();

            var deserializedAttachedCards = serializedAttachedCards.Split('|').Select(ac =>
            {
                var acParts = ac.Split('^');
                if (acParts.Length < 2)
                {
                    throw new InvalidOperationException($"Invalid attached card format: {ac}");
                }
                return new Card(acParts[0], acParts[1]);
            }).ToList();

            return deserializedAttachedCards;
        }

        public static string SerializeLanes(List<List<Card>> lanes)
        {
            string serializedLanes = string.Join(";", lanes.Select(lane =>
                string.Join(",", lane.Select(c =>
                {
                    string attachedCardsSerialized = SerializeAttachedCards(c.AttachedCards);
                    Console.WriteLine($"Serializing Lane Card: {c.Face}, {c.Suit} with Attached: {attachedCardsSerialized}");
                    return $"{c.Face}-{c.Suit}-{c.Number}-{c.Direction}-{c.Effect ?? string.Empty}-{attachedCardsSerialized}";
                }))));
            return serializedLanes;
        }

        public static List<List<Card>> DeserializeLanes(string serializedLanes)
        {
            var lanes = new List<List<Card>>();
            var laneEntries = serializedLanes.Split(';');
            foreach (var laneEntry in laneEntries)
            {
                if (!string.IsNullOrWhiteSpace(laneEntry))
                {
                    lanes.Add(laneEntry.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(c =>
                    {
                        var parts = c.Split('-');
                        if (parts.Length < 5)
                        {
                            throw new InvalidOperationException($"Invalid lane card format: {c}");
                        }

                        var card = new Card(parts[0], parts[1])
                        {
                            Number = int.Parse(parts[2]),
                            Direction = parts[3],
                            Effect = parts.Length > 4 ? parts[4] : null
                        };

                        if (parts.Length > 5)
                        {
                            var attachedCardsData = string.Join("-", parts.Skip(5));
                            card.AttachedCards = DeserializeAttachedCards(attachedCardsData);
                        }

                        return card;
                    }).ToList());
                }
                else
                {
                    lanes.Add(new List<Card>());
                }
            }
            return lanes;
        }

        public static string SerializePlayerCards(List<Card> playerCards)
        {
            string serializedPlayerCards = SerializeCards(playerCards);
            return serializedPlayerCards;
        }

        public static List<Card> DeserializePlayerCards(string serializedPlayerCards)
        {
            var deserializedPlayerCards = DeserializeCards(serializedPlayerCards);
            return deserializedPlayerCards;
        }
    }
}
