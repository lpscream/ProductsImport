using Microsoft.Data.SqlClient;
using ProductsImport.Models;

namespace ProductsImport.Data;

/// <summary>All access to the ass1/ass2/ass3/ass4/gru2/cls2/nds1 product catalog tables.</summary>
public class SqlServerRepository
{
    private readonly ConnectionProfile _profile;

    public SqlServerRepository(ConnectionProfile profile)
    {
        _profile = profile;
    }

    public static void TestConnection(ConnectionProfile profile)
    {
        using var connection = new SqlConnection(profile.BuildConnectionString());
        connection.Open();
    }

    public SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(_profile.BuildConnectionString());
        connection.Open();
        return connection;
    }

    public List<GroupInfo> GetGroups(SqlConnection connection)
    {
        var result = new List<GroupInfo>();
        using var command = new SqlCommand(
            "SELECT gru2001, gru2004 FROM gru2 WHERE gru2002 = 1 ORDER BY gru2004", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new GroupInfo
            {
                Code = reader.GetInt64(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
            });
        }

        return result;
    }

    public List<UnitInfo> GetUnits(SqlConnection connection)
    {
        var result = new List<UnitInfo>();
        using var command = new SqlCommand(
            "SELECT cls2001, cls2004 FROM cls2 WHERE cls2002 = 100 ORDER BY cls2004", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new UnitInfo
            {
                Id = reader.GetInt64(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
            });
        }

        return result;
    }

    public List<VatInfo> GetVatRates(SqlConnection connection)
    {
        var result = new List<VatInfo>();
        using var command = new SqlCommand(
            "SELECT nds1001, nds1003 FROM nds1 ORDER BY nds1003", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new VatInfo
            {
                Id = reader.GetInt64(0),
                RatePercent = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1)
            });
        }

        return result;
    }

    public HashSet<long> GetExistingArticleIds(SqlConnection connection)
    {
        var result = new HashSet<long>();
        using var command = new SqlCommand("SELECT ass1001 FROM ass1", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(reader.GetInt64(0));
        }

        return result;
    }

    /// <summary>Creates a new top-level group (gru2002 = 1) for a document group name that has no
    /// match in gru2 yet, and returns its new code.</summary>
    public long InsertGroup(SqlConnection connection, SqlTransaction transaction, string name)
    {
        using var maxCmd = new SqlCommand("SELECT ISNULL(MAX(gru2001), 0) + 1 FROM gru2", connection, transaction);
        var newCode = Convert.ToInt64(maxCmd.ExecuteScalar());

        using var insertCmd = new SqlCommand(
            "INSERT INTO gru2 (gru2001, gru2002, gru2003, gru2004, gru2005) VALUES (@code, 1, @code + 100, @name, 0)",
            connection, transaction);
        insertCmd.Parameters.AddWithValue("@code", newCode);
        insertCmd.Parameters.AddWithValue("@name", name);
        insertCmd.ExecuteNonQuery();

        return newCode;
    }

    public HashSet<string> GetExistingBarcodes(SqlConnection connection)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        using var command = new SqlCommand("SELECT ass3002 FROM ass3 WHERE ass3002 IS NOT NULL", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (!reader.IsDBNull(0))
            {
                result.Add(reader.GetString(0).Trim());
            }
        }

        return result;
    }

    /// <summary>Inserts one product across ass1/ass2/ass3/ass4, mirroring the legacy T-SQL import script.</summary>
    public void InsertProduct(SqlConnection connection, SqlTransaction transaction, ProductRecord record)
    {
        using (var cmd = new SqlCommand(
            "INSERT INTO ass1 (ass1001, ass1002, ass1003, ass1004, ass1005, ass1006, ass1007) " +
            "VALUES (@id, @id, @name, @vat, @unit, @weighted, @excise)", connection, transaction))
        {
            cmd.Parameters.AddWithValue("@id", record.ArticleId);
            cmd.Parameters.AddWithValue("@name", record.Name);
            cmd.Parameters.AddWithValue("@vat", record.VatId);
            cmd.Parameters.AddWithValue("@unit", record.UnitId);
            cmd.Parameters.AddWithValue("@weighted", record.Weighted ? 1 : 0);
            cmd.Parameters.AddWithValue("@excise", record.Excise ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new SqlCommand(
            "INSERT INTO ass2 (ass2001, ass2002, ass2003, ass2004, ass2005, ass2006, ass2007, ass2008, " +
            "ass2009, ass2010, ass2011, ass2012, ass2013, ass2014, ass2015, ass2016, ass2017, ass2018, " +
            "ass2019, ass2020, ass2021, ass2022, ass2023, ass2024, ass2025, ass2026, ass2027, ass2028, " +
            "ass2029, ass2030, ass2031, ass2032) " +
            "VALUES (@id, 0, 0, 0.0, 0.0, 0.0, @name, '', 0.0, @excise, 0, 0.0, 0.0, 0.0, 0, 0.0, 0, 0, " +
            "0.0, 0.0, 0, 0.0, 0, '', '', 1, 0.0, 0, 0.0, 0.0, @uktzed, 0)", connection, transaction))
        {
            cmd.Parameters.AddWithValue("@id", record.ArticleId);
            cmd.Parameters.AddWithValue("@name", record.Name);
            cmd.Parameters.AddWithValue("@excise", record.Excise ? 1 : 0);
            cmd.Parameters.AddWithValue("@uktzed", record.Uktzed);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new SqlCommand(
            "INSERT INTO ass3 (ass3001, ass3002, ass3003, ass3004, ass3005, ass3007) " +
            "VALUES (@id, @barcode, '', 1, 0, 0)", connection, transaction))
        {
            cmd.Parameters.AddWithValue("@id", record.ArticleId);
            cmd.Parameters.AddWithValue("@barcode", record.Barcode);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new SqlCommand(
            "INSERT INTO ass4 (ass4001, ass4002, ass4003) VALUES (@id, 1, @group)", connection, transaction))
        {
            cmd.Parameters.AddWithValue("@id", record.ArticleId);
            cmd.Parameters.AddWithValue("@group", record.GroupCode);
            cmd.ExecuteNonQuery();
        }
    }
}
