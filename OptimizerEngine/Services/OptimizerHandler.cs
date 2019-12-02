using ConsoleTables;
using LSS.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LSS.Services
{
    class OptimizerHandler
    {
        private OptimizerEngineBuilder MyBuilder;
        private OptimizerEngine MyEngine;
        private bool ShowDebugMessages;
        private OptimizerScheduleResults MyResults;
        private bool ThreadInProgress;
        private DatabaseContext context;
        private int counter = 1;
        private Stopwatch watch;
        
        public OptimizerHandler(DatabaseContext _context, OptimizerEngineBuilder builder)
        {
            MyBuilder = builder;
            ShowDebugMessages = builder.ShowDebugMessages;
            context = _context;
            watch = new System.Diagnostics.Stopwatch();
        }

        public void Run()
        {
            MyEngine = MyBuilder.Build();

            if (ShowDebugMessages) watch = System.Diagnostics.Stopwatch.StartNew();

            Console.WriteLine("Optimizing...");

            Thread thread = new Thread(() => Optimize());
            thread.Start();
            ThreadInProgress = true;
            counter = 1;
            while (ThreadInProgress)
            {
                //cancellationToken.ThrowIfCancellationRequested();
                if (MyEngine.MyPriority != Priority.FirstAvailable)
                {
                    var status = MyEngine.GetStatus(counter++.ToString(), watch.Elapsed);
                    Console.WriteLine(status);
                    System.Threading.Thread.Sleep(4000);
                }
            }
            thread.Join();

            // Add the results to the table
            foreach (var result in MyResults.Results)
            {
                result.CreationTimestamp = DateTime.Today;
                context.Entry(result).State = result.ID == 0 ? EntityState.Added : EntityState.Modified;
            }
            foreach (var input in MyResults.Inputs)
            {
                context.Entry(input).State = input.Id == 0 ? EntityState.Added : EntityState.Modified;
            }
            //context.SaveChanges();

            if (ShowDebugMessages)
            {
                watch.Stop();
                Console.WriteLine("\nOptimization Complete.\n");
                if (MyEngine.MyPriority != Priority.FirstAvailable)
                    Console.WriteLine(MyEngine.GetStatus("Final", watch.Elapsed) + "\n");

                Console.WriteLine("\nOriginal Optimizer Inputs");
                ConsoleTable.From<OptimizerInputPrintable>(MyEngine.Inputs.Select(x => new OptimizerInputPrintable(x))).Write(Format.MarkDown);
                Console.WriteLine("");
                Console.WriteLine("Preexisting Schedule");
                ConsoleTable.From<ScheduledClassPrintable>(MyEngine.CurrentSchedule.Select(c => new ScheduledClassPrintable(c))).Write(Format.MarkDown);
                switch (MyEngine.MyPriority)
                {
                    case Priority.Default:
                        Console.WriteLine($"Total classes scheduled: {MyResults.Results.Count} out of {MyBuilder.OriginalInputCount}.\n");
                        break;
                    case Priority.FirstAvailable:
                        break;
                    case Priority.MaximizeInstructorLongestToTeach:
                        Console.WriteLine($"The total time between all instructor assignments and the last time they taught the course is {MyEngine.CurrentBestAnswer.OptimizationScore} days.\n");
                        break;
                    case Priority.MaximizeSpecializedInstructors:
                        Console.WriteLine($"Between all assigned instructors for this answer, they have a total of {MyResults.OptimizationScore} qualifications.\n");
                        break;
                    case Priority.MinimizeForeignInstructorCount:
                        Console.WriteLine($"{MyResults.OptimizationScore} instructors will have to travel to fulfill these assignments.\n");
                        break;
                    case Priority.MinimizeInstructorTravelDistance:
                        Console.WriteLine($"Instructors will have to travel a total of {MyResults.OptimizationScore} miles to fulfill these assignments.\n");
                        break;
                }
                MyResults.Print();
            }
        }

        void Optimize()
        {
            if (MyEngine.MyPriority == Priority.FirstAvailable)
                MyResults = MyEngine.OptimizeGreedy(MyBuilder.IsRoomUnavailable, MyBuilder.IsInstructorUnavailable,
                    MyBuilder.CurrentlyReleased, MyBuilder.LocallyTaughtCoursesPerDay);
            else if (MyEngine.MyPriority == Priority.MaximizeInstructorLongestToTeach)
            {
                MyEngine.OptimizeLongestToTeach(MyBuilder.StartingResults, 0,  MyBuilder.IsInstructorUnavailable, MyBuilder.IsRoomUnavailable,
                    MyBuilder.CurrentlyReleased, MyBuilder.LocallyTaughtCoursesPerDay, 0, MyEngine.CourseCatalog);
                MyResults = MyEngine.CurrentBestAnswer;
            }
            else
            {
                MyEngine.OptimizeRecursion(MyBuilder.StartingResults, 0, MyBuilder.IsInstructorUnavailable,
                    MyBuilder.IsRoomUnavailable, MyBuilder.CurrentlyReleased, MyBuilder.LocallyTaughtCoursesPerDay, 0);
                MyResults = MyEngine.CurrentBestAnswer;
               
            }
            MyResults.Inputs.AddRange(MyEngine.WillAlwaysFail);
                
            ThreadInProgress = false;
            return;
        }
    }
}
