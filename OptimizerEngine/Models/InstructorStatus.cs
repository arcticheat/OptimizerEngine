using System;
using System.Collections.Generic;

namespace OptimizerEngine.Models
{
    public partial class InstructorStatus
    {
        public string InstructorId { get; set; }
        public int RecordId { get; set; }
        public int CourseId { get; set; }
        public int Qualification { get; set; }
        public bool Deleted { get; set; }
        public string InputBy { get; set; }
        public string Comment { get; set; }
        public DateTime InputTime { get; set; }
    }
}
