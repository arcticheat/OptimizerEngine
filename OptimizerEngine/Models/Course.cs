using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptimizerEngine.Models
{
    public partial class Course
    {
        public string Code { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Atachapter { get; set; }
        public int Hours { get; set; }
        public int MaxSize { get; set; }
        public string SpecialReq { get; set; }
        public string Comments { get; set; }
        public bool IsActive { get; set; }
        public long Id { get; set; }
        [NotMapped]
        public List<string> QualifiedInstructors = new List<string>();
        [NotMapped]
        public List<Tuple<int, int?>> RequiredResources = new List<Tuple<int, int?>>();
    }
}