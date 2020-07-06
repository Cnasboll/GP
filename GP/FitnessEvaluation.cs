using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Common;

namespace gp
{
    public class FitnessEvaluation
    {
        private readonly Problem _problem;
        private decimal? _errorSum;
        private Program _program;
        private readonly decimal[] _results;

        public FitnessEvaluation(Program program, Problem problem)
        {
            _problem = problem;
            _results = new decimal[problem.Count];
            Program = program;
        }

        public decimal? ErrorSum => this._errorSum;

        public Program Program
        {
            get => _program;
            set
            {
                if (_program != value)
                {
                    _program = value;
                    _errorSum = 0;
                }
            }
        }

        public Problem Problem => _problem;

        public decimal[] Results => _results;

        public void Evaluate()
        {
            Evaluate(false);
        }

        public void Evaluate(bool printResult)
        {
            Evaluate(printResult, false);
        }

        public void Evaluate(bool printResult, bool evaluateUnknownTargets)
        {
            Evaluate(printResult, evaluateUnknownTargets, null);
        }

        bool IsUnknownTarget(int index)
        {
            return !Problem[index].ExpectedResult.HasValue;
        }

        private RuntimeState GetRuntimeState(int index)
        {
            var runtimeState = new RuntimeState(Program.Varnumber); 
            for (int i = 0; i < Program.Varnumber; ++i) 
            { 
                runtimeState.Inputs[i] = Problem[index][i];
            }

            return runtimeState;
        }

