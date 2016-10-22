﻿using System;
using JetBrains.Annotations;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    public delegate bool TextChangedEventHandler(object sender, TextChangedEventArgs e);

    public sealed class TextChangedEventArgs : EventArgs
    {
        public TextChangedEventArgs([CanBeNull] string newValue, [CanBeNull] string oldValue)
        {
            NewValue = newValue;
            OldValue = oldValue;
        }

        [CanBeNull]
        public string NewValue { get; private set; }

        [CanBeNull]
        public string OldValue { get; private set; }
    }
}