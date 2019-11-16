using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class CourseCategories
    {
        //ID
        [Key]
        public int ID { get; set; }

        //Course Code
        public String CourseCode { get; set; }

        //Course ID
        public int CourseID { get; set; }

        //Category ID
        public int CategoryID { get; set; }
    }
}