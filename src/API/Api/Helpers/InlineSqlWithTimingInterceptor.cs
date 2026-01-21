using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Api.Helpers;

public class InlineSqlWithTimingInterceptor(ILogger<InlineSqlWithTimingInterceptor> logger) : DbCommandInterceptor
{
    private readonly ConcurrentDictionary<Guid, Stopwatch> _timers = new();

    private void StartTimer(CommandEventData eventData)
    {
        var sw = new Stopwatch();
        _timers[eventData.CommandId] = sw;
        sw.Start();
    }

    private void Log(DbCommand command, CommandExecutedEventData eventData)
    {
        if (!_timers.TryRemove(eventData.CommandId, out var sw))
        {
            return;
        }

        sw.Stop();

        var inlineSql = InlineParameters(command);

        logger.LogInformation(
            "EF SQL ({Elapsed} ms)\n{Sql}",
            sw.ElapsedMilliseconds,
            inlineSql
        );
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        StartTimer(eventData);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        StartTimer(eventData);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Log(command, eventData);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        Log(command, eventData);
        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    private static string InlineParameters(DbCommand cmd)
    {
        string sql = cmd.CommandText;

        foreach (DbParameter p in cmd.Parameters)
        {
            string literal = ConvertToSqlLiteral(p.Value);
            sql = sql.Replace(p.ParameterName, literal);
        }

        return sql;
    }

    private static string ConvertToSqlLiteral(object value)
    {
        if (value == null || value == DBNull.Value)
        {
            return "NULL";
        }

        return value switch
        {
            string s => $"'{s.Replace("'", "''")}'",
            bool b => b ? "1" : "0",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'",
            DateOnly d => $"'{d:yyyy-MM-dd}'",
            TimeOnly t => $"'{t:HH:mm:ss.fff}'",
            Guid g => $"'{g}'",
            byte[] bytes => $"0x{BitConverter.ToString(bytes).Replace("-", "")}",
            _ => value.ToString() ?? "NULL"
        };
    }
}
