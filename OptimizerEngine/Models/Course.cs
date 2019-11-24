using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    [Serializable]
    public class Course
    {
        //ID
        public long ID { get; set; }

        //Course Code
        public String Code { get; set; }

        //Title of the course
        public String Title { get; set; }

        //Description of course
        public String Description { get; set; }

        //ATAChapter
        public String ATAChapter { get; set; }

        //Hours the course has
        public int Hours { get; set; }

        //Max class size
        public int MaxSize { get; set; }

        //Any special requirements this course has
        public String SpecialReq { get; set; }

        //Comments
        public String Comments { get; set; }

        //active flag
        public bool IsActive { get; set; }

        //Categories this course falls into
        [NotMapped]
        public String[] CategoriesArray { get; set; }

        //Resources this course requires (for scheduling a room)
        [NotMapped]
        public CourseRequiredResources[] Resources {get; set;}
        [NotMapped]
        public Dictionary<string, DateTime> QualifiedInstructors = new Dictionary<string, DateTime>();
        [NotMapped]
        public Dictionary<int, int?> RequiredResources = new Dictionary<int, int?>();
    }
}
