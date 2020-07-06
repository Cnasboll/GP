using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace gp
{
    public class Population : IList<FitnessEvaluation>
    {
        private readonly Problem _problem;

        private readonly List<FitnessEvaluation> _population;
        private FitnessEvaluation _bestFitnessEvaluation;
        
        private bool _solved;

        public Population(Random rd, int depth, Problem problem)
        {
            _problem = problem;
            _population = new List<FitnessEvaluation>();
            int previousPercentage = -1;
            Console.Out.WriteLine("Creating initial population...");
            for (int i = 0; !_solved && i < Gp.Popsize; ++i)
            {
                int percentage = (int) Math.Round(i * 100.0 / Gp.Popsize);
                if (percentage > previousPercentage)
                {
                    previousPercentage = percentage;
                    Console.Out.WriteLine($"{percentage}%");
                }

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
            while (!item.Tick())
            {
            }

            OnFitness(item);
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
            if (_bestFitnessEvaluation == null || fitnessEvaluation.BetterThan(_bestFitnessEvaluation))
            {
                _bestFitnessEvaluation = fitnessEvaluation;
                Console.WriteLine("Best individual so far: ");
                _bestFitnessEvaluation.Program.Print();
                Console.WriteLine("Fitness=" + _bestFitnessEvaluation.ErrorSum);

                _bestFitnessEvaluation.PrintAllResults();

                if (_bestFitnessEvaluation.ErrorSum < 1e-5m)
                {
                    Console.Write("PROBLEM SOLVED\n");
                    _solved = true;
                }

                Console.WriteLine("Simplified as: ");
                var simplifiedProgram = AssertSimplification(fitnessEvaluation);
                simplifiedProgram.Print();
            }
        }

        Program AssertSimplification(FitnessEvaluation fitnessEvaluation)
        {
            var simplifiedProgram = fitnessEvaluation.Program.Simplify();
            var simplifiedFitnessEvaluation = new FitnessEvaluation(simplifiedProgram, _problem);

            while (!simplifiedFitnessEvaluation.Tick((false)))
            {
            }


            if (!fitnessEvaluation.EqualResults(simplifiedFitnessEvaluation))
            {
                Console.WriteLine($"Simplification rendered another program: Fitness of original program is {fitnessEvaluation.ErrorSum}\nbut fitness of simplified program is simplified program is  {simplifiedFitnessEvaluation.ErrorSum}");
                Console.WriteLine("Here is a trace of the simplification for debugging:");
                _bestFitnessEvaluation.Program.Simplify(_bestFitnessEvaluation);

                simplifiedFitnessEvaluation = new FitnessEvaluation(simplifiedProgram, _problem);

                while (!simplifiedFitnessEvaluation.Tick((true)))
                {
                }

            }

            return simplifiedProgram;
        }

        public void Evolve(Random rd)
        {

            if (!_solved)
            {
                Console.WriteLine("Commencing evolutionary process");
            }

            int generation = 0;
            while (!_solved)
            {
                Console.Out.WriteLine($"Evolving generation {++generation}");
                int newIndivids = 0;
                int previousPercentage = -1;
                while (!_solved && newIndivids < Gp.Popsize)
                {
                    int percentage = (int) Math.Round(newIndivids * 100.0 / Gp.Popsize);
                    if (percentage > previousPercentage)
                    {
                        previousPercentage = percentage;
                        Console.Out.WriteLine($"{percentage}%");
                    }

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

                    var offspring = new FitnessEvaluation(newind, _problem);
                    while (!offspring.Tick())
                    {
                    }

                    int index = NegativeTournament(rd, Gp.Tsize, offspring);
                    this._population[index] = offspring;
                    OnFitness(offspring);
                    ++newIndivids;

                }
            }
        }

        public FitnessEvaluation Tournament(Random rd, int tsize)
        {

            FitnessEvaluation bestFitness = _population[rd.Next(_population.Count)];
            for (int i = 0; i < tsize; ++i)
            {
                FitnessEvaluation competitor = _population[rd.Next(_population.Count)];
                if (competitor.BetterThan(bestFitness))
                {
                    bestFitness = competitor;
                }
            }

            return bestFitness;
        }

        public int NegativeTournament(Random rd, int tsize, FitnessEvaluation offspring)
        {
            int index = rd.Next(_population.Count);
            FitnessEvaluation worstFitness = _population[index];
            bool betterThanOffspring = true;
            for (int i = 0; i < tsize || (betterThanOffspring && i < tsize * tsize); ++i)
            {
                int competitorIndex = rd.Next(_population.Count);
                FitnessEvaluation competitor = _population[competitorIndex];
                if (competitor.WorseThan(worstFitness))
                {
                    worstFitness = competitor;
                    index = competitorIndex;
                }

                if (competitor.WorseThan(offspring))
                {
                    betterThanOffspring = false;
                }
            }

            return index;
        }
    }
}