// MainForm.LegacyShims.cs
namespace TruckModImporter
{
    public partial class MainForm
    {
        // Kompatibilitäts-Shim: alte Aufrufer von GetProfilesRootDir()
        // leiten wir auf die neue, zentrale Methode um.
        private string GetProfilesRootDir() => ResolveProfilesRootDir_Fix();

        // WICHTIG:
        // KEINE weiteren Methoden hier definieren – insbesondere
        // kein DoOpenSelectedProfileFolder_Local() o.ä.,
        // sonst gibt's wieder CS0111 (Duplikat).
    }
}
