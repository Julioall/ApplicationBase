using System;
using System.Collections.Generic;

using Application.Domain.Model.Dtos.Todo;

using Application.Domain.Model.Todo;

namespace Application.Domain.Model.Dtos.Todo
{
    public class TodoRecurrenceDto
    {
        public TodoRecurrenceType Type { get; set; } = TodoRecurrenceType.None;
        public int Interval { get; set; } = 1;
        public DateTime? EndsOn { get; set; }
        public List<DayOfWeek> DaysOfWeek { get; set; } = new();
    }
}





