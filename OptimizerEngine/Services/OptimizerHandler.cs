using OptimizerEngine.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace OptimizerEngine.Services
{
    class OptimizerHandler
    {
        private OptimizerEngineBuilder MyBuilder;
        private OptimizerEngine MyEngine;
        private bool ShowDebugMessages;
        private OptimizerScheduleResults MyResults;
        private bool ThreadInProgress;

        public OptimizerHandler(OptimizerEngineBuilder builder)
        {
            MyBuilder = builder;
            ShowDebugMessages = builder.ShowDebugMessages;
        }

        public void Run()
        {
            MyEngine = MyBuilder.Build();

            //Thread OptimizingThread = new Thread(new ParameterizedThreadStart(Optimize));

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

            // save results to database context

            if (ShowDebugMessages)
            {
                Console.WriteLine("Optimization Complete.\n");
                MyResults.Print();
                watch.Stop();
                Console.WriteLine(MyEngine.GetStatus("Final", watch.Elapsed));
            }
        }

        void Optimize()
        {
            var optimizerScheduleResults = new OptimizerScheduleResults
            {
                Inputs = MyEngine.Inputs,
                Results = new List<OptimizerResult>()
            };
            MyResults = MyEngine.OptimizeRecursion(optimizerScheduleResults, 0, MyBuilder.IsInstructorUnavailable,
                MyBuilder.IsRoomUnavailable, MyBuilder.CurrentlyReleased);
            ThreadInProgress = false;
            return;
        }
    }
}
