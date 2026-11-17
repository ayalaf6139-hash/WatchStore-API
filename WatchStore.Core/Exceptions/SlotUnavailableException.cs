using WatchStore.Core.DTOs;

namespace WatchStore.Core.Exceptions
{
    public class SlotUnavailableException : ConflictException
    {
        public SlotUnavailableException(string message, IReadOnlyList<AvailableSlotResponse> alternatives)
            : base(message)
        {
            Alternatives = alternatives;
        }

        public IReadOnlyList<AvailableSlotResponse> Alternatives { get; }
    }
}
