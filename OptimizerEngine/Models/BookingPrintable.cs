using OptimizerEngine.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OptimizerEngine
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
            Id = book.Id;
            RequestForId = book.RequestForId;
            CodeId = book.CodeId;
            StartDate = book.StartDate;
            EndDate = book.EndDate;
            LocationId = book.LocationId;
            StartTime = book.StartTime;
        }
    }
}
