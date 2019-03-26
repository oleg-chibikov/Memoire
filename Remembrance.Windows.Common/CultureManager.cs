using System.Globalization;
using JetBrains.Annotations;
using Remembrance.Contracts;
using Scar.Common.WPF.Localization;

namespace Remembrance.Windows.Common
{
    class CultureManager : ICultureManager
    {
        public void ChangeCulture([NotNull] CultureInfo cultureInfo)
        {
            CultureUtilities.ChangeCulture(cultureInfo);
        }
    }
}