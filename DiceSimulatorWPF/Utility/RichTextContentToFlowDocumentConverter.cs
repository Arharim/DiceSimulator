using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace DiceSimulatorWPF.Utility
{
    public class RichTextContentToFlowDocumentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parametr, CultureInfo culture)
        {
            if (value is RichTextContent content)
            {
                var doc = new FlowDocument();
                Paragraph currentParagraph = null;

                foreach (var section in content.Sections)
                {
                    var run = new Run(section.Text)
                    {
                        FontWeight = section.IsBold ? FontWeights.Bold : FontWeights.Normal,
                        Foreground = section.Color != null
                            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(section.Color))
                            : Brushes.Black
                    };

                    if (section.IsNewParagraph || currentParagraph == null)
                    {
                        currentParagraph = new Paragraph();
                        doc.Blocks.Add(currentParagraph);
                    }

                    currentParagraph.Inlines.Add(run);
                }

                return doc;
            }
            return new FlowDocument();
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            throw new NotImplementedException();
        }
    }
}
