using System;
using System.Collections.Generic;

namespace OptimizerEngine.Models
{
    public partial class Qualification
    {
        public int Id { get; set; }
        public bool Week1 { get; set; }
        public bool Week2 { get; set; }
        public bool Week3 { get; set; }
        public bool Week4 { get; set; }
        public bool Condition { get; set; }
        public string QualificationCode { get; set; }
        public string Description { get; set; }
        public bool Deleted { get; set; }
    }
}
