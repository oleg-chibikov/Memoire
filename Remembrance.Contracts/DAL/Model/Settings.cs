using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class Settings : TrackedEntity<string>
    {
        public string ValueJson { get; set; }
    }
}