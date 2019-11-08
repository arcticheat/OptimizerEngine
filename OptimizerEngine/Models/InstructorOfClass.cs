using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptimizerEngine.Models
{
    public partial class InstructorOfClass
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ClassId { get; set; }
        public TimeSpan StartTime { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool Cancelled { get; set; }
        public bool? Hidden { get; set; }
        [NotMapped]
        public bool LocalAssignment { get; set; }
    }
}
