using System;
using System.Collections.Generic;

namespace Application.Domain.Model.Students
{
    public static class StudentStatus
    {
        public const string Active = "active";
        public const string Suspended = "suspended";
        public const string NotCurrently = "not_currently";

        public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
        {
            Active,
            Suspended,
            NotCurrently
        };
    }
}
