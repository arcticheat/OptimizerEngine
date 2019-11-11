using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class Notify
    {
        //ID
        public int ID { get; set; }

        //fileNumber to send notification to
        public string SendTo{ get; set; }

        //fileNumber of who the notification is from
        public string SendFrom { get; set; }

        //read flag
        public bool IsRead { get; set; }

        //notification message 
        public string Message { get; set; }

        //Date the message was sent 
        public DateTime SendDate { get; set; }
    }
}
