using System.Collections.Generic;

namespace FlibSystem.Models
{
    public class InfoItem
    {
        public string Label { get; set; }
        public string Value { get; set; }
    }

    public class InfoGroup
    {
        public string Title { get; set; }
        public string Glyph { get; set; }
        public List<InfoItem> Items { get; set; } = new List<InfoItem>();
    }

    public class SystemSummary
    {
        public string Cpu { get; set; }
        public string Ram { get; set; }
        public string Os { get; set; }
    }
}

