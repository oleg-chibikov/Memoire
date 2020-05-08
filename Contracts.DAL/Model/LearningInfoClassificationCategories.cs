using System.Collections.Generic;
using Scar.Services.Contracts.Data.Classification;

namespace Mémoire.Contracts.DAL.Model
{
    public class LearningInfoClassificationCategories
    {
        public IEnumerable<ClassificationCategory> Items { get; set; }

        public double MinMatchThreshold { get; set; }
    }
}
