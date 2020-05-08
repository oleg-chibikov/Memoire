using System;
using System.IO;
using Scar.Common.Installer;

namespace Mémoire.Installer
{
    static class Program
    {
        const string BuildDir = "..\\Build";
        const string ProductIcon = "Icon.ico";
        static readonly Guid UpgradeCode = new Guid("a235657a-58d6-4239-9428-9d0f8840a45b");

        static void Main()
        {
            new InstallBuilder(nameof(Mémoire), nameof(Scar), BuildDir, UpgradeCode).WithIcon(ProductIcon)
                .WithDesktopShortcut()
                .WithProgramMenuShortcut()
                .WithAutostart()
                .OpenFolderAfterInstallation()
                .LaunchAfterInstallation()
                .WithProcessTermination()
                .Build(wixBinariesLocation: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages", "WixSharp.wix.bin", "3.11.2", "tools", "bin"));
        }
    }
}
