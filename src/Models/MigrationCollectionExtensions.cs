#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using SqliteMultiTenant.Constants;

namespace SqliteMultiTenant.Models
{
    /// <summary>
    /// Extension methods for collections of <see cref="Migration"/>.
    /// </summary>
    public static class MigrationCollectionExtensions
    {
        /// <summary>
        /// Returns the migrations ordered by their <c>Version</c> property.
        /// </summary>
        /// <param name="migrations">The source collection of migrations.</param>
        /// <returns>An <see cref="IEnumerable{Migration}"/> ordered by <c>Version</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="migrations"/> is <c>null</c>.</exception>
        public static IEnumerable<Migration> OrderByVersion(this IEnumerable<Migration> migrations)
        {
            if (migrations == null) throw new ArgumentNullException(nameof(migrations));
            // Ordinal comparison is used to keep the ordering deterministic.
            return migrations.OrderBy(m => m.Version, StringComparer.Ordinal);
        }

        /// <summary>
        /// Returns the pending migrations that have a version greater than the supplied version.
        /// </summary>
        /// <param name="migrations">The source collection of migrations.</param>
        /// <param name="version">The version to compare against.</param>
        /// <returns>
        /// An <see cref="IEnumerable{Migration}"/> containing migrations whose <c>Status</c> is
        /// <see cref="MigrationStatus.Pending"/> and whose <c>Version</c> is greater than <paramref name="version"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="migrations"/> or <paramref name="version"/> is <c>null</c>.</exception>
        public static IEnumerable<Migration> PendingAfter(this IEnumerable<Migration> migrations, string version)
        {
            if (migrations == null) throw new ArgumentNullException(nameof(migrations));
            if (version == null) throw new ArgumentNullException(nameof(version));

            return migrations.Where(m =>
                m.Status == MigrationStatus.Pending &&
                string.Compare(m.Version, version, StringComparison.Ordinal) > 0);
        }

        /// <summary>
        /// Retrieves the most recent migration that has been applied (i.e., has a <c>Status</c> of <see cref="MigrationStatus.Completed"/>).
        /// </summary>
        /// <param name="migrations">The source collection of migrations.</param>
        /// <returns>
        /// The latest applied <see cref="Migration"/> based on the highest <c>Version</c>, or <c>null</c> if no such migration exists.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="migrations"/> is <c>null</c>.</exception>
        public static Migration? LatestApplied(this IEnumerable<Migration> migrations)
        {
            if (migrations == null) throw new ArgumentNullException(nameof(migrations));

            return migrations
                .Where(m => m.Status == MigrationStatus.Completed)
                .OrderByDescending(m => m.Version, StringComparer.Ordinal)
                .FirstOrDefault();
        }
    }
}
