using System;
using System.Collections.Generic;

namespace OptimizerEngine.Models
{
    public partial class CourseHistory
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Atachapter { get; set; }
        public int Hours { get; set; }
        public int MaxSize { get; set; }
        public string SpecialReq { get; set; }
        public string Comments { get; set; }
        public bool IsActive { get; set; }
        public int ActiveCourseId { get; set; }
    }
}
