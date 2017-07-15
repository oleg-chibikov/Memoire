using System.Windows;

namespace Remembrance.Card.View
{
    public sealed partial class WordControl
    {
        public static readonly DependencyProperty SpeakerAlignmentProperty = DependencyProperty.Register(nameof(SpeakerAlignment), typeof(HorizontalAlignment), typeof(WordControl), new PropertyMetadata(null));

        public WordControl()
        {
            InitializeComponent();
        }

        public HorizontalAlignment SpeakerAlignment
        {
            get => (HorizontalAlignment) GetValue(SpeakerAlignmentProperty);
            set => SetValue(SpeakerAlignmentProperty, value);
        }
    }
}