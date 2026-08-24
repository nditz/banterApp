namespace BanterApp.Api.Data;

/// <summary>
/// Supabase exposes <c>public</c> through PostgREST. Enable RLS on every public table
/// with no anon/authenticated policies so only the API role (table owner / BYPASSRLS) can read rows.
/// </summary>
internal static class PostgresPublicRls
{
    internal const string EnableAllPublicTables = """
        DO $$
        DECLARE
          r RECORD;
        BEGIN
          FOR r IN
            SELECT tablename
            FROM pg_catalog.pg_tables
            WHERE schemaname = 'public'
          LOOP
            EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', r.tablename);
          END LOOP;
        END
        $$;
        """;
}
