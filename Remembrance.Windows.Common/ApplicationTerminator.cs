using Remembrance.Contracts;
using System.Windows;

namespace Remembrance.Windows.Common
{
    internal class ApplicationTerminator : IApplicationTerminator
    {
        public void Terminate()
        {
            Application.Current.Shutdown();
        }
    }
}
