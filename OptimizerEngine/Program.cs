using OptimizerEngine.Models;
using OptimizerEngine.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizerEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            bool askForInput = false;
            bool debug = true;
            var builder = new Services.OptimizerEngineBuilder(debug);

            DateTime StartDate = Convert.ToDateTime("1/6/2020");
            DateTime EndDate = Convert.ToDateTime("1/10/2020");

            if (askForInput)
            {
                Console.WriteLine("Enter the start day for the range to optimize (dd/mm/yyyy):");
                StartDate = Convert.ToDateTime(Console.ReadLine());

                Console.WriteLine("Enter the end day of the range to optimize (dd/mm/yyyy):");
                EndDate = Convert.ToDateTime(Console.ReadLine());
            }

            Console.WriteLine($"The Optimizer range is set from {StartDate} to {EndDate}");
            var engine = builder.Build(StartDate, EndDate);

            var watch = new System.Diagnostics.Stopwatch();
            if (debug) watch = System.Diagnostics.Stopwatch.StartNew();

            // Greedy
            //engine.OptimizeGreedy(builder.IsRoomUnavailable, builder.IsInstructorUnavailable, builder.CurrentlyReleased);
            //watch.Stop();
            //Console.WriteLine($"Time in milliseconds: {watch.ElapsedMilliseconds}");

            // Recursion
            var optimizerScheduleResults = new OptimizerScheduleResults
            {
                Inputs = engine.Inputs,
                Results = new List<OptimizerResult>()
            };

            //Thread OptimizingThread = new Thread(new ParameterizedThreadStart(Optimize));

            Console.WriteLine("Optimizing...");

            var results = engine.OptimizeRecursion(optimizerScheduleResults, 0, builder.IsInstructorUnavailable,
                builder.IsRoomUnavailable, builder.CurrentlyReleased);

            if (debug) Console.WriteLine("Optimization Complete.\n");

            if (debug)
            {
                watch.Stop();
                results.Print();
                Console.WriteLine($"Time in milliseconds: {watch.ElapsedMilliseconds}");
            }

        }
        //public static void Optimize(object param)
        //{
        //    var builder = (OptimizerEngineBuilder)param;
        //    var engine = builder.Build();
        //    var optimizerScheduleResults = new OptimizerScheduleResults();
        //    optimizerScheduleResults.Inputs = engine.Inputs;
        //    optimizerScheduleResults.Results = new List<OptimizerResult>();
        //    Results = engine.OptimizeRecursion(optimizerScheduleResults, 0, builder.IsInstructorUnavailable, builder.IsRoomUnavailable, builder.CurrentlyReleased);
        //}
    }
}   

