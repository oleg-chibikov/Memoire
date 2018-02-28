using System.Windows;

namespace Remembrance.View.Various
{
    /// <summary>
    /// The favorite button.
    /// </summary>
    internal sealed partial class FavoriteButton
    {
        public static readonly DependencyProperty IsFavoritedProperty = DependencyProperty.Register(nameof(IsFavorited), typeof(bool), typeof(FavoriteButton), new PropertyMetadata(null));

        public FavoriteButton()
        {
            InitializeComponent();
        }

        public bool IsFavorited
        {
            get => (bool)GetValue(IsFavoritedProperty);
            set => SetValue(IsFavoritedProperty, value);
        }
    }
}