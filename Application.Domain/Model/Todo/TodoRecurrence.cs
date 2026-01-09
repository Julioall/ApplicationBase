using System;
using System.Collections.Generic;

namespace Application.Domain.Model.Todo
{
    public enum TodoRecurrenceType
    {
        None = 0,
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Yearly = 4,
        Weekdays = 5
    }

    public class TodoRecurrence
    {
        public TodoRecurrenceType Type { get; set; } = TodoRecurrenceType.None;
        public int Interval { get; set; } = 1;
        public DateTime? EndsOn { get; set; }
        public List<DayOfWeek> DaysOfWeek { get; set; } = new();
    }
}
