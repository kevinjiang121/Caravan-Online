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
            return string.Join(";", cards.Select(c => $"{c.Face},{c.Suit},{c.Number},{c.Direction},{c.Effect ?? string.Empty}"));
        }

        public static List<Card> DeserializeCards(string serializedCards)
        {
            if (string.IsNullOrEmpty(serializedCards)) return new List<Card>();

            return serializedCards.Split(';').Select(c =>
            {
                var parts = c.Split(',');
                return new Card(parts[0], parts[1])
                {
                    Number = int.Parse(parts[2]),
                    Direction = parts[3],
                    Effect = parts.Length > 4 ? parts[4] : null
                };
            }).ToList();
        }

        public static string SerializeLanes(List<List<Card>> lanes)
        {
            return string.Join(";", lanes.Select(lane => string.Join(",", lane.Select(c => $"{c.Face}-{c.Suit}"))));
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
                        return new Card(parts[0], parts[1]);
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
