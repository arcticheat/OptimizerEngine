using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class BookingCode
    {
        //BookingCode ID
        public int ID { get; set; }

        //Booking Code
        public String Code { get; set; }

        //Description
        public String Description { get; set; }

        //Can be selected by an instructor flag
        public bool InstructorSelectable { get; set; }

        //can be selected by a supervisor flag
        public bool SupervisorSelectable { get; set; }

        //Restrict calendar view flag
        public bool RestrictCalendarView { get; set; }

        //Deleted flag
        public bool DeleteFlag { get; set; }

        //category ID 
        public int CategoryID { get; set; }

        //Can be ignored by the optimizer
        public bool OptimizerIgnorable { get; set; }
    }
}
