namespace DustyPig.AppHost;

internal static class PostgresContainerImageTags
{
    /// <remarks>docker.io</remarks>
    public const string Registry = "docker.io";

    /// <remarks>library/postgres</remarks>
    public const string Image = "library/postgres";

    /// <remarks>17.6</remarks>
    public const string Tag = "17.6";

    /// <remarks>docker.io</remarks>
    public const string PgAdminRegistry = "docker.io";

    /// <remarks>dpage/pgadmin4</remarks>
    public const string PgAdminImage = "dpage/pgadmin4";

    /// <remarks>9.9.0</remarks>
    public const string PgAdminTag = "9.9.0";

    /// <remarks>docker.io</remarks>
    public const string PgWebRegistry = "docker.io";

    /// <remarks>sosedoff/pgweb</remarks>
    public const string PgWebImage = "sosedoff/pgweb";

    /// <remarks>0.16.2</remarks>
    public const string PgWebTag = "0.16.2";
}
