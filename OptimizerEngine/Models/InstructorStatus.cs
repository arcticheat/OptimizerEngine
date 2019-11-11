using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class InstructorStatus
    {
        //Instructor FileNumber
        public String InstructorID { get; set; }

        //Record ID
        [Key]
        public int RecordID { get; set; }

        //ID of course
        public int CourseID { get; set; }

        //Course Code
        [NotMapped]
        public string CourseCode { get; set; }

        //Title of Course
        [NotMapped]
        public string CourseTitle { get; set; }

        //Teaching qualification
        public int Qualification { get; set; }

        //deleted flag
        public bool Deleted { get; set; }

        //FileNumber of who added qualification
        public String InputBy { get; set; }

        //Input Time
        public DateTime InputTime { get; set; }

        //Comments
        public String Comment { get; set; }

        //Course
        [NotMapped]
        public InstructorCourse Course { get; set; }

        //Array of courses an instructor can teach
        [NotMapped]
        public InstructorCourse[] CoursesArray { get; set; }

        //Instructor Last Name
        [NotMapped]
        public String InstructorLastName { get; set; }

        //Instructor First Name
        [NotMapped]
        public String InstructorFirstName { get; set; }

        //Instructor Middle Initial
        [NotMapped]
        public String InstructorMiddleInitial { get; set; }

        //Qualification Description
        [NotMapped]
        public String QualificationDescription { get; set; }

    }
    
    /// <summary>
    /// Course details for courses this instructor can teach
    /// </summary>
    [NotMapped]
    public class InstructorCourse
    {
        //Course Description
        public String Description { get; set; }

        //Course ID
        public int CourseID { get; set; }

        //Course Title
        public String CourseTitle { get; set; }

        //Comments 
        public String Comment { get; set; }
    } 
}
