using System;
using System.Collections.Generic;
using System.Threading;

namespace gp
{
    internal class FitnessEvaluationQueue
    {
        private readonly Queue<FitnessEvaluation> _fitnessEvaluations = new Queue<FitnessEvaluation>();
        private readonly Thread _thread;
        private bool _stopped;
        public delegate void OnFitnessCalculated(FitnessEvaluation fitnessEvaluation);
        private readonly OnFitnessCalculated _onFitnessCalculated;

        public FitnessEvaluationQueue(OnFitnessCalculated onFitnessCalculated)
        {
            _onFitnessCalculated = onFitnessCalculated;
            _thread = new Thread(Run);
            _thread.Start();
        }

        private readonly object _tickTournamentLockLock = new object();
        public object TickTournamentLock
        { 
            get
            {
                return _tickTournamentLockLock;
            }
        }

        public void Run()
        {
            while (!Tick())
            {
            }
        }

        private bool Tick()
        {
            bool stopped;
            FitnessEvaluation fitnessEvaluation;
            lock (_fitnessEvaluations)
            {
                while (!_stopped && _fitnessEvaluations.Count == 0)
                {
                    try
                    {
                        Monitor.Wait(_fitnessEvaluations);
                    }
                    catch (ThreadInterruptedException)
                    {
                    }
                }

                fitnessEvaluation = _stopped ? null : _fitnessEvaluations.Dequeue();
                stopped = _stopped;
            }

            if (fitnessEvaluation != null)
            {
                Console.WriteLine("After Dequeueing: _fitessEvaluations.Count = {0}", _fitnessEvaluations.Count);
                Tick(fitnessEvaluation);
            }
            return stopped;
        }

        private void Tick(FitnessEvaluation fitnessEvaluation)
        {
            lock (fitnessEvaluation)
            {
                if (fitnessEvaluation.Queued)
                {
                    bool ticked;
                    lock (TickTournamentLock) //Has to wait for a tournament to complete before updating any fitness
                    {
                        ticked = fitnessEvaluation.Tick();
                    }
                    if (ticked)
                    {
                        fitnessEvaluation.Queued = false;
                        Console.WriteLine("Caclulated fitness {0}", fitnessEvaluation.SquaredErrorSum);
                        _onFitnessCalculated(fitnessEvaluation);
                    }
                    else if (fitnessEvaluation.Queued)
                    {
                        lock (_fitnessEvaluations)
                        {
                            _fitnessEvaluations.Enqueue(fitnessEvaluation);
                            Console.WriteLine("After Enqueueing again: _fitessEvaluations.Count = {0}", _fitnessEvaluations.Count);
                        }
                    }
                }
            }
        }

        public void Enqueue(FitnessEvaluation fitnessEvaluation)
        {
            lock (fitnessEvaluation)
            {
                if (fitnessEvaluation.Tick())
                {
                    fitnessEvaluation.Queued = false;
                    //Console.WriteLine("Caclulated fitness without enqueuing {0}", fitnessEvaluation.SquaredErrorSum);
                    _onFitnessCalculated(fitnessEvaluation);
                }
                else if (!fitnessEvaluation.Queued)
                {
                    fitnessEvaluation.Queued = true;
                    lock (_fitnessEvaluations)
                    {
                        _fitnessEvaluations.Enqueue(fitnessEvaluation);
                        Monitor.PulseAll(_fitnessEvaluations);
                        Console.WriteLine("After Enqueueing: _fintessEvaluations.Count = {0}", _fitnessEvaluations.Count);
                    }
                }
            }
        }

        public void Stop()
        {
            lock (_fitnessEvaluations)
            {
                _stopped = true;
                Monitor.PulseAll(_fitnessEvaluations);
            }
            _thread.Join(10000);
        }
    }
}