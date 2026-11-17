using WatchStore.Core.DTOs;
using WatchStore.Core.Entities;

namespace WatchStore.Service.Scheduling
{
    public static class AppointmentMapper
    {
        public static AppointmentResponse ToResponse(
            Appointment appointment,
            Customer customer,
            AppointmentType appointmentType,
            TimeZoneInfo timeZone)
        {
            return new AppointmentResponse
            {
                Id = appointment.Id,
                CustomerId = appointment.CustomerId,
                CustomerName = customer.FullName,
                CustomerPhoneNumber = customer.PhoneNumber,
                AppointmentTypeId = appointment.AppointmentTypeId,
                AppointmentTypeName = appointmentType.Name,
                StartUtc = appointment.StartUtc,
                EndUtc = appointment.EndUtc,
                StartLocal = BusinessTime.ToLocal(appointment.StartUtc, timeZone),
                EndLocal = BusinessTime.ToLocal(appointment.EndUtc, timeZone),
                DurationMinutes = appointment.DurationMinutes,
                Status = appointment.Status,
                ConversationId = appointment.ConversationId,
                CustomerNotes = appointment.CustomerNotes,
                PreparationNotes = appointment.PreparationNotes,
                CreatedAtUtc = appointment.CreatedAtUtc,
                CreatedBy = appointment.CreatedBy
            };
        }

        public static AppointmentResponse ToResponse(Appointment appointment, TimeZoneInfo timeZone)
        {
            return ToResponse(appointment, appointment.Customer, appointment.AppointmentType, timeZone);
        }

        public static AvailableSlotResponse ToSlotResponse(CandidateSlot slot, TimeZoneInfo timeZone)
        {
            return new AvailableSlotResponse
            {
                StartUtc = slot.StartUtc,
                EndUtc = slot.EndUtc,
                StartLocal = BusinessTime.ToLocal(slot.StartUtc, timeZone),
                EndLocal = BusinessTime.ToLocal(slot.EndUtc, timeZone),
                DurationMinutes = (int)(slot.EndUtc - slot.StartUtc).TotalMinutes
            };
        }
    }
}
