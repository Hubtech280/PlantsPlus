using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PlantsPlus.Core
{
    // No Unity dependency: favorites are IDs, not localized names or skin IDs.
    internal sealed class FavoriteStore
    {
        private readonly string path;
        private HashSet<int> ids = new();
        internal FavoriteStore(string path)
        {
            this.path = path;
            if (!File.Exists(path)) return;
            foreach (string line in File.ReadLines(path))
                if (int.TryParse(line.Trim(), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int id) && id >= 0)
                    ids.Add(id);
        }

        internal bool Contains(int id) => ids.Contains(id);

        internal bool Toggle(int id)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            var next = new HashSet<int>(ids);
            bool selected = next.Add(id);
            if (!selected) next.Remove(id);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            string temporary = path + ".tmp";
            // Replace only after the complete file has been written. A failed
            // save leaves both the previous file and in-memory choices intact.
            File.WriteAllLines(temporary, next.OrderBy(value => value)
                .Select(value => value.ToString(CultureInfo.InvariantCulture)));
            File.Move(temporary, path, true);
            ids = next;
            return selected;
        }
    }
}
