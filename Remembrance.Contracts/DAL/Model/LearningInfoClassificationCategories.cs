using System.Collections.Generic;
using Remembrance.Contracts.Classification.Data;

namespace Remembrance.Contracts.DAL.Model
{
    public class LearningInfoClassificationCategories
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public IReadOnlyCollection<ClassificationCategory> Items { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public decimal MinMatchThreshold { get; set; }
    }
}
