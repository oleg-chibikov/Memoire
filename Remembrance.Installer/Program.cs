using System;
using Scar.Common.Installer;

namespace Remembrance.Installer
{
    internal static class Program
    {
        private const string BuildDir = "..\\Build";

        private const string ProductIcon = "Icon.ico";

        private static readonly Guid UpgradeCode = new Guid("a235657a-58d6-4239-9428-9d0f8840a45b");

        private static void Main()
        {
            new InstallBuilder(nameof(Remembrance), nameof(Scar), BuildDir, UpgradeCode).WithIcon(ProductIcon)
                .WithDesktopShortcut()
                .WithProgramMenuShortcut()
                .WithAutostart()
                .OpenFolderAfterInstallation()
                .LaunchAfterInstallation()
                .WithProcessTermination()
                .Build(wixBinariesLocation: @"..\packages\WixSharp.wix.bin.3.11.2\tools\bin");
        }
    }
}
