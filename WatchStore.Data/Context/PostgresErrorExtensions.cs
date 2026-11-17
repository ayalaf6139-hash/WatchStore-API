using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace WatchStore.Data.Context
{
    public static class PostgresErrorExtensions
    {
        public static bool IsUniqueViolation(this DbUpdateException exception, string constraintName)
        {
            return IsViolation(exception, PostgresErrorCodes.UniqueViolation, constraintName);
        }

        public static bool IsExclusionViolation(this DbUpdateException exception, string constraintName)
        {
            return IsViolation(exception, PostgresErrorCodes.ExclusionViolation, constraintName);
        }

        private static bool IsViolation(DbUpdateException exception, string sqlState, string constraintName)
        {
            return exception.InnerException is PostgresException postgres &&
                   postgres.SqlState == sqlState &&
                   postgres.ConstraintName == constraintName;
        }
    }
}
