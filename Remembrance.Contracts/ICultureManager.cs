using System.Globalization;

namespace Remembrance.Contracts
{
    public interface ICultureManager
    {
        void ChangeCulture(CultureInfo cultureInfo);
    }
}
