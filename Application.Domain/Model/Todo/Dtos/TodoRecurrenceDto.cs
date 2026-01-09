using System;
using System.Collections.Generic;

namespace Application.Domain.Model.Todo.Dtos
{
    public class TodoRecurrenceDto
    {
        public TodoRecurrenceType Type { get; set; } = TodoRecurrenceType.None;
        public int Interval { get; set; } = 1;
        public DateTime? EndsOn { get; set; }
        public List<DayOfWeek> DaysOfWeek { get; set; } = new();
    }
}
