using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class Email
    {
        //Addresses
        public string[] Addresses { get; set; }

        //Subject
        public string Subject { get; set; }

        //Body
        public string Body { get; set; }
    }
}
