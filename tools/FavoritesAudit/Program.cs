using PlantsPlus.Core;
using System.Reflection;
using System.Security.Cryptography;

// Exercises the actual persistence class without Unity or native game calls.
string fixture = Path.Combine(Path.GetTempPath(), "PlantsPlus-FavoritesAudit-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(fixture);
string path = Path.Combine(fixture, "favorites.txt");
void Check(bool valid, string message)
{
    if (!valid) throw new Exception(message);
}
try
{
    var store = new FavoriteStore(path);
    Check(!store.Contains(1), "First run must be empty.");
    Check(store.Toggle(1), "Vanilla plant selection failed.");
    Check(store.Toggle(6041), "Mod plant selection failed.");
    var reloaded = new FavoriteStore(path);
    Check(reloaded.Contains(1) && reloaded.Contains(6041), "Choices did not survive reload.");
    Check(!reloaded.Toggle(1) && !reloaded.Contains(1), "Unfavorite failed.");
    Check(new FavoriteStore(path).Contains(6041), "Removing vanilla plant removed mod plant.");
    Check(!reloaded.Toggle(6041), "Last favorite removal failed.");
    Check(!new FavoriteStore(path).Contains(6041), "Empty list did not persist.");
    File.WriteAllLines(path, new[] { "6041", "6041", "not-an-id", "-4", " 1 " });
    store = new FavoriteStore(path);
    Check(store.Contains(6041) && store.Contains(1) && !store.Contains(-4), "Malformed-line handling failed.");
    bool rejected = false;
    try { store.Toggle(-4); } catch (ArgumentOutOfRangeException) { rejected = true; }
    Check(rejected, "Negative ID accepted.");
    string before = File.ReadAllText(path);
    Directory.CreateDirectory(path + ".tmp"); // Force write failure, portably.
    bool failed = false;
    try { store.Toggle(6041); }
    catch (IOException) { failed = true; }
    catch (UnauthorizedAccessException) { failed = true; }
    Check(failed && store.Contains(6041) && File.ReadAllText(path) == before,
        "Failed save must preserve memory and original file.");
    Directory.Delete(path + ".tmp");
    Console.WriteLine("PASS: select/unselect, vanilla + mod IDs, restart, empty list, malformed lines, failed-save safety.");

    if (args.Length >= 2)
    {
        Assembly assembly = Assembly.LoadFile(Path.GetFullPath(args[0]));
        foreach (string state in new[] { "Selected", "Unselected" })
        {
            using Stream resource = assembly.GetManifestResourceStream(
                "PlantsPlus.Resources.UI.favorite_" + state.ToLowerInvariant() + ".png")!;
            using Stream original = File.OpenRead(Path.Combine(args[1], "Favorite " + state + ".png"));
            using var hash = SHA256.Create();
            Check(resource != null && hash.ComputeHash(resource).SequenceEqual(hash.ComputeHash(original)),
                "Supplied heart differs from embedded asset: " + state);
        }
        Console.WriteLine("PASS: both embedded hearts match the supplied PNGs byte for byte.");
    }
}
finally
{
    // Only exact files generated in this uniquely named fixture are removed.
    if (File.Exists(path)) File.Delete(path);
    if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
    if (Directory.Exists(path + ".tmp")) Directory.Delete(path + ".tmp");
    Directory.Delete(fixture);
}
