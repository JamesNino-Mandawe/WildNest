using System.Collections.Generic;

namespace Project
{
    public sealed class BookingSummary
    {
        public string BookingType { get; set; } = string.Empty;
        public string CabinName { get; set; } = string.Empty;
        public string PrimarySubtitle { get; set; } = string.Empty;
        public decimal PrimaryAmount { get; set; }
        public string PrimaryAmountLabel { get; set; } = string.Empty;
        public List<SummaryLine> Lines { get; set; } = new();
        public List<AddonItem> Addons { get; set; } = new();
        public decimal GrandTotal { get; set; }
        public int CabinPricePerNight { get; set; }
        public int Nights { get; set; }
    }

    public sealed class SummaryLine
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public sealed class AddonItem
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