        decimal Run(RuntimeState runtimeState)
        {
            Program.DecodeSymbol(Problem.Varnumber,
                Program.Code[runtimeState.Pc++],
                out var _symbol, out var _qualifier);

            switch (_symbol)
            {
                //Identifier operations of increasing arity:
                case Symbols.InputArgument:
                {
                    //Evaluates to one of the input arguments
                    return runtimeState.Inputs[_qualifier];
                }
                case Symbols.IntegerLiteral:
                {
                    return Program.Constants.Integers[_qualifier];
                }
                case Symbols.DoubleLiteral:
                {
                    //Evaluates to one of the constants. Behaves like the input arguments for
                    //the evaluation point of view but are not given as input, instead they evolve
                    //together with the rest of the _code.
                    return Program.Constants.Doubles[_qualifier];
                }
                case Symbols.WorkingVariable:
                {
                    return runtimeState.Variables[_qualifier];
                }
                case Symbols.AssignWorkingVariable:
                {
                    // Assingns the given working variable to the RHS
                    var operand = Run(runtimeState);
                    runtimeState.Variables[_qualifier] = operand;

                    //The result is always the RHS (even if the variable does not change),
                    //this is for not changing the program by removing the assignment
                    return operand;
                }
                //Unary operators:
                case Symbols.Not:
                {
                    return (int) Run(runtimeState) != 0 ? 1 : 0;
                }
                //Binary operators:
                case Symbols.Add:
                {
                    return Run(runtimeState) + Run(runtimeState);
                }
                case Symbols.Sub:
                {
                    return Run(runtimeState) - Run(runtimeState);
                }
                case Symbols.Mul:
                {
                    return Run(runtimeState) * Run(runtimeState);
                }
                case Symbols.Div:
                {
                    var lhs = Run(runtimeState);
                    var rhs = Run(runtimeState);
                    if (Math.Abs(rhs) >= 0.001m)
                    {
                        try
                        {
                            return lhs / rhs;
                        }
                        catch (DivideByZeroException)
                        {
                        }
                    }

                    // We treat division by zero as division by one
                    return lhs;

                }
                case Symbols.Lt:
                {
                    return Run(runtimeState) < Run(runtimeState) ? 1 : 0;
                }
                case Symbols.Lteq:
                {
                    return Run(runtimeState) <= Run(runtimeState) ? 1 : 0;
                }
                case Symbols.Gt:
                {
                    return Run(runtimeState) > Run(runtimeState) ? 1 : 0;
                }
                case Symbols.Gteq:
                {
                    return Run(runtimeState) >= Run(runtimeState) ? 1 : 0;
                }
                case Symbols.Eq:
                {
                    return Run(runtimeState) == Run(runtimeState) ? 1 : 0;
                }
                case Symbols.Neq:
                {
                    return Run(runtimeState) != Run(runtimeState) ? 1 : 0;
                }
                case Symbols.And:
                {
                    var lhs = Run(runtimeState);
                    if ((int) lhs == 0)
                    {
                        //short circuit
                        runtimeState.Pc = _program.Traverse(runtimeState.Pc);
                        return 0;
                    }

                    var rhs = Run(runtimeState);

                    return (int) rhs != 0 ? 1 : 0;
                }
                case Symbols.Or:
                {
                    var lhs = Run(runtimeState);
                    if ((int) lhs != 0)
                    {
                        //short circuit
                        runtimeState.Pc = _program.Traverse(runtimeState.Pc);
                        return 1;
                    }

                    var rhs = Run(runtimeState);

                    return (int) rhs != 0 ? 1 : 0;
                }
                case Symbols.Xor:
                {
                    var lhs = (int) Run(runtimeState) != 0;
                    var rhs = (int) Run(runtimeState) != 0;
                    return lhs != rhs ? 1 : 0;
                }

                case Symbols.Chain:
                {
                    //Ignore the first result and return the second. This can build blocks.
                    Run(runtimeState);
                    return Run(runtimeState);
                }
                case Symbols.If:
                {
                    var guard = (int) Run(runtimeState) != 0;
                    if (guard)
                    {
                        //Evaluate and return the yes branch
                        return Run(runtimeState);
                    }
                    else
                    {
                        //Skip the yes branch, return 0
                        runtimeState.Pc = _program.Traverse(runtimeState.Pc);
                        return 0;
                    }
                }
                case Symbols.Mov:
                {
                    //Like assign but without using qualifier for the destination address, instead
                    //it is calculated from the LHS expression.
                    //Can be used to implement arrays such as in the following example
                    //where x[2]..x[2+x[1]-1] constitutes an array of length x[1].
                    /*
                     *  let[0] lit[0]                       #   x[0] = 0;
                     *  let[1] lit[2]                       #   x[1] = 2;
                     *  while lt var[0] var[1]              #   while (x[0] < x[1])
                     *  chain                               #   {
                     *      mov add lit[2] var[0] var[0]    #       x[2+x0] = x0;
                     *      let[0] add var[0] lit[1]        #       ++x[0];
                     *                                      #   }                        
                    */
                    var index = Run(runtimeState);
                    var rhs = Run(runtimeState);
                    runtimeState.Variables[(int) Math.Abs(index)] = rhs;
                    //The result is always the RHS (even if the variable does not change),
                    //this is for not changing the program by removing the assignment
                    return rhs;
                }
                //Ternary operator(s):
                case Symbols.Ifelse:
                {
                    var guard = (int) Run(runtimeState) != 0;


                    if (guard)
                    {
                        //Evaluate and return the yes branch
                        var result = Run(runtimeState);

                        //Skip the no branch
                        runtimeState.Pc = _program.Traverse(runtimeState.Pc);

                        return result;
                    }

                    //Skip the yes branch
                    runtimeState.Pc = _program.Traverse(runtimeState.Pc);

                    // Evaluate and return the no branch
                    return Run(runtimeState);
                }
                case Symbols.Noop:
                {
                    return 0;
                }
                default:
                {
                    throw new InvalidConstraintException("Invalid symbol " + _symbol + " found at runtime");
                }
            }
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

        public void Evaluate(bool printResult, bool evaluateUnknownTargets, decimal[] resultsBeforeSimplification)
        {
            for (int targetIndex = 0; targetIndex < _problem.Count; ++targetIndex)
            {
                if (!IsUnknownTarget(targetIndex) || //If we know  the excpeted output, evaluate.
                    printResult || //If we're just debugging, evaluate
                    evaluateUnknownTargets || //If the expected output is unknown, but we stil want to evaluate, then do
                    resultsBeforeSimplification != null
                ) //If we  want to compare with a previous result, then evaluate unknown targets, too.
                {
                    RuntimeState runtimeState = GetRuntimeState(targetIndex);

                    _results[targetIndex] = Run(runtimeState);
                    //Use the sum of the errors
                    var expectedResult = Problem[targetIndex].ExpectedResult;
                    if (expectedResult.HasValue)
                    {
                        var error = _results[targetIndex] - expectedResult.Value;
                        _errorSum += Math.Abs(error);
                    }


                    DebugTarget(runtimeState.Inputs,
                        _problem[targetIndex].ExpectedResult,
                        resultsBeforeSimplification?[targetIndex],
                        _results[targetIndex],
                        printResult);
                }
            }
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

            if (ErrorSum == fitnessEvaluation.ErrorSum)
            {
                return _program.Code.Count.CompareTo(fitnessEvaluation.Program.Code.Count);
            }

            return ErrorSum.Value.CompareTo(fitnessEvaluation.ErrorSum.Value);
                

            /*int sum = WeightedComparison(SquaredErrorSum, fitnessEvaluation.SquaredErrorSum, 10)
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