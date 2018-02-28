using System;
using Scar.Common.Installer;

namespace Remembrance.Installer
{
    internal static class Program
    {
        private const string BuildDir = "Build";

        private const string ProductIcon = "Icon.ico";

        private static readonly Guid UpgradeCode = new Guid("a235657a-58d6-4239-9428-9d0f8840a45b");

        private static void Main()
        {
            // TODO: Stop previous before installing new version.
            var fileName = $"{nameof(Remembrance)}.exe";
            new InstallBuilder(nameof(Remembrance), nameof(Scar), BuildDir, UpgradeCode).WithIcon(ProductIcon)
                .WithShortcut(fileName)
                .WithAutostart(fileName)
                .OpenFolderAfterInstallation()
                .LaunchAfterInstallation(fileName)
                .WithProcessTermination(fileName)
                .Build();
        }
    }
}