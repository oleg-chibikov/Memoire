using System.Windows.Media.Imaging;
using JetBrains.Annotations;

namespace Remembrance.ViewModel
{
    public interface IBitmapImageLoader
    {
        [NotNull]
        BitmapImage LoadImage([NotNull] byte[] imageBytes);
    }
}