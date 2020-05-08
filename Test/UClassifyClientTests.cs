using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Easy.MessageHub;
using Moq;
using NUnit.Framework;
using Remembrance.Core.Classification;

namespace Remembrance.Test
{
    class UClassifyClientTests
    {
        [Test]
        public async Task ReturnsClassificationsForWordAsync()
        {
            // Arrange
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.uclassify.com/v1/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", UClassifyTopicsClient.Token);
            var sut = new UClassifyTopicsClient(Mock.Of<IMessageHub>(), httpClient);

            // Act
            var categories = await sut.GetCategoriesAsync("Genesis", null, default).ConfigureAwait(false);

            // Assert
            Assert.That(categories.Count(), Is.GreaterThan(0));
        }
    }
}
