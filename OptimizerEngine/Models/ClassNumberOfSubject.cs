using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class ClassNumberOfSubject
    {
        //ID
        public int ID { get; set; }

        //Scheduled Class ID
        public int ScheduledClassID { get; set; }

        //Number of Subjects the scheduled class has 
        public int NumberOfSubjects { get; set; }
    }
}
