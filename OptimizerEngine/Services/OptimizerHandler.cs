using LSS.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        
        public OptimizerHandler(DatabaseContext _context, OptimizerEngineBuilder builder)
        {
            MyBuilder = builder;
            ShowDebugMessages = builder.ShowDebugMessages;
            context = _context;
        }

        public void Run()
        {
            MyEngine = MyBuilder.Build();

            Console.WriteLine("Please review the data the optimizer engine will be using above.\nPress Enter when ready to proceed.");
            do
            {

            } while (Console.ReadKey().Key != ConsoleKey.Enter);

            var watch = new System.Diagnostics.Stopwatch();
            if (ShowDebugMessages) watch = System.Diagnostics.Stopwatch.StartNew();

            Console.WriteLine("Optimizing...");

            Thread thread = new Thread(() => Optimize());
            thread.Start();
            ThreadInProgress = true;
            int counter = 1;
            while (ThreadInProgress)
            {
                var status = MyEngine.GetStatus(counter++.ToString(), watch.Elapsed);
                Console.WriteLine(status);
                System.Threading.Thread.Sleep(4000);
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
                Console.WriteLine("Optimization Complete.\n");
                MyResults.Print();
                watch.Stop();
                Console.WriteLine(MyEngine.GetStatus("Final", watch.Elapsed));
            }
        }

        async void Optimize()
        {
            if (MyEngine.MyPriority == Priority.FirstAvailable)
                MyEngine.OptimizeGreedy(MyBuilder.IsRoomUnavailable, MyBuilder.IsInstructorUnavailable,
                    MyBuilder.CurrentlyReleased, MyBuilder.LocallyTaughtCoursesPerDay);
            else
            {
                Task<OptimizerScheduleResults> resultThread = MyEngine.OptimizeRecursionAsync(MyBuilder.StartingResults, 0, MyBuilder.IsInstructorUnavailable,
                    MyBuilder.IsRoomUnavailable, MyBuilder.CurrentlyReleased, MyBuilder.LocallyTaughtCoursesPerDay, 0);
                await resultThread;
                MyResults = resultThread.Result;
            }
                
            ThreadInProgress = false;
            return;
        }
    }
}
