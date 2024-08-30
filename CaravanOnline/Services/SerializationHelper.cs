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
            return string.Join(";", cards.Select(c => $"{c.Face},{c.Suit},{c.Number},{c.Direction},{c.Effect ?? string.Empty},{SerializeAttachedCards(c.AttachedCards)}"));
        }

        public static List<Card> DeserializeCards(string serializedCards)
        {
            if (string.IsNullOrEmpty(serializedCards)) return new List<Card>();

            return serializedCards.Split(';').Select(c =>
            {
                var parts = c.Split(',');
                if (parts.Length < 5)
                {
                    throw new InvalidOperationException($"Invalid card format: {c}");
                }

                var attachedCards = new List<Card>();
                if (parts.Length > 6)
                {
                    var attachedParts = string.Join(",", parts.Skip(5)); // Join all attached parts correctly
                    attachedCards = DeserializeAttachedCards(attachedParts); // Use proper deserialization method for attached cards
                }

                return new Card(parts[0], parts[1])
                {
                    Number = int.Parse(parts[2]),
                    Direction = parts[3],
                    Effect = parts.Length > 4 ? parts[4] : null,
                    AttachedCards = attachedCards
                };
            }).ToList();
        }

        public static string SerializeAttachedCards(List<Card> attachedCards)
        {
            // Serialize each attached card using a different delimiter for face and suit, e.g., "Face^Suit"
            return string.Join("|", attachedCards.Select(ac => $"{ac.Face}^{ac.Suit}"));
        }

        public static List<Card> DeserializeAttachedCards(string serializedAttachedCards)
        {
            if (string.IsNullOrEmpty(serializedAttachedCards)) return new List<Card>();

            return serializedAttachedCards.Split('|').Select(ac =>
            {
                var acParts = ac.Split('^'); 
                if (acParts.Length < 2)
                {
                    throw new InvalidOperationException($"Invalid attached card format: {ac}");
                }
                return new Card(acParts[0], acParts[1]);
            }).ToList();
        }

        public static string SerializeLanes(List<List<Card>> lanes)
        {
            return string.Join(";", lanes.Select(lane => string.Join(",", lane.Select(c => $"{c.Face}-{c.Suit}-{c.Number}-{c.Direction}-{c.Effect ?? string.Empty}-{SerializeAttachedCards(c.AttachedCards)}"))));
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
                        return new Card(parts[0], parts[1])
                        {
                            Number = int.Parse(parts[2]),
                            Direction = parts[3],
                            Effect = parts.Length > 4 ? parts[4] : null,
                            AttachedCards = parts.Length > 5 ? DeserializeAttachedCards(parts[5]) : new List<Card>()
                        };
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
            return SerializeCards(playerCards);
        }

        public static List<Card> DeserializePlayerCards(string serializedPlayerCards)
        {
            return DeserializeCards(serializedPlayerCards);
        }
    }
}
