using OptimizerEngine.Models;
using OptimizerEngine.Services;
using System;
using System.IO;

namespace OptimizerEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            bool askForInput = false;
            bool debug = true;
            var builder = new Services.OptimizerEngineBuilder(debug);

            DateTime StartDate = Convert.ToDateTime("1/1/2020");
            DateTime EndDate = Convert.ToDateTime("3/31/2020");

            if (askForInput)
            {
                Console.WriteLine("Enter the start day for the range to optimize (dd/mm/yyyy):");
                StartDate = Convert.ToDateTime(Console.ReadLine());

                Console.WriteLine("Enter the end day of the range to optimize (dd/mm/yyyy):");
                EndDate = Convert.ToDateTime(Console.ReadLine());
            }

            Console.WriteLine($"The Optimizer range is set from {StartDate} to {EndDate}");

            var watch = new System.Diagnostics.Stopwatch();
            if (debug) watch = System.Diagnostics.Stopwatch.StartNew();

            var engine = builder.Build(StartDate, EndDate);

            //engine.OptimizeGreedy(builder.IsRoomUnavailable, builder.IsInstructorUnavailable, builder.CurrentlyReleased);
            var answer = engine.OptimizeRecursion(ref engine.Inputs, new OptimizerScheduleResults(), builder.IsInstructorUnavailable, builder.IsRoomUnavailable, builder.CurrentlyReleased, 0);
            if (debug) Console.WriteLine("Optimization Complete.\n");

            if (debug)
            {
                watch.Stop();
                answer.Print();
                Console.WriteLine($"Time in milliseconds: {watch.ElapsedMilliseconds}");
                
            }

        }
    }
}
