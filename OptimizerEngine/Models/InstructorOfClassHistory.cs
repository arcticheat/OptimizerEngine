using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class InstructorOfClassHistory
    {
        //ID
        public int ID { get; set; }

        //Associated scheduled class ID
        public int ScheduleID { get; set; }

        //instructor FileNumber
        public String UserID { get; set; }

        //Associated scheduled Class ID
        public int ClassID { get; set; }

        //Start Time
        public TimeSpan StartTime { get; set; }

        //Start Time
        public DateTime StartDate { get; set; }

        //End Date 
        public DateTime EndDate { get; set; }
    }
}
