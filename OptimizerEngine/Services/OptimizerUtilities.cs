using LSS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

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
            Console.WriteLine("| LocationCode | Local Room IDs |");
            Console.WriteLine("|--------------|----------------|");
            foreach (var l in Locations)
            {
                if (!l.HasLocalRooms()) continue;
                Console.Write($"| {l.Code}          |");
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
            Console.WriteLine("| LocationCode | Local Instructor IDs |");
            Console.WriteLine("|--------------|----------------------|");
            foreach (var l in Locations)
            {
                if (!l.HasLocalInstructors()) continue;
                Console.Write($"| {l.Code}          |");
                string InstructorString = " ";
                int InstructorsOnLine = 0;
                foreach (var i in l.LocalInstructors)
                {
                    if (InstructorsOnLine >= 10)
                    {
                        InstructorString += "\n|              | ";
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

        internal static void Print2dDictionary(Dictionary<int, Dictionary<string, bool>> dict)
        {
            var output = "| ID | ";
            output += "|----|------";

            foreach (var firstPair in dict)
            {
                output += "| " + firstPair.Key;
                if (firstPair.Key / 10 == 0)
                    output += " ";
                output += " | ";
                foreach (var secondPair in firstPair.Value)
                {
                    output += secondPair.Value + ", ";
                }
                output = output.Substring(0, output.Length - 2);
                output += '\n';
            }
        }
        public static T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }

        // source: https://stackoverflow.com/questions/419019/split-list-into-sublists-with-linq/10425490
        public static List<List<T>> Split<T>(List<T> source, int subLists)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / subLists)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

    }
}
