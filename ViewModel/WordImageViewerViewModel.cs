using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Easy.MessageHub;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Resources;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.Async;
using Scar.Common.Messages;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;
using Scar.Services.Contracts;
using Scar.Services.Contracts.Data.ImageSearch;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class WordImageViewerViewModel : BaseViewModel
    {
        internal const string DefaultSearchTextTemplate = "{0} {1}";
        readonly ICancellationTokenSourceProvider _cancellationTokenSourceProvider;
        readonly ISharedSettingsRepository _sharedSettingsRepository;
        readonly IImageDownloader _imageDownloader;
        readonly IImageSearcher _imageSearcher;
        readonly bool _isReadonly;
        readonly ILogger _logger;
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
            ILogger<WordImageViewerViewModel> logger,
            IWordImageInfoRepository wordImageInfoRepository,
            ICancellationTokenSourceProvider cancellationTokenSourceProvider,
            IImageDownloader imageDownloader,
            IImageSearcher imageSearcher,
            IMessageHub messageHub,
            IWordImageSearchIndexRepository wordImageSearchIndexRepository,
            ICommandManager commandManager,
            ISharedSettingsRepository sharedSettingsRepository,
            bool isReadonly = false) : base(commandManager)
        {
            _wordKey = wordKey ?? throw new ArgumentNullException(nameof(wordKey));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _wordImageInfoRepository = wordImageInfoRepository ?? throw new ArgumentNullException(nameof(wordImageInfoRepository));
            _cancellationTokenSourceProvider = cancellationTokenSourceProvider ?? throw new ArgumentNullException(nameof(cancellationTokenSourceProvider));
            _imageDownloader = imageDownloader ?? throw new ArgumentNullException(nameof(imageDownloader));
            _imageSearcher = imageSearcher ?? throw new ArgumentNullException(nameof(imageSearcher));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _wordImageSearchIndexRepository = wordImageSearchIndexRepository ?? throw new ArgumentNullException(nameof(wordImageSearchIndexRepository));
            _sharedSettingsRepository = sharedSettingsRepository ?? throw new ArgumentNullException(nameof(sharedSettingsRepository));
            SetNextImageCommand = AddCommand(SetNextImageAsync);
            SetPreviousImageCommand = AddCommand(SetPreviousImageAsync);
            ReloadImageCommand = AddCommand(ReloadImageAsync);

            _thisAndParentSearchText = string.Format(CultureInfo.InvariantCulture, DefaultSearchTextTemplate, wordKey.Word.Text, parentText);
            _isReadonly = isReadonly;

            var wordImageInfo = _wordImageInfoRepository.TryGetById(_wordKey);
            _shouldRepeat = true;
            ConstructionTask = wordImageInfo != null ? LoadInitialImageAsync(wordImageInfo) : SetNextOrPreviousImageAsync(true, false);
        }

        public IReadOnlyCollection<byte>? ThumbnailBytes { get; private set; }

        public bool IsLoading { get; private set; } = true;

        [DependsOn(nameof(ThumbnailBytes), nameof(IsLoading))]
        public bool IsReloadVisible => (ThumbnailBytes == null) && !IsLoading;

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

        bool IsFirst => (SearchIndex == 0) && !IsAlternate;

        string SearchText => IsAlternate ? _wordKey.Word.Text : _thisAndParentSearchText;

        public override string ToString()
        {
            return $"Image for {_wordKey}";
        }

        internal async Task ReloadImageAsync()
        {
            _shouldRepeat = true;
            await SetNextOrPreviousImageAsync(true, true).ConfigureAwait(false);
        }

        internal async Task SetNextImageAsync()
        {
            await SetNextOrPreviousImageAsync(true, true).ConfigureAwait(false);
        }

        internal async Task SetPreviousImageAsync()
        {
            await SetNextOrPreviousImageAsync(false, true).ConfigureAwait(false);
        }

        async Task LoadInitialImageAsync(WordImageInfo wordImageInfo)
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
        /// This function swaps searchText every next time until one of the texts stops to give results.
        /// </remarks>
        async Task SetNextOrPreviousImageAsync(bool increase, bool showException)
        {
            if (IsFirst && !increase)
            {
                return;
            }

            IsLoading = true;
            var searchIndex = SearchIndex;
            if (!_shouldRepeat)
            {
                _logger.LogTrace("Setting {WordImage} image for {Direction}...", this, increase ? "next" : "previous");
                var swappedPosition = IsAlternate ? 0 : 1;
                var position = IsAlternate ? 1 : 0;

                var nonAvailableIndex = _nonAvailableIndexes[position];
                var nonAvailableIndexForSwapped = _nonAvailableIndexes[swappedPosition];
                var nextPossibleIndex = increase ? searchIndex + 1 : searchIndex - 1;
                var noSwap = false;
                if ((nonAvailableIndexForSwapped == null) || (nextPossibleIndex < nonAvailableIndexForSwapped))
                {
                    IsAlternate = !IsAlternate;
                }
                else
                {
                    noSwap = true;
                    if ((nonAvailableIndex != null) && (nextPossibleIndex >= nonAvailableIndex))
                    {
                        _messageHub.Publish(string.Format(CultureInfo.InvariantCulture, Errors.NoMoreImages, _wordKey).ToWarning());
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
            await SetWordImageAsync(searchIndex, SearchText, showException).ConfigureAwait(false);
            IsLoading = false;
        }

        async Task SetWordImageAsync(int index, string searchText, bool showException)
        {
            WordImageInfo wordImageInfo;
            _logger.LogTrace("Setting new image for {WordKey} at search index {Index} with searchText {SearchText}...", _wordKey, index, searchText);
            try
            {
                await _cancellationTokenSourceProvider.ExecuteOperationAsync(
                        async cancellationToken =>
                        {
                            var imageInfos = await _imageSearcher.SearchImagesAsync(
                                    searchText,
                                    _sharedSettingsRepository.SolveQwantCaptcha,
                                    "en_AU", // TODO
                                    index,
                                    1,
                                    ex =>
                                    {
                                        if (showException)
                                        {
                                            _messageHub.Publish((Errors.CannotGetQwantResults + ": " + ex.Message).ToError(ex));
                                        }
                                    },
                                    cancellationToken)
                                .ConfigureAwait(false);
                            if (imageInfos == null)
                            {
                                // Null means error - just displaying Error instead of image. Message is already shown to client;
                                _shouldRepeat = true;
                            }
                            else
                            {
                                if (imageInfos.Count > 1)
                                {
                                    throw new InvalidOperationException("Search should return only one image");
                                }

                                ImageInfoWithBitmap? imageInfoWithBitmap = null;
                                if (imageInfos.Count > 0)
                                {
                                    var imageDownloadTasks = imageInfos.Select(
                                        async image => new ImageInfoWithBitmap
                                        {
                                            ImageBitmap = null,
                                            ThumbnailBitmap = await _imageDownloader.DownloadImageAsync(
                                                    image.ThumbnailUrl,
                                                    ex => _messageHub.Publish(Errors.CannotDownloadImage.ToError(ex)),
                                                    cancellationToken)
                                                .ConfigureAwait(false),
                                            ImageInfo = image
                                        });
                                    imageInfoWithBitmap = (await Task.WhenAll(imageDownloadTasks).ConfigureAwait(false)).SingleOrDefault();
                                    if (imageInfoWithBitmap == null)
                                    {
                                        _logger.LogWarning("Cannot download image for {WordKey}", _wordKey);
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
                                if ((index > 0) || IsAlternate)
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

                                _logger.LogDebug("Image for {WordKey} at search index {Index} was saved", _wordKey, index);

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

            _nonAvailableIndexes = wordImageInfo.NonAvailableIndexes.ToArray();
            var image = wordImageInfo.Image;
            IReadOnlyCollection<byte>? thumbnailBytes;
            if ((image == null) || ((thumbnailBytes = image.ThumbnailBitmap) == null) || (thumbnailBytes.Count == 0))
            {
                // Try to search the next image if the current image is not available, but has metadata
                await SetNextOrPreviousImageAsync(true, false).ConfigureAwait(false);
                return;
            }

            _shouldRepeat = false;
            ImageName = image.ImageInfo.Name;
            ImageUrl = image.ImageInfo.Url.ToString();
            ThumbnailBytes = thumbnailBytes;
        }
    }
}
