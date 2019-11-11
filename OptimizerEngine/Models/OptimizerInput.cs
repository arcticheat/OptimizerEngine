using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class OptimizerInput
    {
        public int Id { get; set; }
        public String CourseCode { get; set; }
        public String LocationID { get; set; }
        public int NumTimesToRun { get; set; }
        public TimeSpan StartTime { get; set; }
        public String CourseTitle { get; set; }
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

