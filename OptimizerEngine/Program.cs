﻿using LSS.Models;
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
            bool recursion = true;

            DateTime StartDate = Convert.ToDateTime("1/1/2020");
            DateTime EndDate = Convert.ToDateTime("1/31/2020");

                if (askForInput)
                {
                    Console.WriteLine("Enter the start day for the range to optimize (dd/mm/yyyy):");
                    StartDate = Convert.ToDateTime(Console.ReadLine());

                    Console.WriteLine("Enter the end day of the range to optimize (dd/mm/yyyy):");
                    EndDate = Convert.ToDateTime(Console.ReadLine());
                }

            Console.WriteLine($"The Optimizer range is set from {StartDate} to {EndDate}");
            var builder = new Services.OptimizerEngineBuilder(StartDate, EndDate, debug);

            // Greedy
            if (!recursion)
            {
                var watch = new System.Diagnostics.Stopwatch();
                if (debug) watch = System.Diagnostics.Stopwatch.StartNew();
                var engine = builder.Build();
                engine.OptimizeGreedy(builder.IsRoomUnavailable, builder.IsInstructorUnavailable, builder.CurrentlyReleased, builder.LocallyTaughtCoursesPerDay);
                if (debug)
                {
                    watch.Stop();
                    Console.WriteLine($"Time in milliseconds: {watch.ElapsedMilliseconds}ms");
                }
            }
            // Recursion
            else
            {
                var handler = new OptimizerHandler(builder);
                handler.Run();
            }
        }
    }
}   

