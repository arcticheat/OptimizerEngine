using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class InstructorsByCourse : Course
    {
     
        //List of instructors who are qualified to teach a course 
        [NotMapped]
        public User[] InstructorArray { get; set; }
    }
}
