using LSS.Models;
using LSS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LSS
{
    class Program
    {
        static void Main(string[] args)
        {
            bool askForInput = false;
            bool debug = true;
            Priority priority = Priority.Default;

            DateTime StartDate = Convert.ToDateTime("1/1/2020");
            DateTime EndDate = Convert.ToDateTime("1/31/2020");

            if (askForInput)
            {
                Console.WriteLine("Enter the start day for the range to optimize (dd/mm/yyyy):");
                StartDate = Convert.ToDateTime(Console.ReadLine());

                Console.WriteLine("Enter the end day of the range to optimize (dd/mm/yyyy):");
                EndDate = Convert.ToDateTime(Console.ReadLine());
            }

            DatabaseContext context = new DatabaseContext();
            Console.WriteLine($"The Optimizer range is set from {StartDate} to {EndDate}");
            Console.WriteLine($"Priority: {priority}");
            var builder = new Services.OptimizerEngineBuilder(context, StartDate, EndDate, priority, debug);

            var handler = new OptimizerHandler(context, builder);
            handler.Run();
        }
    }
}   

