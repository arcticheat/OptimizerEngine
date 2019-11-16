using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class CancellationCodes
    {
        //ID
        public int ID { get; set; }

        //Cancellation Code
        public string Code { get; set; }
        
        //Description of Code 
        public string Description { get; set; }
    }
}
