using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.ViewModel.Translation
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationVariantViewModel : PriorityWordViewModel, IDisposable
    {
        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly IEqualityComparer<IWord> _wordsEqualityComparer;

        public TranslationVariantViewModel(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] IMessageHub messenger,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] ILog logger,
            [NotNull] IWordPriorityRepository wordPriorityRepository,
            [NotNull] IWordImagesInfoRepository imagesInfoRepository,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer)
            : base(textToSpeechPlayer, messenger, wordsProcessor, logger, wordPriorityRepository)
        {
            if (imagesInfoRepository == null)
                throw new ArgumentNullException(nameof(imagesInfoRepository));

            _wordsEqualityComparer = wordsEqualityComparer ?? throw new ArgumentNullException(nameof(wordsEqualityComparer));
            _subscriptionTokens.Add(messenger.Subscribe<WordImagesInfo>(OnWordImagesInfoReceived));
            var wordImagesInfo = imagesInfoRepository.GetImagesInfo(TranslationEntryId, this);
            UpdateImage(wordImagesInfo);
        }

        [CanBeNull]
        public PriorityWordViewModel[] Synonyms { get; set; }

        [CanBeNull]
        public WordViewModel[] Meanings { get; set; }

        [CanBeNull]
        public Example[] Examples { get; set; }

        [CanBeNull]
        public BitmapSource Image { get; private set; }

        public void Dispose()
        {
            foreach (var subscriptionToken in _subscriptionTokens)
                Messenger.UnSubscribe(subscriptionToken);
        }

        private void UpdateImage([CanBeNull] WordImagesInfo wordImagesInfo)
        {
            var imageBytes = wordImagesInfo?.Images.FirstOrDefault(x => x.ThumbnailBitmap != null)
                ?.ThumbnailBitmap;
            Image = LoadImage(imageBytes);
        }

        [CanBeNull]
        private static BitmapImage LoadImage([CanBeNull] byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }

            image.Freeze();
            return image;
        }

        private void OnWordImagesInfoReceived([NotNull] WordImagesInfo wordImagesInfo)
        {
            if (Equals(wordImagesInfo.TranslationEntryId, TranslationEntryId) && _wordsEqualityComparer.Equals(this, wordImagesInfo))
            {
                Logger.InfoFormat("Received image for {0}", this);
                UpdateImage(wordImagesInfo);
            }
        }
    }
}