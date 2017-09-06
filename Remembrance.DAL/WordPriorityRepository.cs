﻿using System.Linq;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL
{
    [UsedImplicitly]
    internal sealed class WordPriorityRepository : LiteDbRepository<WordPriority, int>, IWordPriorityRepository
    {
        public WordPriorityRepository([NotNull] ILog logger)
            : base(logger)
        {
            Collection.EnsureIndex(x => x.Text);
            Collection.EnsureIndex(x => x.PartOfSpeech);
            Collection.EnsureIndex(x => x.TranslationEntryId);
        }

        [NotNull]
        protected override string DbName => nameof(WordPriority);

        [NotNull]
        protected override string DbPath => Paths.SharedDataPath;

        public IWord[] GetPriorityWordsForTranslationEntry(object translationEntryId)
        {
            return Collection.Find(x => x.TranslationEntryId == translationEntryId).Cast<IWord>().ToArray();
        }

        public bool IsPriority(IWord word, object translationEntryId)
        {
            return Collection.Exists(x => x.Text == word.Text && x.PartOfSpeech == word.PartOfSpeech && x.TranslationEntryId == translationEntryId);
        }

        public void MarkPriority(IWord word, object translationEntryId)
        {
            Collection.Upsert(new WordPriority(word.Text, word.PartOfSpeech, translationEntryId));
        }

        public void MarkNonPriority(IWord word, object translationEntryId)
        {
            Collection.Delete(x => x.Text == word.Text && x.PartOfSpeech == word.PartOfSpeech && x.TranslationEntryId == translationEntryId);
        }
    }
}