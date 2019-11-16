using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class Qualification
    {
        //Qualification ID
        public int ID { get; set; }

        //Qualified to teach week 1 flag
        public bool Week_1 { get; set; }

        //Qualified to teach week 2 flag
        public bool Week_2 { get; set; }

        //Qualified to teach week 3 flag
        public bool Week_3 { get; set; }

        //Qualified to teach week 4 flag
        public bool Week_4 { get; set; }

        //Conditions flag
        public bool Condition { get; set; }

        //Qualification Code
        public string QualificationCode { get; set; }

        //Description
        public string Description { get; set; }

        //Deleted flag
        public bool Deleted { get; set; }


    }
}
