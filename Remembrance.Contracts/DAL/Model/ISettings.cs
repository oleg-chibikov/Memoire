using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public interface ISettings : IEntity<string>
    {
        object Value { get; set; }
    }
}