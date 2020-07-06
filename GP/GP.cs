using System;
using System.IO;

namespace gp
{
    public class Gp
    {
        public const int Depth = 5;
        public const int Popsize = 100000;
        public const int Tsize = 10;

        public static readonly double
            _crossoverProb = 0.9;

        public static readonly double
            _pmutPerNode = 0.05;

        public static void Main(String[] args)
        {
            try
            {
                var problem = Problem.Read(args[0]);

                var rd = new Random();
                var pop = new Population(rd, Popsize, Depth, problem);
                pop.Evolve(rd);
            }
            catch (FileNotFoundException)
            {
                Console.Write("ERROR: Please provide a data file");
            }
            /*catch (Exception e)
            {
                Console.Write("ERROR: Incorrect data format");
            }*/
            Console.In.Read();
        }
    }

    internal class NotSuchElementException : Exception
    {
    }
}