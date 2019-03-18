using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.ImageSearch;
using Remembrance.Contracts.ImageSearch.Data;
using Remembrance.Resources;
using Scar.Common.Async;
using Scar.Common.Messages;
using Scar.Common.WPF.Commands;

namespace Remembrance.ViewModel
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class WordImageViewerViewModel
    {
        internal const string DefaultSearchTextTemplate = "{0} {1}";

        [NotNull]
        private readonly ICancellationTokenSourceProvider _cancellationTokenSourceProvider;

        [NotNull]
        private readonly IImageDownloader _imageDownloader;

        [NotNull]
        private readonly IBitmapImageLoader _bitmapBitmapImageLoader;

        [NotNull]
        private readonly IImageSearcher _imageSearcher;

        private readonly bool _isReadonly;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly string _thisAndParentSearchText;

        [NotNull]
        private readonly IWordImageInfoRepository _wordImageInfoRepository;

        [NotNull]
        private readonly IWordImageSearchIndexRepository _wordImageSearchIndexRepository;

        [NotNull]
        private readonly WordKey _wordKey;

        private int?[] _nonAvailableIndexes = new int?[2];

        private bool _shouldRepeat;

        public WordImageViewerViewModel(
            [NotNull] WordKey wordKey,
            [NotNull] string parentText,
            [NotNull] ILog logger,
            [NotNull] IWordImageInfoRepository wordImageInfoRepository,
            [NotNull] ICancellationTokenSourceProvider cancellationTokenSourceProvider,
            [NotNull] IImageDownloader imageDownloader,
            [NotNull] IImageSearcher imageSearcher,
            [NotNull] IMessageHub messageHub,
            [NotNull] IWordImageSearchIndexRepository wordImageSearchIndexRepository,
            [NotNull] IBitmapImageLoader bitmapImageLoader,
            bool isReadonly = false)
        {
            _wordKey = wordKey ?? throw new ArgumentNullException(nameof(wordKey));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _wordImageInfoRepository = wordImageInfoRepository ?? throw new ArgumentNullException(nameof(wordImageInfoRepository));
            _cancellationTokenSourceProvider = cancellationTokenSourceProvider ?? throw new ArgumentNullException(nameof(cancellationTokenSourceProvider));
            _imageDownloader = imageDownloader ?? throw new ArgumentNullException(nameof(imageDownloader));
            _imageSearcher = imageSearcher ?? throw new ArgumentNullException(nameof(imageSearcher));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _wordImageSearchIndexRepository = wordImageSearchIndexRepository ?? throw new ArgumentNullException(nameof(wordImageSearchIndexRepository));
            _bitmapBitmapImageLoader = bitmapImageLoader ?? throw new ArgumentNullException(nameof(bitmapImageLoader));
            SetNextImageCommand = new AsyncCorrelationCommand(SetNextImageAsync);
            SetPreviousImageCommand = new AsyncCorrelationCommand(SetPreviousImageAsync);
            ReloadImageCommand = new AsyncCorrelationCommand(ReloadImageAsync);

            _thisAndParentSearchText = string.Format(DefaultSearchTextTemplate, wordKey.Word.Text, parentText);
            _isReadonly = isReadonly;

            var wordImageInfo = _wordImageInfoRepository.TryGetById(_wordKey);
            _shouldRepeat = true;
            ConstructionTask = wordImageInfo != null ? LoadInitialImage(wordImageInfo) : SetNextOrPreviousImageAsync(true);
        }

        [CanBeNull]
        public BitmapSource Image { get; private set; }

        public bool IsLoading { get; private set; } = true;

        [DependsOn(nameof(Image), nameof(IsLoading))]
        public bool IsReloadVisible => Image == null && !IsLoading;

        public bool IsSetNextImageVisible => !_isReadonly;

        [DependsOn(nameof(SearchIndex), nameof(IsAlternate))]
        public bool IsSetPreviousImageVisible => !_isReadonly && !IsFirst;

        [NotNull]
        public ICommand ReloadImageCommand { get; }

        [NotNull]
        public ICommand SetNextImageCommand { get; }

        [NotNull]
        public ICommand SetPreviousImageCommand { get; }

        [DependsOn(nameof(ImageName), nameof(ImageUrl), nameof(SearchIndex), nameof(SearchText), nameof(IsAlternate))]
        [CanBeNull]
        public string ToolTip => _isReadonly ? null : $"{SearchIndex + 1}{AlternateInfo}. {SearchText}: {ImageName} ({ImageUrl})";

        [NotNull]
        internal Task ConstructionTask { get; }

        internal bool IsAlternate { get; private set; }

        internal int SearchIndex { get; private set; }

        [CanBeNull]
        private string AlternateInfo => IsAlternate ? " (*)" : null;

        [CanBeNull]
        private string ImageName { get; set; }

        [CanBeNull]
        private string ImageUrl { get; set; }

        private bool IsFirst => SearchIndex == 0 && !IsAlternate;

        [NotNull]
        private string SearchText => IsAlternate ? _wordKey.Word.Text : _thisAndParentSearchText;

        public override string ToString()
        {
            return $"Image for {_wordKey}";
        }

        [NotNull]
        internal async Task ReloadImageAsync()
        {
            _shouldRepeat = true;
            await SetNextOrPreviousImageAsync(true).ConfigureAwait(false);
        }

        [NotNull]
        internal async Task SetNextImageAsync()
        {
            await SetNextOrPreviousImageAsync(true).ConfigureAwait(false);
        }

        [NotNull]
        internal async Task SetPreviousImageAsync()
        {
            await SetNextOrPreviousImageAsync(false).ConfigureAwait(false);
        }

        private async Task LoadInitialImage([NotNull] WordImageInfo wordImageInfo)
        {
            IsLoading = true;
            var wordImageSearchIndex = _wordImageSearchIndexRepository.TryGetById(wordImageInfo.Id);
            await UpdateImageViewAsync(wordImageInfo, wordImageSearchIndex).ConfigureAwait(false);
            IsLoading = false;
        }

        /// <summary>
        /// The set next or previous image async.
        /// </summary>
        /// <remarks>
        /// This function swaps searchText every next time until one of the texts stops to give results
        /// </remarks>
        [NotNull]
        private async Task SetNextOrPreviousImageAsync(bool increase)
        {
            if (IsFirst && !increase)
            {
                return;
            }

            IsLoading = true;
            var searchIndex = SearchIndex;
            if (!_shouldRepeat)
            {
                _logger.TraceFormat("Setting {1} image for {0}...", this, increase ? "next" : "previous");
                var swappedPosition = IsAlternate ? 0 : 1;
                var position = IsAlternate ? 1 : 0;

                var nonAvailableIndex = _nonAvailableIndexes[position];
                var nonAvailableIndexForSwapped = _nonAvailableIndexes[swappedPosition];
                var nextPossibleIndex = increase ? searchIndex + 1 : searchIndex - 1;
                var noSwap = false;
                if (nonAvailableIndexForSwapped == null || nextPossibleIndex < nonAvailableIndexForSwapped)
                {
                    IsAlternate = !IsAlternate;
                }
                else
                {
                    noSwap = true;
                    if (nonAvailableIndex != null && nextPossibleIndex >= nonAvailableIndex)
                    {
                        _messageHub.Publish(string.Format(Errors.NoMoreImages, _wordKey).ToWarning());
                        IsLoading = false;
                        return;
                    }
                }

                if (increase)
                {
                    if (noSwap || !IsAlternate)
                    {
                        searchIndex++;
                    }
                }
                else
                {
                    if (noSwap || IsAlternate)
                    {
                        searchIndex--;
                    }
                }
            }

            _shouldRepeat = false;

            // after getting isAlternate
            await SetWordImageAsync(searchIndex, SearchText).ConfigureAwait(false);
            IsLoading = false;
        }

        [NotNull]
        private async Task SetWordImageAsync(int index, [NotNull] string searchText)
        {
            WordImageInfo wordImageInfo;
            _logger.TraceFormat("Setting new image for {0} at search index {1} with searchText {2}...", _wordKey, index, searchText);
            try
            {
                await _cancellationTokenSourceProvider.ExecuteAsyncOperation(
                        async cancellationToken =>
                        {
                            var imagesUrls = await _imageSearcher.SearchImagesAsync(searchText, cancellationToken, index).ConfigureAwait(false);
                            if (imagesUrls == null)
                            {
                                // Null means error - just displaying Error instead of image. Message is already shown to client;
                                _logger.WarnFormat("Cannot search images for {0}", _wordKey);
                                _shouldRepeat = true;
                            }
                            else
                            {
                                if (imagesUrls.Count > 1)
                                {
                                    throw new InvalidOperationException("Search should return only one image");
                                }

                                ImageInfoWithBitmap imageInfoWithBitmap = null;
                                if (imagesUrls.Any())
                                {
                                    var imageDownloadTasks = imagesUrls.Select(
                                        async image => new ImageInfoWithBitmap
                                        {
                                            ImageBitmap = null,
                                            ThumbnailBitmap = await _imageDownloader.DownloadImageAsync(image.ThumbnailUrl, cancellationToken).ConfigureAwait(false),
                                            ImageInfo = image
                                        });
                                    imageInfoWithBitmap = (await Task.WhenAll(imageDownloadTasks).ConfigureAwait(false)).SingleOrDefault();
                                    if (imageInfoWithBitmap == null)
                                    {
                                        _logger.WarnFormat("Cannot download image for {0}", _wordKey);
                                    }
                                }
                                else
                                {
                                    var position = IsAlternate ? 1 : 0;
                                    _nonAvailableIndexes[position] = index;
                                }

                                wordImageInfo = new WordImageInfo(_wordKey, imageInfoWithBitmap, _nonAvailableIndexes);
                                _wordImageInfoRepository.Upsert(wordImageInfo);
                                WordImageSearchIndex wordImageSearchIndex = null;
                                if (index > 0 || IsAlternate)
                                {
                                    wordImageSearchIndex = new WordImageSearchIndex(_wordKey, index, IsAlternate);
                                    _wordImageSearchIndexRepository.Upsert(wordImageSearchIndex);
                                }
                                else
                                {
                                    // Only update - no need to waste DB space with default values
                                    if (_wordImageSearchIndexRepository.Check(_wordKey))
                                    {
                                        wordImageSearchIndex = new WordImageSearchIndex(_wordKey, index, IsAlternate);
                                        _wordImageSearchIndexRepository.Update(wordImageSearchIndex);
                                    }
                                }

                                _logger.DebugFormat("Image for {0} at search index {1} was saved", _wordKey, index);

                                await UpdateImageViewAsync(wordImageInfo, wordImageSearchIndex).ConfigureAwait(false);
                            }
                        })
                    .ConfigureAwait(false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [NotNull]
        private async Task UpdateImageViewAsync([NotNull] WordImageInfo wordImageInfo, [CanBeNull] WordImageSearchIndex wordImageSearchIndex)
        {
            if (wordImageSearchIndex != null)
            {
                SearchIndex = wordImageSearchIndex.SearchIndex;
                IsAlternate = wordImageSearchIndex.IsAlternate;
            }
            else
            {
                SearchIndex = 0;
                IsAlternate = false;
            }

            _nonAvailableIndexes = wordImageInfo.NonAvailableIndexes;
            var imageBytes = wordImageInfo.Image?.ThumbnailBitmap;
            if (imageBytes == null || imageBytes.Length == 0)
            {
                // Try to search the next image if the current image is not available, but has metadata
                await SetNextOrPreviousImageAsync(true).ConfigureAwait(false);
                return;
            }

            _shouldRepeat = false;
            ImageName = wordImageInfo.Image.ImageInfo.Name;
            ImageUrl = wordImageInfo.Image.ImageInfo.Url;
            Image = _bitmapBitmapImageLoader.LoadImage(imageBytes);
        }
    }
}