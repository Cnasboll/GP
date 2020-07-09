using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;

namespace gp
{
    public class Gp
    {
        public const int Depth = 5;
        public const int Popsize = 100000;
        public const int Tsize = 2;

        public static readonly double
            _migrationProb = 0.1;

        public static readonly double
            _crossoverProb = 0.9;

        public static readonly double
            _pmutPerNode = 0.05;

        private static readonly ILog Log = LogManager.GetLogger(typeof(Gp));

        public static void Main(String[] args)
        {
            try
            {
                XmlConfigurator.Configure();
                var problem = Problem.Read(args[0]);

                var rd = new Random();
                var populations = new Population[Environment.ProcessorCount];
                var migrationQueues = new ConcurrentQueue<FitnessEvaluation>[populations.Length];
                var tasks = new Task[populations.Length];
                var factory = new TaskFactory();
                var source = new CancellationTokenSource();
                var token = source.Token;
                for (int i = 0; i < populations.Length; ++i)
                {
                    var pop = new Population(rd, Depth, problem, i);
                    if (pop.Solved)
                    {
                        return;
                    }

                    populations[i] = pop;
                    migrationQueues[i] = new ConcurrentQueue<FitnessEvaluation>();
                }

                for (int i = 0; i < populations.Length; ++i)
                {
                    var pop = populations[i];
                    tasks[i] = factory.StartNew(() => Evolve(pop, new Random(), token, migrationQueues),
                        token,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default);
                }

                Task.WaitAny(tasks);
                source.Cancel();
            }
            catch (FileNotFoundException)
            {
                Log.Error("ERROR: Please provide a data file");
            }
            /*catch (Exception e)
            {
                Console.Write("ERROR: Incorrect data format");
            }*/
        }


        static void Evolve(Population pop, Random rd, CancellationToken token,
            ConcurrentQueue<FitnessEvaluation>[] migrationQueues)
        {
            while (!pop.Solved && !token.IsCancellationRequested)
            {
                try
                {
                    pop.Evolve(rd, token, migrationQueues);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to evolve population {pop.Index}", e);
                }
            }
        }
    }

    internal class NotSuchElementException : Exception
    {
    }
}