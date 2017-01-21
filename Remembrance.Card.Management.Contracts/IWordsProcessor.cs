﻿using JetBrains.Annotations;

namespace Remembrance.Card.Management.Contracts
{
    public interface IWordsProcessor
    {
        bool ProcessNewWord([NotNull] string word, [CanBeNull] string sourceLanguage = null, [CanBeNull] string targetLanguage = null, bool showCard = true);
        bool ChangeText(int id, [NotNull] string newWord, [NotNull] string sourceLanguage, [NotNull] string targetLanguage, bool showCard = true);
    }
}