using System;
using System.Collections.Generic;

namespace OptimizerEngine.Models
{
    public partial class ClassNumberOfSubject
    {
        public int Id { get; set; }
        public int ScheduledClassId { get; set; }
        public int NumberOfSubjects { get; set; }
    }
}
