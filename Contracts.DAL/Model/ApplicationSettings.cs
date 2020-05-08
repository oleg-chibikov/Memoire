using Scar.Common.DAL.Contracts.Model;

namespace Mémoire.Contracts.DAL.Model
{
    public sealed class ApplicationSettings : TrackedEntity<string>
    {
        public string ValueJson { get; set; }
    }
}
