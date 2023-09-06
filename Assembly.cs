using System.Resources;
using System.Runtime.CompilerServices;

// Attributes are needed for startup and for installer
[assembly: InternalsVisibleTo(nameof(Mémoire) + "." + "Test")]
[assembly: NeutralResourcesLanguage("en")]
