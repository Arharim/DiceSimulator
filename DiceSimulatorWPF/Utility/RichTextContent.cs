using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace DiceSimulatorWPF.Utility
{
    public class RichTextContent
    {
        public List<RichTextSection> Sections { get; } = new List<RichTextSection>();
    }

    public class RichTextSection
    {
        public string Text { get; set; }
        public bool IsBold { get; set; }
        public string Color { get; set; } // Hex color code (e.g., "#FF0000" for red)
        public bool IsNewParagraph { get; set; } = true; // Default to new paragraph
    }
}
