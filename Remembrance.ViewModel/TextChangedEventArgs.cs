using System;

namespace Remembrance.ViewModel
{
    public delegate void TextChangedEventHandler(object sender, TextChangedEventArgs e);

    public sealed class TextChangedEventArgs : EventArgs
    {
        public TextChangedEventArgs(string? newValue, string? oldValue)
        {
            NewValue = newValue;
            OldValue = oldValue;
        }

        public string? NewValue { get; }

        public string? OldValue { get; }
    }
}