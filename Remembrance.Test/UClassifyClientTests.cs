using System.Linq;
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
        public async Task ReturnsClassificationsForWord()
        {
            // Arrange
            using var sut = new UClassifyTopicsClient(Mock.Of<IMessageHub>());

            // Act
            var categories = await sut.GetCategoriesAsync("Genesis", null, default);

            // Assert
            Assert.That(categories.Count(), Is.GreaterThan(0));
        }
    }
}
