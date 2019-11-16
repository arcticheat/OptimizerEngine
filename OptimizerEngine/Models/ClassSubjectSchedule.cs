using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    /**
     * This model will be used in the future to allow the input of 
     * scheduled class subject names and required hours for those subjects.
     * Not being used right now. 
     **/
    public class ClassSubjectSchedule
    {
        int ID { get; set; }

        int ScheduledClassID { get; set; }

        string Name { get; set; }

        int Hours { get; set; }
    }
}
