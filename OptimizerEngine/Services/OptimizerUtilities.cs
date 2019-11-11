using LSS.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LSS.Services
{
    static class OptimizerUtilities
    {
        public static DateTime Max(DateTime a, DateTime b)
        {
            return a > b ? a : b;
        }

        public static DateTime Min(DateTime a, DateTime b)
        {
            return a < b ? a : b;
        }

        // source
        // https://stackoverflow.com/questions/1847580/how-do-i-loop-through-a-date-range
        public static IEnumerable<DateTime> EachWeekDay(DateTime from, DateTime thru)
        {
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
            {
                if ((day.DayOfWeek != DayOfWeek.Saturday) && (day.DayOfWeek != DayOfWeek.Sunday))
                {
                    yield return day;
                }
            }
        }

        /// <summary>
        /// Returns the work day however many days after the start date 
        /// </summary>
        /// <param name="startDate">The original date</param>
        /// <param name="workdays">How many days to go backwards from</param>
        /// <param name="substract">Set true to subtract week days instead</param>
        /// <returns></returns>
        public static DateTime AddWeekDays(DateTime startDate, int workdays, bool subtract = false)
        {
            DateTime date = startDate;
            while (workdays > 0)
            {
                if (subtract)
                    date = date.AddDays(-1);
                else
                    date = date.AddDays(1);
                if (date.DayOfWeek < DayOfWeek.Saturday &&
                    date.DayOfWeek > DayOfWeek.Sunday)
                    workdays--;
            }
            return date;
        }

        public static void PrintDate2DArray<T1, T2>(T1[,] matrix, DateTime StartDate, DateTime EndDate, List<T2> RowNames)
        {
            Console.Write("|\t");
            foreach (DateTime day in EachWeekDay(StartDate, EndDate))
            {
                Console.Write("|" + day.Month + "-" + day.Day + "\t");
            }
            Console.WriteLine("|");
            Console.Write("|-------");
            foreach (DateTime day in EachWeekDay(StartDate, EndDate))
            {
                Console.Write("|-------");
            }
            Console.WriteLine("|");

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                Console.Write("|" + RowNames[i]);
                if (RowNames[i].ToString().Length < 7)
                    Console.Write("\t");
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    Console.Write("|" + matrix[i, j] + "\t");
                }
                Console.WriteLine("|");
            }
            Console.WriteLine("");
        }

        public static void PrintRoomTable(List<Location> Locations)
        {
            Console.WriteLine("| LocationID | Local Room IDs |");
            Console.WriteLine("|------------|----------------|");
            foreach (var l in Locations)
            {
                if (!l.HasLocalRooms()) continue;
                Console.Write($"| {l.ID}         ");
                if (l.ID / 10 == 0)
                    Console.Write(" ");
                Console.Write("|");
                string RoomsString = " ";
                foreach (var r in l.LocalRooms)
                {
                    RoomsString += r + ", ";
                }
                if (RoomsString.Length > 1)
                {
                    RoomsString = RoomsString.Substring(0, RoomsString.Length - 2);
                }
                Console.WriteLine(RoomsString);

            }
            Console.WriteLine("");
        }
        public static void PrintInstructorTable(List<Location> Locations)
        {
            Console.WriteLine("| LocationID | Local Instructor IDs |");
            Console.WriteLine("|------------|----------------------|");
            foreach (var l in Locations)
            {
                if (!l.HasLocalInstructors()) continue;
                Console.Write($"| {l.ID}         ");
                if (l.ID / 10 == 0)
                    Console.Write(" ");
                Console.Write("|");
                string InstructorString = " ";
                int InstructorsOnLine = 0;
                foreach (var i in l.LocalInstructors)
                {
                    if (InstructorsOnLine >= 10)
                    {
                        InstructorString += "\n|------------| ";
                        InstructorsOnLine = 0;
                    }
                    InstructorString += i + ", ";
                    InstructorsOnLine++;

                }
                if (InstructorString.Length > 1)
                {
                    InstructorString = InstructorString.Substring(0, InstructorString.Length - 2);
                }
                Console.WriteLine(InstructorString);

            }
            Console.WriteLine("");
        }
    }
}
