using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptimizerEngine.Models
{
    public partial class OptimizerInput
    {
        public int Id { get; set; }
        public string CourseCode { get; set; }
        public string LocationId { get; set; }
        public int NumTimesToRun { get; set; }
        public TimeSpan StartTime { get; set; }
        public string CourseTitle { get; set; }
        public bool Succeeded { get; set; }
        public string Reason { get; set; }
        [NotMapped]
        public int LengthDays { get; set; }
        [NotMapped]
        public int CourseId { get; set; }
        [NotMapped]
        public int LocationIdLiteral { get; set; }

    }
}
