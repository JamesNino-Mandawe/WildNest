using System.Drawing;

namespace Project
{
    public class ZoneInfo
    {
        public string Id;
        public string Name;
        public string Icon;
        public Color PinColor;
        public string Status;       // "open" | "limited" | "advisory"
        public string Desc;
        public string[] Animals;
        public bool[] AnimalOk;
        public int SlotCap;
        public int SlotUsed;
        public string ExperienceName;
        public string[] Cabins;
        // Position as % of map width/height (0.0–1.0)
        public float PosX;
        public float PosY;
    }
}