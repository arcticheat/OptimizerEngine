using System;
using System.Collections.Generic;
using System.Text;

namespace LSS.Models
{

    class BookingPrintable
    {
        public long Id { get; set; }
        public string RequestForId { get; set; }
        public int CodeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? LocationId { get; set; }
        public TimeSpan? StartTime { get; set; }

        public BookingPrintable (Booking book)
        {
            Id = book.ID;
            RequestForId = book.RequestForID;
            CodeId = book.CodeID;
            StartDate = book.StartDate;
            EndDate = book.EndDate;
            LocationId = book.LocationID;
            StartTime = book.StartTime;
        }
    }
}
