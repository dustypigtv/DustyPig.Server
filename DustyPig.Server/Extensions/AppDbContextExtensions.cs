using DustyPig.Server.Data;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.Extensions;

internal static class AppDbContextExtensions
{
    public static string GetTableName<TEntity>(this DbContext context) where TEntity : class
    {
        IEntityType entityType = context.Model.FindEntityType(typeof(TEntity));
        return entityType.GetTableName()!;
    }

    public static async Task<DbConnection> GetOpenDbConnection(this DbContext db, CancellationToken cancellationToken)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);
        return conn;
    }



    /// <summary>
    /// This calls <see cref="DbContext.SaveChangesAsync(bool, CancellationToken)"/> on <paramref name="db"/>
    /// </summary>
    public static async Task MarkPlaylistArtworkNeedsupdate(this AppDbContext db, List<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0)
            return;

        var playlists = await db.Playlists
            .Where(_ => ids.Contains(_.Id))
            .ToListAsync(cancellationToken);

        if (playlists.Count == 0)
            return;

        playlists.ForEach(_ => _.ArtworkUpdateNeeded = true);
        await db.SaveChangesAsync(cancellationToken);
    }
}
