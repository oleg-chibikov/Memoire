namespace Remembrance.Contracts.Classification.Data
{
    public class ClassificationCategory
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public string ClassName { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public decimal Match { get; set; }
    }
}
