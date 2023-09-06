using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Scar.Common.Async;
using Scar.Services.Contracts;
using Scar.Services.Contracts.Data.ImageSearch;

namespace Mémoire.Test
{
    [TestFixture]
    [Parallelizable]
    [Apartment(ApartmentState.STA)]
    public sealed class WordImageViewerViewModelTests
    {
        [Test]
        public async Task IsLoadingIsTrueDuringProcessingAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            using var semaphore = new SemaphoreSlim(0, 1);
            var runningTask = Task.Run(async () => await semaphore.WaitAsync().ConfigureAwait(false));
            autoMock.Mock<ICancellationTokenSourceProvider>().Setup(x => x.ExecuteOperationAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<bool>())).Returns(runningTask);

            // Act
            var sut = await autoMock.CreateViewModelAsync(false, false).ConfigureAwait(false);

            // Assert
            Assert.That(sut.IsLoading, Is.True);
            semaphore.Release();
            await runningTask.ConfigureAwait(false);
        }

        [Test]
        public async Task IsLoadingIsFalseAfterProcessingAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.WithInitialEmptyImage();
            autoMock.WithSuccessfulImageSearch();

            // Act
            var sut = await autoMock.CreateViewModelAsync(false, false).ConfigureAwait(false);

            // Assert
            Assert.That(sut.IsLoading, Is.False);
        }

        [Test]
        public async Task ReloadIsNotVisibleDuringProcessingAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            using var semaphore = new SemaphoreSlim(0, 1);
            var runningTask = Task.Run(async () => await semaphore.WaitAsync().ConfigureAwait(false));
            autoMock.Mock<ICancellationTokenSourceProvider>().Setup(x => x.ExecuteOperationAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<bool>())).Returns(runningTask);

            // Act
            var sut = await autoMock.CreateViewModelAsync(false, false).ConfigureAwait(false);

            // Assert
            Assert.That(sut.IsReloadVisible, Is.False);
            semaphore.Release();
            await runningTask.ConfigureAwait(false);
        }

        [Test]
        public async Task PerformsReloadAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.WithInitialEmptyImage();
            autoMock.WithSuccessfulImageSearch();
            var sut = await autoMock.CreateViewModelAsync().ConfigureAwait(false);

            // Act
            await sut.ReloadImageAsync().ConfigureAwait(false);

            // Assert
            autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.DefaultSearchText);
            autoMock.VerifyImagesSearch(Times.Never, WordImageViewerViewModelTestsExtensions.FallbackSearchText);
            autoMock.VerifyDownloadCount(Times.Once);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
            Assert.That(sut.IsReloadVisible, Is.False);
        }

        [Test]
        public void When_Canceled_Throws()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.Mock<ICancellationTokenSourceProvider>().Setup(x => x.ExecuteOperationAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<bool>())).Throws(new OperationCanceledException());

            // Assert
            Assert.That(
                async () =>
                {
                    // Act
                    await autoMock.CreateViewModelAsync().ConfigureAwait(false);
                },
                Throws.Exception.TypeOf<OperationCanceledException>());
        }

        [Test]
        public async Task When_DownloadsEmptyImage_ShouldTrySearchWithDifferentTextAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.Mock<IImageDownloader>()
                .SetupSequence(x => x.DownloadImageAsync(It.IsAny<Uri>(), It.IsAny<Action<Exception>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(null)
                .ReturnsAsync(new byte[1]);
            autoMock.WithSuccessfulImageSearch();

            // Act
            var sut = await autoMock.CreateViewModelAsync(false).ConfigureAwait(false);

            // Assert
            autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.DefaultSearchText);
            autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.FallbackSearchText);
            autoMock.VerifyDownloadCount(() => Times.Exactly(2));
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.True);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        public async Task When_ErrorOccursDuringSearch_StopsSearchAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.WithImageSearchError();

            // Act
            var sut = await autoMock.CreateViewModelAsync(false).ConfigureAwait(false);

            // Assert
            autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.DefaultSearchText);
            autoMock.VerifyImagesSearch(Times.Never, WordImageViewerViewModelTestsExtensions.FallbackSearchText);
            autoMock.VerifyDownloadCount(Times.Never);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Null);
        }

        [Test]
        public async Task When_GoingBackwards_AlternatesSearchTextForEveryRequestAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.WithInitialImage(2);
            autoMock.WithSuccessfulImageSearch();
            var sut = await autoMock.CreateViewModelAsync().ConfigureAwait(false);

            for (var i = 1; i >= 0; i--)
            {
                // Act
                autoMock.Reset();
                await sut.SetPreviousImageAsync().ConfigureAwait(false);

                // Assert
                autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.FallbackSearchText);
                Assert.That(sut.SearchIndex, Is.EqualTo(i));
                Assert.That(sut.IsAlternate, Is.True);
                Assert.That(sut.ThumbnailBytes, Is.Not.Null);

                // Act
                autoMock.Reset();
                await sut.SetPreviousImageAsync().ConfigureAwait(false);

                // Assert
                autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.DefaultSearchText);
                Assert.That(sut.SearchIndex, Is.EqualTo(i));
                Assert.That(sut.IsAlternate, Is.False);
                Assert.That(sut.ThumbnailBytes, Is.Not.Null);
            }
        }

        [Test]
        public async Task When_GoingForwards_AlternatesSearchTextForEveryRequestAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.WithInitialImage(0, true);
            autoMock.WithSuccessfulImageSearch();
            var sut = await autoMock.CreateViewModelAsync().ConfigureAwait(false);

            for (var i = 1; i <= 2; i++)
            {
                // Act
                autoMock.Reset();
                await sut.SetNextImageAsync().ConfigureAwait(false);

                // Assert
                autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.DefaultSearchText);
                Assert.That(sut.SearchIndex, Is.EqualTo(i));
                Assert.That(sut.IsAlternate, Is.False);
                Assert.That(sut.ThumbnailBytes, Is.Not.Null);

                // Act
                autoMock.Reset();
                await sut.SetNextImageAsync().ConfigureAwait(false);

                // Assert
                autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.FallbackSearchText);
                Assert.That(sut.SearchIndex, Is.EqualTo(i));
                Assert.That(sut.IsAlternate, Is.True);
                Assert.That(sut.ThumbnailBytes, Is.Not.Null);
            }
        }

        [Test]
        public async Task When_ImageBytesAreNull_Then_ShouldRepeatSearchInitiallyAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.WithInitialEmptyImage();
            autoMock.WithSuccessfulImageSearch();

            // Act
            var sut = await autoMock.CreateViewModelAsync(false).ConfigureAwait(false);

            // Assert
            autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.DefaultSearchText);
            autoMock.VerifyImagesSearch(Times.Never, WordImageViewerViewModelTestsExtensions.FallbackSearchText);
            autoMock.VerifyDownloadCount(Times.Once);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        public async Task When_ImageIsEmpty_ReloadIsVisibleAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.WithInitialEmptyImage();

            // Act
            var sut = await autoMock.CreateViewModelAsync().ConfigureAwait(false);

            // Assert
            Assert.That(sut.ThumbnailBytes, Is.Null);
            Assert.That(sut.IsReloadVisible, Is.True);
        }

        [Test]
        public async Task When_IndexIsZero_And_IsAlternate_PreviousSearchShouldUseDefaultSearchTextAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.WithInitialImage(0, true);
            autoMock.WithSuccessfulImageSearch();
            var sut = await autoMock.CreateViewModelAsync().ConfigureAwait(false);

            // Act
            await sut.SetPreviousImageAsync().ConfigureAwait(false);

            // Assert
            autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.DefaultSearchText);
            autoMock.VerifyImagesSearch(Times.Never, WordImageViewerViewModelTestsExtensions.FallbackSearchText);
            autoMock.VerifyDownloadCount(Times.Once);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        public async Task When_IndexIsZero_AndNot_IsAlternate_PreviousButton_Should_DoNothingAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.WithInitialImage();
            autoMock.WithSuccessfulImageSearch();
            var sut = await autoMock.CreateViewModelAsync().ConfigureAwait(false);

            // Act
            await sut.SetPreviousImageAsync().ConfigureAwait(false);

            // Assert
            autoMock.VerifyImagesSearch(Times.Never);
            autoMock.VerifyDownloadCount(Times.Never);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        public async Task When_InitialImageIsEmpty_PerformsSearchWithDefaultSearchTextAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.WithSuccessfulImageSearch();

            // Act
            var sut = await autoMock.CreateViewModelAsync(false).ConfigureAwait(false);

            // Assert
            autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.DefaultSearchText);
            autoMock.VerifyImagesSearch(Times.Never, WordImageViewerViewModelTestsExtensions.FallbackSearchText);
            autoMock.VerifyDownloadCount(Times.Once);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        public async Task When_IsInitializedWithImage_NoSearchAndDownloadOccursAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.WithInitialImage();
            autoMock.WithSuccessfulImageSearch();

            // Act
            var sut = await autoMock.CreateViewModelAsync().ConfigureAwait(false);

            // Assert
            autoMock.VerifyImagesSearch(Times.Never);
            autoMock.VerifyDownloadCount(Times.Never);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        public async Task When_MoreThanOneImageIsReturned_ThrowsAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.WithInitialImage();
            autoMock.WithImageSearchResult(new ImageInfo[2]);
            var sut = await autoMock.CreateViewModelAsync().ConfigureAwait(false);

            // Act
            // Assert
            Assert.That(() => sut.SetNextImageAsync(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public async Task When_NoImagesAvailableForBothSearchTexts_PerformsTwoAttemptsWithDifferentSearchTextsAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.WithNoImageFound();

            // Act
            var sut = await autoMock.CreateViewModelAsync(false).ConfigureAwait(false);

            // Assert
            autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.DefaultSearchText);
            autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.FallbackSearchText);
            autoMock.VerifyDownloadCount(Times.Never);
            autoMock.VerifyMessage(Times.Once);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.True);
            Assert.That(sut.ThumbnailBytes, Is.Null);
        }

        [Test]
        public async Task When_OnlyDefaultSearchTextReturnsResults_IndexIncreasesEveryTimeAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.WithSuccessfulImageSearch(WordImageViewerViewModelTestsExtensions.DefaultSearchText);
            autoMock.WithNoImageFound(WordImageViewerViewModelTestsExtensions.FallbackSearchText);

            var sut = await autoMock.CreateViewModelAsync().ConfigureAwait(false); // this should search using only DefaultSearchText

            // Act
            await sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.DefaultSearchText);
            autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.FallbackSearchText);
            autoMock.VerifyDownloadCount(Times.Once);

            autoMock.Reset();

            // Act
            await sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.DefaultSearchText);
            autoMock.VerifyImagesSearch(Times.Never, WordImageViewerViewModelTestsExtensions.FallbackSearchText);
            autoMock.VerifyDownloadCount(Times.Once);
            Assert.That(sut.SearchIndex, Is.EqualTo(2));
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        public async Task When_OnlyFallbackSearchTextReturnsResults_IndexIncreasesEveryTimeAsync()
        {
            // Arrange
            using var autoMock = WordImageViewerViewModelTestsExtensions.CreateAutoMock();
            autoMock.WithSuccessfulImageSearch(WordImageViewerViewModelTestsExtensions.FallbackSearchText);
            autoMock.WithNoImageFound(WordImageViewerViewModelTestsExtensions.DefaultSearchText);

            var sut = await autoMock.CreateViewModelAsync().ConfigureAwait(false); // this should search using DefaultSearchText and then FallbackSearchText

            // Act
            await sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            autoMock.VerifyImagesSearch(Times.Never, WordImageViewerViewModelTestsExtensions.DefaultSearchText);
            autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.FallbackSearchText);
            autoMock.VerifyDownloadCount(Times.Once);

            // Act
            autoMock.Reset();
            await sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            autoMock.VerifyImagesSearch(Times.Never, WordImageViewerViewModelTestsExtensions.DefaultSearchText);
            autoMock.VerifyImagesSearch(Times.Once, WordImageViewerViewModelTestsExtensions.FallbackSearchText);
            autoMock.VerifyDownloadCount(Times.Once);
            Assert.That(sut.SearchIndex, Is.EqualTo(2));
            Assert.That(sut.IsAlternate, Is.True);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }
    }
}
