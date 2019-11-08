using System;
using System.Collections.Generic;

namespace OptimizerEngine.Models
{
    public partial class RequestedCourse
    {
        public int Id { get; set; }
        public string RequestById { get; set; }
        public int? CourseId { get; set; }
        public int LocationId { get; set; }
        public int Status { get; set; }
        public string Comment { get; set; }
        public DateTime NeededBy { get; set; }
        public string Shift { get; set; }
        public string Reason { get; set; }
        public int NumStudents { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string TimelineReason { get; set; }
        public string ResponseComment { get; set; }
    }
}
