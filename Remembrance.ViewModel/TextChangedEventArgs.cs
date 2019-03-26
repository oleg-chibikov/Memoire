using System;
using JetBrains.Annotations;

namespace Remembrance.ViewModel
{
    public delegate void TextChangedEventHandler(object sender, TextChangedEventArgs e);

    public sealed class TextChangedEventArgs : EventArgs
    {
        public TextChangedEventArgs([CanBeNull] string? newValue, [CanBeNull] string? oldValue)
        {
            NewValue = newValue;
            OldValue = oldValue;
        }

        [CanBeNull]
        public string? NewValue { get; }

        [CanBeNull]
        public string? OldValue { get; }
    }
}