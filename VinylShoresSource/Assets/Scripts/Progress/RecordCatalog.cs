using System;
using System.Collections.Generic;

namespace VinylShores.Progress
{
    public enum IslandArea { Beach, Forest, Mountain }

    /// <summary>One collectable record. Ids are stable and never depend on NPC or scene object names.</summary>
    public readonly struct RecordDefinition
    {
        public string Id { get; }
        public IslandArea Area { get; }
        /// <summary>Position inside its area (1-3). Records in an area are found in this order.</summary>
        public int Order { get; }

        public RecordDefinition(string id, IslandArea area, int order)
        {
            Id = id;
            Area = area;
            Order = order;
        }

        public override string ToString() => Id;
    }

    /// <summary>
    /// The nine records of the island: three per area, in sequence.
    /// The final boss is a separate challenge and is intentionally not in this list.
    /// </summary>
    public static class RecordCatalog
    {
        public const int RecordsPerArea = 3;
        public const string FinalBossChallengeId = "mountain_boss";

        private static readonly RecordDefinition[] records =
        {
            new RecordDefinition("beach_1", IslandArea.Beach, 1),
            new RecordDefinition("beach_2", IslandArea.Beach, 2),
            new RecordDefinition("beach_3", IslandArea.Beach, 3),
            new RecordDefinition("forest_1", IslandArea.Forest, 1),
            new RecordDefinition("forest_2", IslandArea.Forest, 2),
            new RecordDefinition("forest_3", IslandArea.Forest, 3),
            new RecordDefinition("mountain_1", IslandArea.Mountain, 1),
            new RecordDefinition("mountain_2", IslandArea.Mountain, 2),
            new RecordDefinition("mountain_3", IslandArea.Mountain, 3),
        };

        public static IReadOnlyList<RecordDefinition> All => records;
        public static int Count => records.Length;

        public static bool TryGet(string recordId, out RecordDefinition record)
        {
            foreach (var candidate in records)
            {
                if (string.Equals(candidate.Id, recordId, StringComparison.Ordinal))
                {
                    record = candidate;
                    return true;
                }
            }
            record = default;
            return false;
        }

        public static bool IsRecord(string recordId) => TryGet(recordId, out _);

        /// <summary>Records of one area, in their collection order.</summary>
        public static IEnumerable<RecordDefinition> InArea(IslandArea area)
        {
            foreach (var record in records)
                if (record.Area == area) yield return record;
        }
    }
}
