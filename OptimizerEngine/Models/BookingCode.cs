using System;
using System.Collections.Generic;

namespace OptimizerEngine.Models
{
    public partial class BookingCode
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public bool InstructorSelectable { get; set; }
        public bool SupervisorSelectable { get; set; }
        public bool RestrictCalendarView { get; set; }
        public bool DeleteFlag { get; set; }
        public int CategoryId { get; set; }
        public bool? OptimizerIgnorable { get; set; }
    }
}
