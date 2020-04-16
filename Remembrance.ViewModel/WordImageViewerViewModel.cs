using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.Logging;
using Easy.MessageHub;
using PropertyChanged;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.ImageSearch;
using Remembrance.Contracts.ImageSearch.Data;
using Remembrance.Resources;
using Scar.Common.Async;
using Scar.Common.Messages;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class WordImageViewerViewModel : BaseViewModel
    {
        internal const string DefaultSearchTextTemplate = "{0} {1}";

        readonly ICancellationTokenSourceProvider _cancellationTokenSourceProvider;

        readonly IImageDownloader _imageDownloader;

        readonly IImageSearcher _imageSearcher;

        readonly bool _isReadonly;

        readonly ILog _logger;

        readonly IMessageHub _messageHub;

        readonly string _thisAndParentSearchText;

        readonly IWordImageInfoRepository _wordImageInfoRepository;

        readonly IWordImageSearchIndexRepository _wordImageSearchIndexRepository;

        readonly WordKey _wordKey;

        int?[] _nonAvailableIndexes = new int?[2];

        bool _shouldRepeat;

        public WordImageViewerViewModel(
            WordKey wordKey,
            string parentText,
            ILog logger,
            IWordImageInfoRepository wordImageInfoRepository,
            ICancellationTokenSourceProvider cancellationTokenSourceProvider,
            IImageDownloader imageDownloader,
            IImageSearcher imageSearcher,
            IMessageHub messageHub,
            IWordImageSearchIndexRepository wordImageSearchIndexRepository,
            ICommandManager commandManager,
            bool isReadonly = false)
            : base(commandManager)
        {
            _wordKey = wordKey ?? throw new ArgumentNullException(nameof(wordKey));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _wordImageInfoRepository = wordImageInfoRepository ?? throw new ArgumentNullException(nameof(wordImageInfoRepository));
            _cancellationTokenSourceProvider = cancellationTokenSourceProvider ?? throw new ArgumentNullException(nameof(cancellationTokenSourceProvider));
            _imageDownloader = imageDownloader ?? throw new ArgumentNullException(nameof(imageDownloader));
            _imageSearcher = imageSearcher ?? throw new ArgumentNullException(nameof(imageSearcher));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _wordImageSearchIndexRepository = wordImageSearchIndexRepository ?? throw new ArgumentNullException(nameof(wordImageSearchIndexRepository));
            SetNextImageCommand = AddCommand(SetNextImageAsync);
            SetPreviousImageCommand = AddCommand(SetPreviousImageAsync);
            ReloadImageCommand = AddCommand(ReloadImageAsync);

            _thisAndParentSearchText = string.Format(DefaultSearchTextTemplate, wordKey.Word.Text, parentText);
            _isReadonly = isReadonly;

            var wordImageInfo = _wordImageInfoRepository.TryGetById(_wordKey);
            _shouldRepeat = true;
            ConstructionTask = wordImageInfo != null ? LoadInitialImage(wordImageInfo) : SetNextOrPreviousImageAsync(true);
        }

        public byte[]? ThumbnailBytes { get; private set; }

        public bool IsLoading { get; private set; } = true;

        [DependsOn(nameof(ThumbnailBytes), nameof(IsLoading))]
        public bool IsReloadVisible => ThumbnailBytes == null && !IsLoading;

        public bool IsSetNextImageVisible => !_isReadonly;

        [DependsOn(nameof(SearchIndex), nameof(IsAlternate))]
        public bool IsSetPreviousImageVisible => !_isReadonly && !IsFirst;

        public ICommand ReloadImageCommand { get; }

        public ICommand SetNextImageCommand { get; }

        public ICommand SetPreviousImageCommand { get; }

        [DependsOn(nameof(ImageName), nameof(ImageUrl), nameof(SearchIndex), nameof(SearchText), nameof(IsAlternate))]

        public string? ToolTip => _isReadonly ? null : $"{SearchIndex + 1}{AlternateInfo}. {SearchText}: {ImageName} ({ImageUrl})";

        internal Task ConstructionTask { get; }

        internal bool IsAlternate { get; private set; }

        internal int SearchIndex { get; private set; }

        string? AlternateInfo => IsAlternate ? " (*)" : null;

        string? ImageName { get; set; }

        string? ImageUrl { get; set; }

        bool IsFirst => SearchIndex == 0 && !IsAlternate;

        string SearchText => IsAlternate ? _wordKey.Word.Text : _thisAndParentSearchText;

        public override string ToString()
        {
            return $"Image for {_wordKey}";
        }

        internal async Task ReloadImageAsync()
        {
            _shouldRepeat = true;
            await SetNextOrPreviousImageAsync(true).ConfigureAwait(false);
        }

        internal async Task SetNextImageAsync()
        {
            await SetNextOrPreviousImageAsync(true).ConfigureAwait(false);
        }

        internal async Task SetPreviousImageAsync()
        {
            await SetNextOrPreviousImageAsync(false).ConfigureAwait(false);
        }

        async Task LoadInitialImage(WordImageInfo wordImageInfo)
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
        async Task SetNextOrPreviousImageAsync(bool increase)
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

        async Task SetWordImageAsync(int index, string searchText)
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
                                _shouldRepeat = true;
                            }
                            else
                            {
                                if (imagesUrls.Count > 1)
                                {
                                    throw new InvalidOperationException("Search should return only one image");
                                }

                                ImageInfoWithBitmap? imageInfoWithBitmap = null;
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
                                WordImageSearchIndex? wordImageSearchIndex = null;
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

        async Task UpdateImageViewAsync(WordImageInfo wordImageInfo, WordImageSearchIndex? wordImageSearchIndex)
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
            var image = wordImageInfo.Image;
            byte[]? thumbnailBytes;
            if (image == null || (thumbnailBytes = image.ThumbnailBitmap) == null || thumbnailBytes.Length == 0)
            {
                // Try to search the next image if the current image is not available, but has metadata
                await SetNextOrPreviousImageAsync(true).ConfigureAwait(false);
                return;
            }

            _shouldRepeat = false;
            ImageName = image.ImageInfo.Name;
            ImageUrl = image.ImageInfo.Url;
            ThumbnailBytes = thumbnailBytes;
        }
    }
}
