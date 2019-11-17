using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LSS.Models;

namespace LSS.Services
{
    public class Utilities
    {
        public static void log(object text)
        {
            string now = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
            //System.IO.File.AppendAllText(@"c:\Users\CSE498\Desktop\LSSLog.txt", (string)text + "\n"); //This one is for the capstone computer
            //System.IO.File.AppendAllText(@"C:\Users\Fergi\Documents\LSSLog.txt", (string)text + "\n");
        }

        public static DateTime getNextBusinessDate(DateTime d, int days = 1)
        {
            //Gets the next business day datetime - skipping sat/sun
            for (int i = 0; i < days; i++)
            {
                int daysUntilNextBusinessDay = 1;

                while (((int)d.DayOfWeek + daysUntilNextBusinessDay) % 7 == 0 || (int)d.DayOfWeek + daysUntilNextBusinessDay == 6)
                {
                    daysUntilNextBusinessDay += 1;
                }

                d = d.AddDays(daysUntilNextBusinessDay);
            }

            return d;

        }

        public static int timeSpanToIndex(TimeSpan t, int timeIncrement, TimeSpan startOfDay)
        {
            int index = (t.Hours * 60 + t.Minutes) / timeIncrement;
            int offset = index - ((startOfDay.Hours * 60 + startOfDay.Minutes) / timeIncrement);
            return offset;
        }

        public static TimeSpan indexToTimeSpan(int index, int timeIncrement, TimeSpan startOfDay)
        {
            TimeSpan t = TimeSpan.FromHours((index*timeIncrement)/60);
            t = t.Add(TimeSpan.FromMinutes((index*timeIncrement) % 60));
            t = t.Add(startOfDay);
            return t;
        }

        public static User getUserFromFileNumber(DatabaseContext _context, string fileNumber)
        {
            return _context.User.Where(x => x.FileNumber == fileNumber).FirstOrDefault();
            
        }

        public static double getDistanceLatLong(double lat1, double lon1, double lat2, double lon2)
        {
            return Math.Sqrt(Math.Pow(lat1 - lat2, 2) + Math.Pow(lon1 - lon2, 2));
        }

    }
}
