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
        private double? _squaredErrorSum;
        private int _nullResultCount;
        private Program _program;
        private RuntimeState[] _runtimeStates;
        private int _tickCount;
        private readonly double[] _results;

        public FitnessEvaluation(Program program, Problem problem)
        {
            _problem = problem;
            _results = new double[problem.Count];
            Program = program;
        }

        public FitnessEvaluation(FitnessEvaluation evaluation)
        {
            _program = new Program(evaluation._program);
            _problem = evaluation.Problem;
            _results = (double[]) evaluation._results.Clone();
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
            _squaredErrorSum = evaluation._squaredErrorSum;
        }

        public bool Queued { get; set; }

        public double? SquaredErrorSum => this._squaredErrorSum;

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
                    _squaredErrorSum = 0;
                    Queued = false;
                }
            }
        }

        public Problem Problem => _problem;

        public double[] Results => _results;

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
            return Double.IsNaN(Problem[index][Program.Varnumber]);
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

        private void DebugTarget(double[] inputs,
                                 double expectedResult,
                                 double? resultBeforeSimplification,
                                 double result,
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
                    Console.Write(inputs[i]);
                }

                if (resultBeforeSimplification == null)
                {
                    Console.WriteLine("] = {0} expected result = {1}", result,
                        Double.IsNaN(expectedResult)
                            ? "?"
                            : expectedResult.ToString(CultureInfo.GetCultureInfo("en-GB")
                                .NumberFormat));
                }
                else if (printResult || result != resultBeforeSimplification)
                {
                    Console.WriteLine("] = {0} but before simplification = {1}, expected result = {2}",
                        result,
                        resultBeforeSimplification,
                        Double.IsNaN(expectedResult)
                            ? "?"
                            : expectedResult.ToString(CultureInfo.GetCultureInfo("en-GB")
                                .NumberFormat));
                }
            }
        }

        public bool Tick(bool printResult, bool evaluateUnknownTargets, double[] resultsBeforeSimplification)
        {
            bool everyTargetEvaluated = true;
            bool evaluatedNewTarget = false;

            for (int targetIndex = 0; targetIndex < _problem.Count; ++targetIndex)
            {
                if (!IsUnknownTarget(targetIndex) || //If we know  the expeted output, evaluate.
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
                                _problem[targetIndex][Problem.Varnumber],
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
                    _squaredErrorSum = 0.0;
                    for (int targetIndex = 0; targetIndex < _problem.Count; ++targetIndex)
                    {
                        double expectedResult = Problem[targetIndex][Program.Varnumber];

                        CallTree callTree = GetCallTree(targetIndex);
                        if (Double.IsNaN(callTree.Result))
                        {
                            //Not good.
                            ++_nullResultCount;
                        }
                        else if (!Double.IsNaN(expectedResult))
                        {
                            //Use the sum of the squared errors
                            _squaredErrorSum +=
                                Math.Pow(callTree.Result - Problem[targetIndex][Program.Varnumber], 2.0);
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

        public int CompareFitness(FitnessEvaluation fitnessEvaluation)
        {
            if (fitnessEvaluation.SquaredErrorSum == null)
            {
                return Int32.MinValue;
            }

            if (double.IsNaN(fitnessEvaluation.SquaredErrorSum.Value))
            {
                return Int32.MinValue;
            }

            int sum = WeightedComparison(SquaredErrorSum, fitnessEvaluation.SquaredErrorSum, 10)
                      + WeightedComparison(_tickCount, fitnessEvaluation._tickCount, 5)
                      + WeightedComparison(_nullResultCount, fitnessEvaluation._nullResultCount, 50)
                      + WeightedComparison(_program.Code.Count, fitnessEvaluation.Program.Code.Count, 1);
            return sum;
        }
    }
}