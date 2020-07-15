using System;
using System.IO;
using Scar.Common.Installer;

namespace Mémoire.Installer
{
    static class Program
    {
        // Requires .Net Framework 3.5.1 to be installed. Wix does not have to be installed
        const string ReleaseDir = "..\\Release";
        const string ProductIcon = "Mémoire.ico";
        static readonly Guid UpgradeCode = new Guid("a235657a-58d6-4239-9428-9d0f8840a45b");

        static void Main()
        {
            new InstallBuilder(nameof(Mémoire), nameof(Scar), ReleaseDir, UpgradeCode).WithIcon(ProductIcon)
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
