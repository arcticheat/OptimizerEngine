using System;
using System.Collections.Generic;

namespace OptimizerEngine.Models
{
    public partial class Attendance
    {
        public long Id { get; set; }
        public int ClassRosterId { get; set; }
        public int SubjectNumber { get; set; }
        public int? Hours { get; set; }
    }
}
