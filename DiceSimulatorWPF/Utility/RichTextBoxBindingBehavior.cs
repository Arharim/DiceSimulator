using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Xaml.Behaviors;

namespace DiceSimulatorWPF.Utility
{
    public class RichTextBoxBindingBehavior : Behavior<RichTextBox>
    {
        public static readonly DependencyProperty DocumentContentProperty =
            DependencyProperty.Register(
                nameof(DocumentContent),
                typeof(object),
                typeof(RichTextBoxBindingBehavior),
                new PropertyMetadata(null, OnDocumentContentChanged)
            );

        public object DocumentContent
        {
            get => GetValue(DocumentContentProperty);
            set => SetValue(DocumentContentProperty, value);
        }

        private static void OnDocumentContentChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (d is RichTextBoxBindingBehavior behavior && behavior.AssociatedObject != null)
            {
                var richTextBox = behavior.AssociatedObject;
                if (e.NewValue is FlowDocument document)
                {
                    richTextBox.Document = document;
                }
                else
                {
                    richTextBox.Document = new FlowDocument();
                }
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            UpdateDocument();
        }

        private void UpdateDocument()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.Document = DocumentContent as FlowDocument ?? new FlowDocument();
            }
        }
    }
}
