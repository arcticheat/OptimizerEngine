using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LSS.Models
{
    public class BookingHistory
    {
        //Booking History ID
        public long ID { get; set; }

        //Exception ID
        public int BookingID { get; set; }

        //FileNumber of who the exception was requested for
        public String RequestForID { get; set; }

        //FileNumber of who requested the exception
        public String RequestByID { get; set; }

        //Possible associated scheduled class
        public int? ScheduleID { get; set; }

        //Exception Code
        public int CodeID { get; set; }

        //Location of booking
        public int LocationID { get; set; }

        //Comments added by requester
        public String RequestComment { get; set; }

        //Comments added by approver/denier
        public String AdminComment { get; set; }

        //Time exception was requested
        public DateTime RequestTimestamp { get; set; }

        //Time exception was last updated
        public DateTime ResponseTimestamp { get; set; }

        //exception status
        public int Status { get; set; }

        //cancelled flag
        public bool Cancelled { get; set; }

        //Start Date
        public DateTime StartDate { get; set; }

        //End Date
        public DateTime EndDate { get; set; }

        //Start Time 
        public TimeSpan StartTime { get; set; }

    }
}

