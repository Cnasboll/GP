using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace gp
{
    public class Population : IList<FitnessEvaluation>
    {
        private readonly Problem _problem;
        private readonly int _index;

        private readonly List<FitnessEvaluation> _population;
        private FitnessEvaluation _bestFitnessEvaluation;
        
        private bool _solved;

        public Population(Random rd, int depth, Problem problem, int index)
        {
            _problem = problem;
            _index = index;
            _population = new List<FitnessEvaluation>();
            Console.Out.WriteLine($"Creating initial population {index}");
            for (int i = 0; !_solved && i < Gp.Popsize; ++i)
            {
                Add(new FitnessEvaluation(new Program(rd, depth, problem.Varnumber), problem));
                if (_solved)
                {
                    return;
                }
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
            var stringBuilder = new StringBuilder();
            item.Evaluate(stringBuilder);

            OnFitness(item, stringBuilder);
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Insert(0, "\n");
                Console.Out.WriteLine(stringBuilder);
            }
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

        public bool Solved => _solved;
        public int Index => _index;

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

        public void OnFitness(FitnessEvaluation fitnessEvaluation, StringBuilder stringBuilder)
        {
            if (_bestFitnessEvaluation == null || fitnessEvaluation.BetterThan(_bestFitnessEvaluation))
            {
                _bestFitnessEvaluation = fitnessEvaluation;
                _bestFitnessEvaluation.PrintAllResults(stringBuilder);
                stringBuilder.AppendLine($"Best individual so far in population {_index}: ");
                _bestFitnessEvaluation.Program.Print(stringBuilder);
                stringBuilder.AppendLine("Fitness=" + _bestFitnessEvaluation.ErrorSum);

                if (_bestFitnessEvaluation.ErrorSum < 1e-5m)
                {
                    stringBuilder.AppendLine("PROBLEM SOLVED");
                    _solved = true;
                }

                stringBuilder.AppendLine("Simplified as: ");
                var simplifiedProgram = AssertSimplification(fitnessEvaluation, stringBuilder);
                simplifiedProgram.Print(stringBuilder);
            }
        }

        Program AssertSimplification(FitnessEvaluation fitnessEvaluation, StringBuilder stringBuilder)
        {
            var simplifiedProgram = fitnessEvaluation.Program.Simplify(stringBuilder);
            var simplifiedFitnessEvaluation = new FitnessEvaluation(simplifiedProgram, _problem);

            simplifiedFitnessEvaluation.Evaluate((false), stringBuilder);


            if (!fitnessEvaluation.EqualResults(simplifiedFitnessEvaluation))
            {
                stringBuilder.AppendLine($"Simplification rendered another program: Fitness of original program is {fitnessEvaluation.ErrorSum}\nbut fitness of simplified program is simplified program is  {simplifiedFitnessEvaluation.ErrorSum}");
                stringBuilder.AppendLine("Here is a trace of the simplification for debugging:");
                _bestFitnessEvaluation.Program.Simplify(_bestFitnessEvaluation, stringBuilder);

                simplifiedFitnessEvaluation = new FitnessEvaluation(simplifiedProgram, _problem);

                simplifiedFitnessEvaluation.Evaluate((true), stringBuilder);

            }

            return simplifiedProgram;
        }

        public void Evolve(Random rd, CancellationToken token, ConcurrentQueue<FitnessEvaluation>[] migrationQueues)
        {

            if (!_solved)
            {
                Console.WriteLine($"Commencing evolutionary process of population {_index}");
            }

            int generation = 0;
            int maxMigrationQIndex = migrationQueues.Length - 1;
            while (!_solved && !token.IsCancellationRequested)
            {
                Console.Out.WriteLine($"Evolving generation {++generation} of population {_index}");
                for (int newIndivids = 0; !_solved && !token.IsCancellationRequested && newIndivids < Gp.Popsize; ++newIndivids)
                {
                    var stringBuilder = new StringBuilder();
                    FitnessEvaluation offspring = null;
                    // Always look at one random migration queue other than this one and try to take the item.
                    int migrationQueueIndex = rd.Next(maxMigrationQIndex);

                    int index = -1;

                    if (!(migrationQueueIndex != _index &&
                          migrationQueues[migrationQueueIndex].TryDequeue(out offspring)))
                    {
                        // We did not get one form that queue. Try to evolve one.
                        Program newind;
                        if (rd.NextDouble() < Gp._crossoverProb)
                        {
                            FitnessEvaluation parent1 = _population[Tournament(rd, Gp.Tsize)];
                            FitnessEvaluation parent2 = _population[Tournament(rd, Gp.Tsize)];

                            newind = parent1.Program.Crossover(rd, parent2.Program);
                        }
                        else
                        {
                            newind = _population[Tournament(rd, Gp.Tsize)].Program.Mutate(rd, Gp._pmutPerNode);
                        }

                        offspring = new FitnessEvaluation(newind, _problem);
                        offspring.Evaluate(stringBuilder);

                        if (rd.NextDouble() < Gp._migrationProb)
                        {
                            if (migrationQueues[_index].Count < 10)
                            {
                                // Find *good* individual to move out to the migration queue. It is being replaced with the new one.
                                index = Tournament(rd, Gp.Tsize);
                                migrationQueues[_index].Enqueue(this._population[index]);
                            }
                        }
                    }

                    if (index < 0)
                    {
                        index = NegativeTournament(rd, Gp.Tsize, offspring);
                    }
                    this._population[index] = offspring;

                    OnFitness(offspring, stringBuilder);
                    if (stringBuilder.Length > 0)
                    {
                        stringBuilder.Insert(0, "\n");
                        Console.Out.WriteLine(stringBuilder);
                    }
                }
            }

            Console.Out.WriteLine($"Finishing evolution of population {_index}");
        }

        public int Tournament(Random rd, int tsize)
        {
            int index = rd.Next(_population.Count);
            FitnessEvaluation bestFitness = _population[index];
            for (int i = 0; i < tsize; ++i)
            {
                int competitorIndex = rd.Next(_population.Count);
                FitnessEvaluation competitor = _population[competitorIndex];
                if (competitor.BetterThan(bestFitness))
                {
                    bestFitness = competitor;
                    index = competitorIndex;
                }
            }

            return index;
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