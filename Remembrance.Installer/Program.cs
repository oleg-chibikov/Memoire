using System;
using Scar.Common.Installer;

namespace Remembrance.Installer
{
    internal static class Program
    {
        private static void Main()
        {
            //TODO: Stop previous before installing new version.
            const string buildDir = "Build";
            var upgradeCode = new Guid("a235657a-58d6-4239-9428-9d0f8840a45b");
            const string productIcon = "Icon.ico";
            var fileName = $"{nameof(Remembrance)}.exe";
            new InstallBuilder(nameof(Remembrance),
                    nameof(Scar),
                    buildDir,
                    upgradeCode)
                .WithIcon(productIcon)
                .WithShortcut(fileName)
                .WithAutostart(fileName)
                .OpenFolderAfterInstallation()
                .LaunchAfterInstallation(fileName)
                .WithProcessTermination(fileName)
                .Build();
        }
    }
}