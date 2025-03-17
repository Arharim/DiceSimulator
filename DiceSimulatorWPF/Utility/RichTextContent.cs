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
        public string Color { get; set; }
        public bool IsNewParagraph { get; set; } = true;
    }
}
