using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace gp
{
    public class FitnessEvaluation
    {
        private readonly Problem _problem;
        private CallTree[] _callTrees;
        private decimal? _errorSum;
        private int _nullResultCount;
        private Program _program;
        private RuntimeState[] _runtimeStates;
        private int _tickCount;
        private readonly decimal[] _results;

        public FitnessEvaluation(Program program, Problem problem)
        {
            _problem = problem;
            _results = new decimal[problem.Count];
            Program = program;
        }

        public FitnessEvaluation(FitnessEvaluation evaluation)
        {
            _program = new Program(evaluation._program);
            _problem = evaluation.Problem;
            _results = (decimal[]) evaluation._results.Clone();
            _callTrees = new CallTree[_problem.Count];
            _tickCount = evaluation._tickCount;
            _runtimeStates = new RuntimeState[_problem.Count];
            for (int i = 0; i < evaluation._runtimeStates.Length; ++i)
            {
                if (evaluation._runtimeStates[i] != null)
                {
                    _runtimeStates[i] = new RuntimeState(evaluation._runtimeStates[i]);
                }
            }
            _nullResultCount = evaluation._nullResultCount;
            _errorSum = evaluation._errorSum;
        }

        public bool Queued { get; set; }

        public decimal? ErrorSum => this._errorSum;

        public Program Program
        {
            get => _program;
            set
            {
                if (_program != value)
                {
                    _program = value;
                    _runtimeStates = new RuntimeState[_problem.Count];
                    _callTrees = new CallTree[_problem.Count];
                    _tickCount = 0;
                    _nullResultCount = 0;
                    _errorSum = 0;
                    Queued = false;
                }
            }
        }

        public Problem Problem => _problem;

        public decimal[] Results => _results;

        public bool Tick()
        {
            return Tick(false);
        }

        public bool Tick(bool printResult)
        {
            return Tick(printResult, false);
        }

        public bool Tick(bool printResult, bool evaluateUnknownTargets)
        {
            return Tick(printResult, evaluateUnknownTargets, null);
        }

        bool IsUnknownTarget(int index)
        {
            return !Problem[index].ExpectedResult.HasValue;
        }

        private RuntimeState GetRuntimeState(int index)
        {
            if (_runtimeStates[index] == null)
            {
                _runtimeStates[index] = new RuntimeState(Program.Varnumber);
                for (int i = 0; i < Program.Varnumber; ++i)
                {
                    _runtimeStates[index].Inputs[i] = Problem[index][i];
                }
            }
            return _runtimeStates[index];
        }

        private CallTree GetCallTree(int index)
        {
            if (_callTrees[index] == null)
            {
                int pc = 0;
                _callTrees[index] = new CallTree(_program, ref pc);
            }
            return _callTrees[index];
        }

        private void DebugTarget(decimal[] inputs,
                                 decimal? expectedResult,
                                 decimal? resultBeforeSimplification,
                                 decimal result,
                                 bool printResult)
        {
            if (printResult || (resultBeforeSimplification != null && resultBeforeSimplification.Value != result))
            {
                Console.Write("[");
                for (int i = 0; i < inputs.Length; ++i)
                {
                    if (i > 0)
                    {
                        Console.Write(",");
                    }
                    Console.Write(inputs[i].ToString(CultureInfo.GetCultureInfo("en-GB")
                        .NumberFormat));
                }

                if (resultBeforeSimplification == null)
                {
                    Console.WriteLine("] = {0} expected result = {1}", result,
                        expectedResult == null
                            ? "?"
                            : expectedResult.Value.ToString(CultureInfo.GetCultureInfo("en-GB")
                                .NumberFormat));
                }
                else if (printResult || result != resultBeforeSimplification)
                {
                    Console.WriteLine("] = {0} but before simplification = {1}, expected result = {2}",
                        result,
                        resultBeforeSimplification,
                        expectedResult == null
                            ? "?"
                            : expectedResult.Value.ToString(CultureInfo.GetCultureInfo("en-GB")
                                .NumberFormat));
                }
            }
        }

        public void PrintAllResults()
        {
            for (int targetIndex = 0; targetIndex < _problem.Targets.Count; ++targetIndex)
            {
                var target = _problem.Targets[targetIndex];

                var expectedResult = target.ExpectedResult;
                Console.Write("[");
                for (int i = 0; i < _problem.Varnumber; ++i)
                {
                    if (i > 0)
                    {
                        Console.Write(",");
                    }

                    Console.Write(target[i].ToString(CultureInfo.GetCultureInfo("en-GB")
                        .NumberFormat));
                }


                Console.WriteLine("] = {0} expected result = {1}", Results[targetIndex],
                    expectedResult == null
                        ? "?"
                        : expectedResult.Value.ToString(CultureInfo.GetCultureInfo("en-GB")
                            .NumberFormat));
            }
        }

        public bool Tick(bool printResult, bool evaluateUnknownTargets, decimal[] resultsBeforeSimplification)
        {
            bool everyTargetEvaluated = true;
            bool evaluatedNewTarget = false;

            for (int targetIndex = 0; targetIndex < _problem.Count; ++targetIndex)
            {
                if (!IsUnknownTarget(targetIndex) || //If we know  the excpeted output, evaluate.
                    printResult || //If we're just debugging, evaluate
                    evaluateUnknownTargets || //If the expected output is unknown, but we stil want to evaluate, then do
                    resultsBeforeSimplification != null
                ) //If we  want to compare with a previous result, then evaluate unknown targets, too.
                {
                    RuntimeState runtimeState = GetRuntimeState(targetIndex);
                    CallTree callTree = GetCallTree(targetIndex);

                    if (!callTree.Evaluated)
                    {
                        ++_tickCount;
                        if (!callTree.Tick(runtimeState, Program.Constants))
                        {
                            everyTargetEvaluated = false;
                        }
                        else
                        {
                            evaluatedNewTarget = true;
                            _results[targetIndex] = callTree.Result;

                            DebugTarget(runtimeState.Inputs,
                                _problem[targetIndex].ExpectedResult,
                                resultsBeforeSimplification?[targetIndex],
                                callTree.Result,
                                printResult);
                        }
                    }
                }
            }

            if (everyTargetEvaluated)
            {
                Queued = false;
                if (evaluatedNewTarget)
                {
                    _nullResultCount = 0;
                    _errorSum = 0.0m;
                    for (int targetIndex = 0; targetIndex < _problem.Count; ++targetIndex)
                    {
                        var expectedResult = Problem[targetIndex].ExpectedResult;

                        CallTree callTree = GetCallTree(targetIndex);
                       if (expectedResult.HasValue)
                        {
                            //Use the sum of the errors
                            _errorSum +=
                                Math.Abs(callTree.Result - expectedResult.Value);
                        }
                    }

                }
            }
            return everyTargetEvaluated;
        }

        private static int WeightedComparison(int lhs, int rhs, int weight)
        {
            if (lhs < rhs)
            {
                return -weight;
            }

            if (rhs < lhs)
            {
                return weight;
            }
            return 0;
        }

        private static int WeightedComparison(double? lhs, double? rhs, int weight)
        {
            if (lhs == null)
            {
                if (rhs == null)
                {
                    return 0;
                }
                return -1;
            }

            if (rhs == null)
            {
                return 1;
            }

            if (lhs < rhs)
            {
                return -weight;
            }

            if (lhs > rhs)
            {
                return weight;
            }
            return 0;
        }

        public bool WorseThan(FitnessEvaluation fitnessEvaluation)
        {
            return CompareFitness(fitnessEvaluation) > 0;
        }

        public bool BetterThan(FitnessEvaluation fitnessEvaluation)
        {
            return CompareFitness(fitnessEvaluation) < 0;
        }

        private int CompareFitness(FitnessEvaluation fitnessEvaluation)
        {
            if (fitnessEvaluation.ErrorSum == null)
            {
                return Int32.MinValue;
            }

            if (_nullResultCount == fitnessEvaluation._nullResultCount)
            {
                if (ErrorSum == fitnessEvaluation.ErrorSum)
                {
                    return _program.Code.Count.CompareTo(fitnessEvaluation.Program.Code.Count);
                }

                return ErrorSum.Value.CompareTo(fitnessEvaluation.ErrorSum.Value);
            }

            return _nullResultCount.CompareTo(fitnessEvaluation._nullResultCount);

            /*int sum = WeightedComparison(SquaredErrorSum, fitnessEvaluation.SquaredErrorSum, 10)
                      + WeightedComparison(_tickCount, fitnessEvaluation._tickCount, 5)
                      + WeightedComparison(_nullResultCount, fitnessEvaluation._nullResultCount, 50)
                      + WeightedComparison(_program.Code.Count, fitnessEvaluation.Program.Code.Count, 1);
            return sum;*/
        }

        public bool EqualResults(FitnessEvaluation other)
        {
            if (Math.Abs(ErrorSum.Value - other.ErrorSum.Value) > 1e-5m)
            {
                return false;
            }

            for (int i = 0; i < Results.Length; ++i)
            {
                if (Math.Abs(Results[i] - other.Results[i]) > 1e-5m * Results.Length)
                {
                    return false;
                }
            }

            return true;
        }
    }
}