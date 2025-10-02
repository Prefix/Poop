using System;

namespace Prefix.Poop.Models
{
    public class PoopRecord
    {
        public int Id { get; set; }
        public string PooperName { get; set; } = "";
        public string PooperSteamId { get; set; } = "";
        public string VictimName { get; set; } = "";
        public string VictimSteamId { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string MapName { get; set; } = "";
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
    }
}
