using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class CourseRequiredResources
    {
        public int ID { get; set; }

        [NotMapped]
        public string CourseCode { get; set; }

        public int ResourceID { get; set; }

        public int? Amount { get; set; }

        public int CourseID { get; set; }

        [NotMapped]
        public string ResourceDescription { get; set; }
    }
}
