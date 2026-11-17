namespace WatchStore.Data.Context
{
    public static class AppointmentOverlapConstraint
    {
        public const string CreateSql =
            "ALTER TABLE \"Appointments\" ADD CONSTRAINT \"" + DbConstraintNames.AppointmentNoOverlap + "\" " +
            "EXCLUDE USING gist (" +
            "\"AppointmentResourceId\" WITH =, " +
            "tstzrange(\"ReservedStartUtc\", \"ReservedEndUtc\", '[)') WITH &&) " +
            "WHERE (\"Status\" = 'Booked');";

        public const string DropSql =
            "ALTER TABLE \"Appointments\" DROP CONSTRAINT IF EXISTS \"" + DbConstraintNames.AppointmentNoOverlap + "\";";
    }
}
