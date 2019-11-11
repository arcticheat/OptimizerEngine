using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LSS.Models
{
    public class Booking
    {
        //Booking ID
        public long ID { get; set; }

        //FileNumber of who the exception is for
        public String RequestForID { get; set; }

        //FileNumber of who requested the exception
        public String RequestByID { get; set; }

        //Possible associated scheduled class
        public int? ScheduleID { get; set; }

        //Exception code
        public int CodeID { get; set; }

        //Location of exception
        public int LocationID { get; set; }

        //Comments added by the requester
        public String RequestComment { get; set; }

        //Comments added by approver/denier
        public String AdminComment { get; set; }

        //Who last edited this exception
        public string LastTouchedBy { get; set; }

        //when this exception was requested 
        public DateTime RequestTimestamp { get; set; }

        //When this exception was last updated 
        public DateTime ResponseTimestamp { get; set; }

        //Exception status 
        public int Status { get; set; }

        //Cancelled flag
        public bool Cancelled { get; set; }

        //Exception Start Date
        public DateTime StartDate { get; set; }

        //Exception End Date 
        public DateTime EndDate { get; set; }

        //Exception Start Time 
        public TimeSpan StartTime { get; set; }

        //Instructor FileNumber
        [NotMapped]
        public String InstructorFileNumber { get; set; }

        //Instructor First Name
        [NotMapped]
        public String InstructorFirstName { get; set; }

        //Instructro Last NAme
        [NotMapped]
        public String InstructorLastName { get; set; }

        //Instructor Middle Initial 
        [NotMapped]
        public String InstructorMiddleInitial { get; set; }

        //Supervisor First Name
        [NotMapped]
        public String SupervisorFirstName { get; set; }

        //Supervisor Last Name
        [NotMapped]
        public String SupervisorLastName { get; set; }

        //Supervisor Middle Initial 
        [NotMapped]
        public String SupervisorMiddleInitial { get; set; }

        //Exception Code
        [NotMapped]
        public String BookingCode { get; set; }

        //Exception Code Description
        [NotMapped]
        public String BookingDescription { get; set; } 

        //Location Code
        [NotMapped]
        public String LocationCode { get; set; }

        //Point ID
        [NotMapped]
        public int PointID { get; set; }

        //Restrict view flag
        [NotMapped]
        public bool RestrictCalendarView { get; set; }

        //Point Code 
        [NotMapped]
        public String InstructorPointCode { get; set; }

        //Name of who last edited exception 
        [NotMapped]
        public string LastTouchedName { get; set; }

    }
}
/*
 * supervisor (FL), instructor (FL), exception & reason, startdate, enddate, comment
 */