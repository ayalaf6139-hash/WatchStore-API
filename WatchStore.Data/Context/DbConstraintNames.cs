namespace WatchStore.Data.Context
{
    public static class DbConstraintNames
    {
        public const string AppointmentNoOverlap = "EX_Appointments_NoOverlap";

        public const string AppointmentReservedRange = "CK_Appointments_ReservedRange";

        public const string AppointmentDurationPositive = "CK_Appointments_DurationPositive";

        public const string AppointmentIdempotencyKey = "IX_Appointments_BusinessId_IdempotencyKey";

        public const string CustomerPhone = "IX_Customers_BusinessId_PhoneNumber";

        public const string ConversationMessageSequence = "IX_ConversationMessages_ConversationId_SequenceNumber";

        public const string OpeningHoursRange = "CK_OpeningHours_Range";

        public const string BlockedPeriodRange = "CK_BlockedPeriods_Range";

        public const string AppointmentTypeDurations = "CK_AppointmentTypes_Durations";
    }
}
