using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class RequestedCourse
    {
        //Requested Course ID
        public int ID { get; set; }

        //FileNumber for who requested the course
        public String RequestById { get; set; }

        //ID of course being requested
        public int CourseID { get; set; }

        //Course code of course being requested
        [NotMapped]
        public String CourseCode { get; set; }

        //Requested Location
        public int LocationID { get; set; }

        //Status of the request
        public int Status { get; set; }

        //Comments
        public String Comment { get; set; }

        //When the course is needed by
        public DateTime NeededBy { get; set; }

        //Time of day to schedule the course
        public string Shift { get; set; }

        //Why the course is being requested
        public string Reason { get; set; }

        //Number of students who will take the course
        public int NumStudents { get; set; }

        //When this course was requested
        public DateTime TimeStamp { get; set; }

        //Requester Email
        public string Email { get; set; }

        //Requester Phone number
        public string Phone { get; set; }

        //Reason for need by date
        public string TimelineReason { get; set; }

        //Comments added by scheduler upon approval/denial
        public string ResponseComment { get; set; }

        //Coordinator
        [NotMapped]
        public string Coordinator { get; set; }

        //Location code
        [NotMapped]
        public string Location { get; set; }

        //title of course 
        [NotMapped]
        public string CourseTitle { get; set; }


    }
}
