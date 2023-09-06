using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using Easy.MessageHub;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.ViewModel;
using Moq;
using Scar.Common.Async;
using Scar.Common.Messages;
using Scar.Services.Contracts;
using Scar.Services.Contracts.Data;
using Scar.Services.Contracts.Data.ImageSearch;

namespace Mémoire.Test
{
    static class WordImageViewerViewModelTestsExtensions
    {
        public const string FallbackSearchText = "foo";
        public const string ParentText = "bar";
        public static readonly string DefaultSearchText = string.Format(CultureInfo.InvariantCulture, WordImageViewerViewModel.DefaultSearchTextTemplate, FallbackSearchText, ParentText);

        public static AutoMock CreateAutoMock()
        {
            var autoMock = AutoMock.GetLoose();
            autoMock.Mock<ICancellationTokenSourceProvider>()
                .Setup(x => x.ExecuteOperationAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<bool>()))
                .Returns((Func<CancellationToken, Task> f, bool _) => f(CancellationToken.None));
            autoMock.Mock<IImageDownloader>().Setup(x => x.DownloadImageAsync(It.IsAny<Uri>(), It.IsAny<Action<Exception>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new byte[1]);
            return autoMock;
        }

        public static ImageInfoWithBitmap CreateImageInfoWithBitmap(this AutoMock autoMock)
        {
            var imageInfoWithBitmap = autoMock.Create<ImageInfoWithBitmap>();
            imageInfoWithBitmap.ThumbnailBitmap = new byte[1];
            imageInfoWithBitmap.ImageBitmap = new byte[1];
            imageInfoWithBitmap.ImageInfo = autoMock.Create<ImageInfo>();
            SetupImageInfo(imageInfoWithBitmap.ImageInfo);
            return imageInfoWithBitmap;
        }

        public static WordKey CreateMockKey(this AutoMock autoMock)
        {
            var key = autoMock.Create<WordKey>();
            var word = key.Word;
            word.Text = FallbackSearchText;
            var translationEntryKey = key.Key;
            translationEntryKey.SourceLanguage = LanguageConstants.RuLanguage;
            translationEntryKey.TargetLanguage = LanguageConstants.EnLanguage;
            translationEntryKey.Text = word.Text;
            return key;
        }

        public static async Task<WordImageViewerViewModel> CreateViewModelAsync(this AutoMock autoMock, bool reset = true, bool wait = true)
        {
            var sut = autoMock.Create<WordImageViewerViewModel>(new TypedParameter(typeof(string), ParentText), new TypedParameter(typeof(WordKey), CreateMockKey(autoMock)));
            if (wait)
            {
                await sut.ConstructionTask.ConfigureAwait(false);
            }

            if (reset)
            {
                Reset(autoMock);
            }

            return sut;
        }

        public static void Reset(this AutoMock autoMock)
        {
            autoMock.Mock<IImageSearcher>().ResetCalls();
            autoMock.Mock<IImageDownloader>().ResetCalls();
        }

        public static void VerifyDownloadCount(this AutoMock autoMock, Func<Times> times)
        {
            autoMock.Mock<IImageDownloader>().Verify(x => x.DownloadImageAsync(It.IsAny<Uri>(), It.IsAny<Action<Exception>>(), It.IsAny<CancellationToken>()), times);
        }

        public static void VerifyImagesSearch(this AutoMock autoMock, Func<Times> times, string? searchText = null)
        {
            autoMock.Mock<IImageSearcher>()
                .Verify(
                    x => x.SearchImagesAsync(
                        It.Is<string>(s => (searchText == null) || (s == searchText)),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Action<Exception>>(),
                        It.IsAny<CancellationToken>()),
                    times);
        }

        public static void VerifyMessage(this AutoMock autoMock, Func<Times> times)
        {
            autoMock.Mock<IMessageHub>().Verify(x => x.Publish(It.IsAny<Message>()), times);
        }

        public static void WithImageSearchError(this AutoMock autoMock, string? searchText = null)
        {
            WithImageSearchResult(autoMock, null, searchText);
        }

        public static void WithImageSearchResult(this AutoMock autoMock, IReadOnlyCollection<ImageInfo>? imageInfos, string? searchText = null)
        {
            autoMock.Mock<IImageSearcher>()
                .Setup(
                    x => x.SearchImagesAsync(
                        It.Is<string>(s => (searchText == null) || (s == searchText)),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Action<Exception>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(imageInfos);
        }

        public static void WithInitialEmptyImage(this AutoMock autoMock)
        {
            var imageInfoWithBitmap = autoMock.Create<ImageInfoWithBitmap>();
            imageInfoWithBitmap.ImageInfo = autoMock.Create<ImageInfo>();
            SetupImageInfo(imageInfoWithBitmap.ImageInfo);
            WithInitialImage(autoMock, customImage: imageInfoWithBitmap);
        }

        public static void WithInitialImage(
            this AutoMock autoMock,
            int searchIndex = 0,
            bool isAlternate = false,
            IReadOnlyCollection<int?>? notAvailableIndexes = null,
            ImageInfoWithBitmap? customImage = null)
        {
            var key = CreateMockKey(autoMock);
            autoMock.Mock<IWordImageInfoRepository>()
                .Setup(x => x.TryGetById(key))
                .Returns(new WordImageInfo(key, customImage ?? CreateImageInfoWithBitmap(autoMock), notAvailableIndexes ?? new int?[2]));
            autoMock.Mock<IWordImageSearchIndexRepository>().Setup(x => x.TryGetById(key)).Returns(new WordImageSearchIndex(key, searchIndex, isAlternate));
        }

        public static void WithNoImageFound(this AutoMock autoMock, string? searchText = null)
        {
            WithImageSearchResult(autoMock, Array.Empty<ImageInfo>(), searchText);
        }

        public static void WithSuccessfulImageSearch(this AutoMock autoMock, string? searchText = null)
        {
            var imageInfo = autoMock.Create<ImageInfo>();
            SetupImageInfo(imageInfo);
            WithImageSearchResult(
                autoMock,
                new[]
                {
                    imageInfo
                },
                searchText);
        }

        static void SetupImageInfo(this ImageInfo imageInfo)
        {
            imageInfo.Name = "Name";
            imageInfo.Url = new Uri("http://hello.world");
            imageInfo.ThumbnailUrl = new Uri("http://hello.world.thumbnail");
        }
    }
}
