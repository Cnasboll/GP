using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace gp
{
    public class Population : IList<FitnessEvaluation>
    {
        #region BestFitnessEvaluationState enum

        public enum BestFitnessEvaluationState
        {
            WAITING,
            CALCULATING,  
            UPDATED
        } ;

        #endregion

        private readonly object _bestFitnessEvaluationGuard = new object();

        private readonly FitnessEvaluationQueue _fitnessEvaluationQueue;
        private readonly List<FitnessEvaluation> _population;  
        private FitnessEvaluation _bestFitnessEvaluation;
        private BestFitnessEvaluationState _bestFitnessEvaluationState = BestFitnessEvaluationState.WAITING;

        private readonly Thread _printerThread;
        private bool _solved;

        public Population(Random rd, int n, int depth, Problem problem)
        {
            _printerThread = new Thread(FitnessPrinter);
            _printerThread.Start();
            _fitnessEvaluationQueue = new FitnessEvaluationQueue(OnFitness);
            _population = new List<FitnessEvaluation>();
            for (int i = 0; i < n; ++i)
            {
                Add(new FitnessEvaluation(new Program(rd, depth, problem.Varnumber), problem));
            }
        }

        #region IList<FitnessEvaluation> Members

        public IEnumerator<FitnessEvaluation> GetEnumerator()
        {
            return _population.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(FitnessEvaluation item)
        {
            _population.Add(item);
            _fitnessEvaluationQueue.Enqueue(item);
        }

        public void Clear()
        {
            _population.Clear();
        }

        public bool Contains(FitnessEvaluation item)
        {
            return _population.Contains(item);
        }

        public void CopyTo(FitnessEvaluation[] array, int arrayIndex)
        {
            _population.CopyTo(array, arrayIndex);
        }

        public bool Remove(FitnessEvaluation item)
        {
            return _population.Remove(item);
        }

        public int Count
        {
            get { return _population.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public int IndexOf(FitnessEvaluation item)
        {
            return _population.IndexOf(item);
        }

        public void Insert(int index, FitnessEvaluation item)
        {
            _population.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _population.RemoveAt(index);
        }

        public FitnessEvaluation this[int index]
        {
            get { return _population[index]; }
            set { _population[index] = value; }
        }

        #endregion

        public void OnFitness(FitnessEvaluation fitnessEvaluation)
        {
            lock (_bestFitnessEvaluationGuard)
            {
                if (_bestFitnessEvaluation == null || fitnessEvaluation.CompareFitness(_bestFitnessEvaluation) < 0)
                {
                    if (fitnessEvaluation.SquaredErrorSum != null && !double.IsNaN(fitnessEvaluation.SquaredErrorSum.Value))
                    {
                        //Cancel any ongoing canculation
                        _bestFitnessEvaluation = new FitnessEvaluation(fitnessEvaluation);
                        _bestFitnessEvaluationState = BestFitnessEvaluationState.UPDATED;
                        Console.WriteLine("Best fitness so far: {0}", fitnessEvaluation.SquaredErrorSum);
                        Monitor.Pulse(_bestFitnessEvaluationGuard);
                    }
                }
            }
        }

        private void FitnessPrinter()
        {
            FitnessEvaluation fitnessEvaluation = null;
            while (!Solved)
            {
                lock (_bestFitnessEvaluationGuard)
                {
                    if (_bestFitnessEvaluationState == BestFitnessEvaluationState.UPDATED)
                    {
                        _bestFitnessEvaluationState = BestFitnessEvaluationState.CALCULATING;
                        Console.WriteLine("Best individual so far: ");
                        _bestFitnessEvaluation.Program.Print();
                        Console.WriteLine("Fitness=" + _bestFitnessEvaluation.SquaredErrorSum);
                        Console.WriteLine("Simplified as: ");
                        var simplifiedProgram = _bestFitnessEvaluation.Program.Simplify();
                        simplifiedProgram.Print();
                        fitnessEvaluation = new FitnessEvaluation(simplifiedProgram, _bestFitnessEvaluation.Problem);
                    }

                    if (fitnessEvaluation != null)
                    {
                        if (fitnessEvaluation.Tick(true))
                        {
                            if (Math.Abs((_bestFitnessEvaluation.SquaredErrorSum ?? 0.0) - (fitnessEvaluation.SquaredErrorSum ?? 0.0)) > 1e-5)
                            {
                                Console.WriteLine("Simplification rendered another program: Fitness of simplified program is " +
                                    fitnessEvaluation.SquaredErrorSum);
                                Console.WriteLine("Here is a trace of the simplification for debugging:");
                                _bestFitnessEvaluation.Program.Simplify(_bestFitnessEvaluation);
                            }
                            if (_bestFitnessEvaluation.SquaredErrorSum < 1e-5)
                            {
                                Console.Write("PROBLEM SOLVED\n");
                                _solved = true;
                                return;
                            }
                            _bestFitnessEvaluationState = BestFitnessEvaluationState.WAITING;
                            fitnessEvaluation = null;
                        }

                        if (_bestFitnessEvaluationState == BestFitnessEvaluationState.UPDATED)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Still waiting for a better program...\n");
                        Monitor.Wait(_bestFitnessEvaluationGuard);
                    }
                }
            }
        }

        bool Solved
        {
            get
            {
                lock (_bestFitnessEvaluationGuard)
                {
                    return _solved;
                }
            }
        }

        public void Evolve(Random rd)
        {
            try
            {
                if (!Solved)
                {
                    Console.WriteLine("Commencing evolutionary process");
                }

                while (!Solved)
                {
                    Program newind;
                    if (rd.NextDouble() < Gp._crossoverProb)
                    {
                        FitnessEvaluation parent1 = Tournament(rd, Gp.Tsize);
                        FitnessEvaluation parent2 = Tournament(rd, Gp.Tsize);

                        newind = parent1.Program.Crossover(rd, parent2.Program);

                        //Console.WriteLine("Crossing:\n{0}\nof fitness {1}\nwith {2}\nof fitness {3}\nyields\n{4}", parent1.Program, 
                        //    parent1.SquaredErrorSum, parent2.Program, parent2.SquaredErrorSum, newind);
                    }
                    else
                    {
                        newind = Tournament(rd, Gp.Tsize).Program.Mutate(rd, Gp._pmutPerNode);
                    }
                    FitnessEvaluation offspring = NegativeTournament(rd, Gp.Tsize);
                    lock (offspring)
                    {
                        offspring.Program = newind;
                    }
                    _fitnessEvaluationQueue.Enqueue(offspring);
                }
            }
            finally
            {
                _fitnessEvaluationQueue.Stop();
            }
        }

        public FitnessEvaluation Tournament(Random rd, int tsize)
        {
            lock (_fitnessEvaluationQueue.TickTournamentLock)
            {
                FitnessEvaluation bestFitness = _population[rd.Next(_population.Count)];
                for (int i = 0; i < tsize; ++i)
                {
                    FitnessEvaluation competitor = _population[rd.Next(_population.Count)];
                    if (competitor.CompareFitness(bestFitness) < 0)
                    {
                        bestFitness = competitor;
                    }
                }
                return bestFitness;
            }
        }

        public FitnessEvaluation NegativeTournament(Random rd, int tsize)
        {
            lock (_fitnessEvaluationQueue.TickTournamentLock)
            {
                FitnessEvaluation worstFitness = _population[rd.Next(_population.Count)];
                for (int i = 0; i < tsize; ++i)
                {
                    FitnessEvaluation competitor = _population[rd.Next(_population.Count)];
                    if (competitor.CompareFitness(worstFitness) > 0)
                    {
                        worstFitness = competitor;
                    }
                }
                return worstFitness;
            }
        }
    }
}