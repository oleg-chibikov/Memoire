﻿using JetBrains.Annotations;

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    public class TextEntryViewModel
    {
        [NotNull]
        public virtual string Text { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}