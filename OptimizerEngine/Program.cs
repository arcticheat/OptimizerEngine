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
            bool showSetup = true;
            var builder = new Services.OptimizerEngineBuilder(showSetup);

            DateTime StartDate = Convert.ToDateTime("11/4/2019");
            DateTime EndDate = Convert.ToDateTime("11/15/2019");

            if (askForInput)
            {
                Console.WriteLine("Enter the start day for the range to optimize (dd/mm/yyyy):");
                StartDate = Convert.ToDateTime(Console.ReadLine());

                Console.WriteLine("Enter the end day of the range to optimize (dd/mm/yyyy):");
                EndDate = Convert.ToDateTime(Console.ReadLine());
            }

            Console.WriteLine($"The Optimizer range is set from {StartDate} to {EndDate}");

            var engine = builder.Build(StartDate, EndDate);

            engine.OptimizeGreedy();

        }
    }
}
