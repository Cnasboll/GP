using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Common;
using log4net;

namespace gp
{
    public class CallTree
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CallTree));
        private const PossibleSign AllSigns =
            PossibleSign.Positive | PossibleSign.Negative | PossibleSign.Zero;

        private const PossibleSign BooleanSigns = PossibleSign.Positive | PossibleSign.Zero;

        private readonly CallTree[] _conditionalChildren;
        private readonly CallTree[] _preEvaluatedArgs;
        private readonly int _qualifier;
        private readonly Symbols _symbol;
        private readonly int _varnumber;
        public static bool PrintDebugInfo;
        private int _conditionalChildrenIndex;
        private int _preEvaluatedArgIndex;

        private decimal _result = 0;
        private bool _evaluated = false;
        private PossibleSign _sign;

        public CallTree(Program program, ref int pc) : this(program.Varnumber, program.Code, ref pc)
        {
        }

        public CallTree(int varnumber, IList<int> code, ref int pc)
        {
            _varnumber = varnumber;
            Program.DecodeSymbol(varnumber, code[pc++], out _symbol, out _qualifier);

            //We are going to first evaluate the dependent arguments.
            _preEvaluatedArgs = new CallTree[Program.GetDependencyArity(_symbol)];
            for (int i = 0; i < _preEvaluatedArgs.Length; ++i)
            {
                _preEvaluatedArgs[i] = new CallTree(varnumber, code, ref pc);
            }

            _conditionalChildren = new CallTree[Program.GetSyntacticArity(_symbol) - _preEvaluatedArgs.Length];
            for (int i = 0; i < _conditionalChildren.Length; ++i)
            {
                _conditionalChildren[i] = new CallTree(varnumber, code, ref pc);
            }
        }

        public CallTree(CallTree callTree)
        {
            _varnumber = callTree._varnumber;
            _symbol = callTree._symbol;
            _qualifier = callTree._qualifier;
            //We are going to first evaluate the dependent arguments.
            _preEvaluatedArgs = new CallTree[Program.GetDependencyArity(_symbol)];
            for (int i = 0; i < _preEvaluatedArgs.Length; ++i)
            {
                _preEvaluatedArgs[i] = new CallTree(callTree._preEvaluatedArgs[i]);
            }

            _conditionalChildren = new CallTree[Program.GetSyntacticArity(_symbol) - _preEvaluatedArgs.Length];
            for (int i = 0; i < _conditionalChildren.Length; ++i)
            {
                _conditionalChildren[i] = new CallTree(callTree._conditionalChildren[i]);
            }

            _preEvaluatedArgIndex = callTree._preEvaluatedArgIndex;
            _conditionalChildrenIndex = callTree._conditionalChildrenIndex;
            _result = callTree._result;
        }

        private CallTree(Symbols symbol, int varnumber) : this(symbol, varnumber, 0)
        {
        }

        private CallTree(Symbols symbol, int varnumber, CallTree[] preEvaluatedArgs)
            : this(symbol, varnumber, preEvaluatedArgs, new CallTree[] {})
        {
        }

        private CallTree(Symbols symbol, int varnumber, CallTree[] preEvaluatedArgs,
                         CallTree[] conditionalChildren)
            : this(symbol, varnumber, 0, preEvaluatedArgs, conditionalChildren)
        {
        }

        private CallTree(Symbols symbol, int varnumber, int qualifier)
            : this(symbol, varnumber, qualifier, new CallTree[] {}, new CallTree[] {})
        {
        }

        private CallTree(Symbols symbol, int varnumber, int qualifier, CallTree[] preEvaluatedArgs,
                         CallTree[] conditionalChildren)
        {
            _symbol = symbol;
            _qualifier = qualifier;
            _varnumber = varnumber;
            _preEvaluatedArgs = preEvaluatedArgs;
            _conditionalChildren = conditionalChildren;
        }

        public bool Evaluated => this._evaluated;

        public decimal Result => _result;

        public bool Bresult
        {
            get => IResult != 0;
            set => IResult = value ? 1 : 0;
        }

        public int IResult
        {
            get => (int) _result;
            set => _result = value;
        }

        public bool IsConstant
        {
            get
            {
                switch (_symbol)
                {
                    case Symbols.IntegerLiteral:
                    case Symbols.DoubleLiteral:
                    case Symbols.Noop:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool HasAssignment
        {
            get
            {
                switch (_symbol)
                {
                    case Symbols.AssignWorkingVariable:
                        return true;
                    case Symbols.Mov:
                        return true;
                    default:
                        {
                            foreach (CallTree callTree in _preEvaluatedArgs)
                            {
                                if (callTree.HasAssignment)
                                {
                                    return true;
                                }
                            }
                            foreach (CallTree callTree in _conditionalChildren)
                            {
                                if (callTree.HasAssignment)
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                }
            }
        }

        private bool HasWhileLoop
        {
            get
            {
                switch (_symbol)
                {
                    case Symbols.While:
                        return true;
                    default:
                        {
                            foreach (CallTree callTree in _preEvaluatedArgs)
                            {
                                if (callTree.HasWhileLoop)
                                {
                                    return true;
                                }
                            }
                            foreach (CallTree callTree in _conditionalChildren)
                            {
                                if (callTree.HasWhileLoop)
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                }
            }
        }

        private bool IsCommutative
        {
            get
            {
                switch (_symbol)
                {
                    case Symbols.Add: //x+y = y+x
                    case Symbols.And: //x AND y = y AND x
                    case Symbols.Eq: //(x = y) = (y = x)
                    case Symbols.Mul: //(x * y) = (y * x)
                    case Symbols.Neq: //(x <> y) = (y <> x)
                    case Symbols.Or: //(x OR y) = (y OR x)
                    case Symbols.Xor: //(x XOR y) = (y XOR x)
                        return true;
                    default:
                        return false;
                }
            }
        }

        private bool IsAssociative
        {
            get
            {
                switch (_symbol)
                {
                    case Symbols.Add:
                    case Symbols.And:
                    case Symbols.Or:
                    case Symbols.Mul:
                        return true;
                    default:
                        return false;
                }
            }
        }

        private bool HasConditionalChildren
        {
            get { return Program.GetSyntacticArity(_symbol) > Program.GetDependencyArity(_symbol); }
        }

        /// <summary>
        /// True if result of all pre evaluated args are in range 0, 1
        /// </summary>
        private bool IsBoolean
        {
            get
            {
                switch (_symbol)
                {
                    case Symbols.And:
                    case Symbols.Eq:
                    case Symbols.Gt:
                    case Symbols.Gteq:
                    case Symbols.Lt:
                    case Symbols.Lteq:
                    case Symbols.Neq:
                    case Symbols.Not:
                    case Symbols.Or:
                    case Symbols.Xor:
                        return true;
                    default:
                        return false;
                }
            }
        }

        private bool TakesBooleanPreEvaluatedArgs
        {
            get
            {
                switch (_symbol)
                {
                    case Symbols.And:
                    case Symbols.If:
                    case Symbols.Ifelse:
                    case Symbols.Or:
                    case Symbols.Xor:
                        return true;
                    default:
                        return false;
                }
            }
        }

        private bool TakesBooleanConditionalChildren
        {
            get
            {
                switch (_symbol)
                {
                    case Symbols.And:
                    case Symbols.Or:
                    case Symbols.While:
                        return true;
                    default:
                        return false;
                }
            }
        }

        private bool LacksSideEffects => IsConstant || (!HasAssignment && !HasWhileLoop);

        private CallTree Operand
        {
            get
            {
                if (Program.GetSyntacticArity(_symbol) != 1)
                {
                    throw new InvalidOperationException(_symbol + " is not a unary operator");
                }

                return _preEvaluatedArgs[0];
            }
        }

        private CallTree Lhs
        {
            get
            {
                if (Program.GetSyntacticArity(_symbol) != 2)
                {
                    throw new InvalidOperationException(_symbol + " is not a binary operator");
                }

                return _preEvaluatedArgs[0];
            }
        }

        private CallTree Rhs
        {
            get
            {
                if (Program.GetSyntacticArity(_symbol) != 2)
                {
                    throw new InvalidOperationException(_symbol + " is not a binary operator");
                }

                return _preEvaluatedArgs.Length == 2 ? _preEvaluatedArgs[1] : _conditionalChildren[0];
            }
        }

        /// <summary>
        /// For commutative operations the program will maintain the exact same meaning if we swap RHS with LHS if both lack side effects.
        /// If ONLY RHS has side effects swapping will maintain semantics if at least one side is but symbol cannot be short circuited
        /// i.e. being neither OR or AND, so both sides were already guaranteed to be evaluated but the evaluation of LHS cannot affect the result of RHS.        
        /// </summary>
        private bool IsSemanticallyCommutative
        {
            get
            {
                if (!IsCommutative)
                {
                    //If the operator is not commutative then order has significance.
                    return false;
                }
                if (LacksSideEffects)
                {
                    //If neither side has side effects then semantics will not change even if the order changes.
                    return true;
                }
                if (HasConditionalChildren)
                {
                    //If any operand has side effects and operator might be short circuited (i.e And or Or),
                    //then we cannot swap them as altering the order might turn evaluation of RHS on or off,
                    //which alters the side effects from that operand(side effects will also be turned on or of)
                    return false;
                }
                //If one side has side effects but the other is neutral to side effects (has no side effects
                //and does not depende on them the side effects from the other side, i.e. operates only on constants
                //and input arguments), then we can still swap them!//i.e ((x0*x1)+(y0:=7)) has to be identical to
                //((y0:=7)+(x0*x1)) as (x0*x1) is neutral to side effects and does not have any side effects
                //(which could have changed the other term)
                return Lhs.SideEffectsNeutral || Rhs.SideEffectsNeutral;
            }
        }

        /// <summary>
        /// Checks if this expression will give the same value each time it is invoked, i.e. is 
        /// independent dependent from side effects
        /// </summary>
        private bool SideEffectsNeutral
        {
            get
            {
                if (IsConstant || _symbol == Symbols.InputArgument)
                {
                    //Constants or input variables are not dependent on side effects.
                    return true;
                }

                if (_symbol == Symbols.WorkingVariable)
                {
                    //Working variables are dependent on side effects
                    return false;
                }

                if (!LacksSideEffects)
                {
                    //If this has side effects it can obivously not be neutral to them.
                    return false;
                }

                foreach (CallTree callTree in _preEvaluatedArgs)
                {
                    if (!callTree.SideEffectsNeutral)
                    {
                        return false;
                    }
                }

                foreach (CallTree callTree in _conditionalChildren)
                {
                    if (!callTree.SideEffectsNeutral)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private bool IsRelational
        {
            get
            {
                switch (_symbol)
                {
                    case Symbols.Eq:
                    case Symbols.Gt:
                    case Symbols.Gteq:
                    case Symbols.Lt:
                    case Symbols.Lteq:
                    case Symbols.Neq:
                        return true;
                }
                return false;
            }
        }

        private PossibleSign Sign
        {
            get
            {
                if (_sign == 0)
                {
                    _sign = DetermineSign();
                }
                return _sign;
            }
        }

        private bool IsPositive => !SignPossible(Sign, PossibleSign.Zero | PossibleSign.Negative);

        private bool IsNegative => !SignPossible(Sign, PossibleSign.Zero | PossibleSign.Positive);

        private bool IsZero => Sign == PossibleSign.Zero;

        private bool IsArithmeticExpression
        {
            get
            {
                switch (_symbol)
                {
                    case Symbols.Add:
                    case Symbols.Sub:
                    case Symbols.Mul:
                    case Symbols.Div:
                        return true;
                }
                return false;
            }
        }

        private bool ResultIsNeverZero => !SignPossible(Sign, PossibleSign.Zero);


        private static bool SignPossible(PossibleSign possibleSigns, PossibleSign test)
        {
            return (possibleSigns & test) != 0;
        }

        private static bool SignsPossible(PossibleSign lhs, PossibleSign lhsTest, PossibleSign rhs, PossibleSign rhsTest)
        {
            return SignPossible(lhs, lhsTest) && SignPossible(rhs, rhsTest);
        }

        private static bool CommutativeSignsPossible(PossibleSign lhs, PossibleSign lhsTest, PossibleSign rhs,
                                                     PossibleSign rhsTest)
        {
            if (SignPossible(lhs, lhsTest) && SignPossible(rhs, rhsTest))
            {
                return true;
            }
            if (lhs != rhs || lhsTest != rhsTest)
            {
                if (SignPossible(rhs, rhsTest) && SignPossible(lhs, lhsTest))
                {
                    return true;
                }
            }
            return false;
        }

        private PossibleSign DetermineSign()
        {
            if (IsConstant)
            {
                //For constant expressions we can always determine sign
                if (Result < 0.0m)
                {
                    return PossibleSign.Negative;
                }
                if (Result > 0.0m)
                {
                    return PossibleSign.Positive;
                }
                return PossibleSign.Zero;
            }

            //For boolean arguments we do not need to analyze the arguments: Sign is 0 or 1.
            //If we can rule out either we have proven the whole argument is a constant and the simplification
            //rules will have replaced by such!
            if (IsBoolean)
            {
                return BooleanSigns;
            }

            switch (_symbol)
            {
                //An input argument or working variable can be zero, negative or positive
                case Symbols.InputArgument:
                case Symbols.WorkingVariable:
                    return AllSigns;

                //The assignment has the sign of the operand, so for the move and chain
                case Symbols.AssignWorkingVariable:
                    return Operand.Sign;
                case Symbols.Mov:
                case Symbols.Chain:
                    return Rhs.Sign;

                //An if-then-else has the same sign that both branches have
                case Symbols.Ifelse:
                    return _conditionalChildren[0].Sign | _conditionalChildren[1].Sign;
                //An if statement and a while loop either has the sign of the body (if guard is true), or Zero (if guard is false) 
                case Symbols.If:
                    {
                        // If the guard expression is false, if-then evaluates to zero.
                        PossibleSign possibleSigns = PossibleSign.Zero;

                        if ((_preEvaluatedArgs[0]._symbol == Symbols.Not &&
                             _preEvaluatedArgs[0].Operand.Equals(_conditionalChildren[0]))
                            ||
                            (_conditionalChildren[0]._symbol == Symbols.Not &&
                             _conditionalChildren[0].Operand.Equals(_preEvaluatedArgs[0])))
                        {
                            //If not X then X [else 0] => 0
                            //If x THEN not X [else 0] => 0
                            return possibleSigns;
                        }

                        possibleSigns |= _conditionalChildren[0].Sign;
                        return possibleSigns;
                    }
                case Symbols.While:
                    return _conditionalChildren[0].Sign | PossibleSign.Zero;
                case Symbols.Add:
                    {
                        PossibleSign lhsPossibleSign = Lhs.Sign;
                        PossibleSign rhsPossibleSign = Rhs.Sign;

                        PossibleSign possibleSigns = 0;
                        if (SignsPossible(lhsPossibleSign, PossibleSign.Positive, 
                            rhsPossibleSign, PossibleSign.Positive))
                        {
                            //Adding two positive numbers always lead to a positive number
                            possibleSigns |= PossibleSign.Positive;
                        }

                        if (CommutativeSignsPossible(lhsPossibleSign, PossibleSign.Negative, 
                            rhsPossibleSign, PossibleSign.Positive))
                        {
                            //Adding a negative and a positive number leads to any finite value.
                            possibleSigns |= AllSigns;
                        }

                        if (SignPossible(lhsPossibleSign, PossibleSign.Zero))
                        {
                            //Adding with zero will keep the sign of the other side.
                            possibleSigns |= rhsPossibleSign;
                        }

                        if (SignPossible(rhsPossibleSign, PossibleSign.Zero))
                        {
                            //Adding with zero will keep the sign of the other side.
                            possibleSigns |= lhsPossibleSign;
                        }

                        return possibleSigns;
                    }
                case Symbols.Sub:
                    {
                        PossibleSign lhsPossibleSign = Lhs.Sign;
                        PossibleSign rhsPossibleSign = Rhs.Sign;

                        PossibleSign possibleSigns = 0;
                        if (SignsPossible(lhsPossibleSign, PossibleSign.Positive, 
                            rhsPossibleSign, PossibleSign.Positive))
                        {
                            //Subtracting a positive number from another positive number leads to any finite value.
                            possibleSigns |= AllSigns;
                        }

                        if (SignsPossible(lhsPossibleSign, PossibleSign.Zero | PossibleSign.Negative,
                            rhsPossibleSign, PossibleSign.Positive))
                        {
                            //Subtracting a positive number from a non-positive number leads to a negative value.
                            possibleSigns |= PossibleSign.Negative;
                        }

                        if (SignsPossible(lhsPossibleSign, PossibleSign.Positive, 
                            rhsPossibleSign, PossibleSign.Negative))
                        {
                            //The difference between a positive number and a negative number is always positive
                            possibleSigns |= PossibleSign.Positive;
                        }

                        if (SignsPossible(lhsPossibleSign, PossibleSign.Zero, 
                            rhsPossibleSign, PossibleSign.Negative))
                        {
                            //Subtracting a negative number from zero leads to a positive value.
                            possibleSigns |= PossibleSign.Negative;
                        }

                        if (SignsPossible(lhsPossibleSign, PossibleSign.Negative, 
                            rhsPossibleSign, PossibleSign.Negative))
                        {
                            //Subtracting a negative number from a negative number leads to any finite value.
                            possibleSigns |= AllSigns;
                        }

                        if (SignPossible(rhsPossibleSign, PossibleSign.Zero))
                        {
                            //Subtracting zero will keep the sign of lhs
                            possibleSigns |= lhsPossibleSign;
                        }

                        return possibleSigns;
                    }
                case Symbols.Div:
                    {
                        PossibleSign lhsPossibleSign = Lhs.Sign;
                        PossibleSign rhsPossibleSign = Rhs.Sign;

                        PossibleSign possibleSigns = 0;

                        if (SignPossible(rhsPossibleSign, PossibleSign.Zero))
                        {
                            // Division by zero is treated as division by one: Keeps sign of lhs
                            possibleSigns |= lhsPossibleSign;
                        }

                        if (CommutativeSignsPossible(lhsPossibleSign, PossibleSign.Positive, 
                            rhsPossibleSign, PossibleSign.Negative))
                        {
                            //If denominator and divisor are on different sides of zero, result is negative
                            possibleSigns |= PossibleSign.Negative;
                        }

                        if (SignsPossible(lhsPossibleSign, PossibleSign.Positive, rhsPossibleSign, PossibleSign.Positive)
                            ||
                            SignsPossible(lhsPossibleSign, PossibleSign.Negative, rhsPossibleSign, PossibleSign.Negative))
                        {
                            //If denominator and divisor are on same side of zero, result is positive
                            possibleSigns |= PossibleSign.Positive;
                        }

                        if (SignsPossible(lhsPossibleSign, PossibleSign.Zero, rhsPossibleSign,
                                          PossibleSign.Positive | PossibleSign.Negative))
                        {
                            //If denominator is zero and divisor is either negative or positive, result is zero
                            possibleSigns |= PossibleSign.Zero;
                        }

                        return possibleSigns;
                    }
                case Symbols.Mul:
                    {
                        PossibleSign lhsPossibleSign = Lhs.Sign;
                        PossibleSign rhsPossibleSign = Rhs.Sign;

                        PossibleSign possibleSigns = 0;
                        if (CommutativeSignsPossible(lhsPossibleSign, PossibleSign.Zero,
                            rhsPossibleSign, AllSigns))
                        {
                            //Zero multiplied with a finite number is zero
                            //Assert.AreEqual(0.0, 0.0 * 0.0);
                            //Assert.AreEqual(0.0, 0.0 * 1.0);
                            //Assert.AreEqual(0.0, 0.0 * -1.0);
                            possibleSigns |= PossibleSign.Zero;
                        }
                        
                        if (CommutativeSignsPossible(lhsPossibleSign, PossibleSign.Positive,
                            rhsPossibleSign, PossibleSign.Negative))
                        {
                            //If both terms are on different sides of zero, result is negateve
                            possibleSigns |= PossibleSign.Negative;
                        }

                        if (SignsPossible(lhsPossibleSign, PossibleSign.Positive, rhsPossibleSign, PossibleSign.Positive)
                            ||
                            SignsPossible(lhsPossibleSign, PossibleSign.Negative, rhsPossibleSign, PossibleSign.Negative))
                        {
                            //If both terms same side of zero, result is positive
                            possibleSigns |= PossibleSign.Positive;
                        }

                        return possibleSigns;
                    }
            }

            return 0;
        }

        private bool TickNextConditionalChild(RuntimeState runtimeState, ConstantsSet constants, ref decimal result, StringBuilder stringBuilder)
        {
            CallTree conditionalChild;
            if (TickNextConditionalChild(runtimeState, constants, out conditionalChild, stringBuilder))
            {
                if (conditionalChild != null)
                {
                    result = conditionalChild.Result;
                }
                return true;
            }

            return false;
        }

        private bool TickNextConditionalChild(RuntimeState runtimeState,
                                              ConstantsSet constants,
                                              out CallTree conditionalChild,
                                              StringBuilder stringBuilder)
        {
            if (_conditionalChildrenIndex < _conditionalChildren.Length)
            {
                conditionalChild = _conditionalChildren[_conditionalChildrenIndex];

                if (conditionalChild.Tick(runtimeState, constants, stringBuilder))
                {
                    ++_conditionalChildrenIndex;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                conditionalChild = null;
            }
            return true;
        }

        private bool Tick(ConstantsSet constants, StringBuilder stringBuilder)
        {
            return Tick(null, constants, stringBuilder);
        }

        public bool Tick(RuntimeState runtimeState, ConstantsSet constants, StringBuilder stringBuilder)
        {
            if (this._evaluated)
            {
                return true;
            }

            this._evaluated = TickImpl(runtimeState, constants, stringBuilder);
            if (PrintDebugInfo && this._evaluated)
            {
                stringBuilder?.AppendLine($"_symbol: {_sign} SyntacticArity: {Program.GetSyntacticArity(_symbol)}, Result = {Result}, _preEvaluatedArgs:");
                foreach (var arg in this._preEvaluatedArgs)
                {
                    stringBuilder?.AppendLine($"{arg.Result}");
                }
                stringBuilder?.AppendLine("============================================");
            }
            if (this._evaluated && IsBoolean)
            {
                if (Result != 0.0m && Result != 1.0m)
                {
                    stringBuilder?.AppendLine($"Boolean expression rendered {Result}");
                }
            }
            return this._evaluated;
        }

        private bool TickImpl(RuntimeState runtimeState, ConstantsSet constants, StringBuilder stringBuilder)
        {
            for (; _preEvaluatedArgIndex < _preEvaluatedArgs.Length; ++_preEvaluatedArgIndex)
            {
                if (!_preEvaluatedArgs[_preEvaluatedArgIndex].Tick(runtimeState, constants, stringBuilder))
                {
                    //We have still not evaluated all arguments.
                    return false;
                }
            }

            switch (_symbol)
            {
                    //Identifier operations of increasing arity:
                case Symbols.InputArgument:
                    {
                        //Evaluates to one of the input arguments
                        this._result = runtimeState.Inputs[_qualifier];
                        return true;
                    }
                case Symbols.IntegerLiteral:
                    {
                        this._result = constants.Integers[_qualifier];
                        return true;
                    }
                case Symbols.DoubleLiteral:
                    {
                        //Evaluates to one of the constants. Behaves like the input arguments for
                        //the evaluation point of view but are not given as input, instead they evolve
                        //together with the rest of the _code.
                        this._result = constants.Doubles[_qualifier];
                        return true;
                    }
                case Symbols.WorkingVariable:
                    {
                        this._result = runtimeState.Variables[_qualifier];
                        return true;
                    }
                case Symbols.AssignWorkingVariable:
                    {
                       // Assingns the given working variable to the RHS
                       runtimeState.Variables[_qualifier] = Operand.Result;
                       
                        //The result is always the RHS (even if the variable does not change),
                        //this is for not changing the program by removing the assignment
                        this._result = Operand.Result;
                        return true;
                    }
                    //Unary operators:
                case Symbols.Not:
                    {
                        Bresult = !Operand.Bresult;
                        return true;
                    }
                    //Binary operators:
                case Symbols.Add:
                    {
                        this._result = Lhs.Result + Rhs.Result;
                        return true;
                    }
                case Symbols.Sub:
                    {
                        this._result = Lhs.Result - Rhs.Result;
                        return true;
                    }
                case Symbols.Mul:
                    {
                        this._result = Lhs.Result*Rhs.Result;
                        return true;
                    }
                    case Symbols.Div:
                    {
                        if (Math.Abs(Rhs.Result) <= 0.001m)
                        {
                            // We treat division by zero as division by one
                            this._result = Lhs.Result;
                        }
                        else
                        {
                            try
                            {
                                this._result = Lhs.Result / Rhs.Result;
                            }
                            catch (DivideByZeroException e)
                            {
                                // We treat division by zero as division by one
                                this._result = Lhs.Result;
                            }
                        }

                        return true;
                    }
                    case Symbols.Lt:
                    {
                        Bresult = Lhs.Result < Rhs.Result;
                        return true;
                    }
                case Symbols.Lteq:
                    {
                        Bresult = Lhs.Result <= Rhs.Result;
                        return true;
                    }
                case Symbols.Gt:
                    {
                        Bresult = Lhs.Result > Rhs.Result;
                        return true;
                    }
                case Symbols.Gteq:
                    {
                        Bresult = Lhs.Result >= Rhs.Result;
                        return true;
                    }
                case Symbols.Eq:
                    {
                        Bresult = Lhs.Result == Rhs.Result;
                        return true;
                    }
                case Symbols.Neq:
                    {
                        Bresult = Lhs.Result != Rhs.Result;
                        return true;
                    }
                case Symbols.And:
                    {
                        if (_conditionalChildrenIndex == 0 && !Lhs.Bresult) //short circuit
                        {
                            Bresult = false;
                            _conditionalChildrenIndex = _conditionalChildren.Length;
                        }

                        CallTree conditionalChild;
                        if (TickNextConditionalChild(runtimeState, constants, out conditionalChild, stringBuilder))
                        {
                            if (conditionalChild != null)
                            {
                                Bresult = conditionalChild.Bresult;
                            }
                            return true;
                        }
                        return false;
                    }
                case Symbols.Or:
                    {
                        if (_conditionalChildrenIndex == 0 && Lhs.Bresult) //short circuit
                        {
                            Bresult = true;
                            _conditionalChildrenIndex = _conditionalChildren.Length;
                        }

                        CallTree conditionalChild;
                        if (TickNextConditionalChild(runtimeState, constants, out conditionalChild, stringBuilder))
                        {
                            if (conditionalChild != null)
                            {
                                Bresult = conditionalChild.Bresult;
                            }
                            return true;
                        }
                        return false;
                    }
                case Symbols.Xor:
                    {
                        Bresult = Lhs.Bresult != Rhs.Bresult;
                        return true;
                    }
                case Symbols.Chain:
                    {
                        //Ignore the first result and return the second. This can build blocks.
                        this._result = Rhs.Result;
                        return true;
                    }
                case Symbols.If:
                    {
                        if (_conditionalChildrenIndex == 0 && !Lhs.Bresult)
                        {
                            //Skip the yes branch.
                            _conditionalChildrenIndex = _conditionalChildren.Length;
                        }
                        //Evaluate and return the yes branch
                        return TickNextConditionalChild(runtimeState, constants, ref _result, stringBuilder);
                    }
                case Symbols.While:
                    {
                        if (_conditionalChildrenIndex == 0)
                        {
                            CallTree conditionalChild;
                            if (TickNextConditionalChild(runtimeState, constants, out conditionalChild, stringBuilder))
                            {
                                if (!conditionalChild.Bresult)
                                {
                                    //The guard expression evaluated to zero (or null), exit the loop.
                                    _conditionalChildrenIndex = _conditionalChildren.Length;
                                }
                            }
                        }

                        if (_conditionalChildrenIndex == 1)
                        {
                            //If the body is fully evaluated, we store the last result.
                            if (TickNextConditionalChild(runtimeState, constants, ref _result, stringBuilder))
                            {
                                //..and reset the conditional children so that the guard expression will be evaluated again.
                                ResetConditionalChildren();
                            }
                        }
                        return _conditionalChildrenIndex >= _conditionalChildren.Length;
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
                        runtimeState.Variables[(int)Math.Abs(Lhs.Result)] = Operand.Result;
                        //The result is always the RHS (even if the variable does not change),
                        //this is for not changing the program by removing the assignment
                        this._result = Operand.Result;
                        return true;
                    }
                    //Ternary operator(s):
                case Symbols.Ifelse:
                    {
                        if (_conditionalChildrenIndex == 0 && !_preEvaluatedArgs[0].Bresult)
                        {
                            //Skip the yes branch.
                            ++_conditionalChildrenIndex;
                        }

                        //Evaluate and return the selected branch (0 for yes, 1 for no)
                        if (TickNextConditionalChild(runtimeState, constants, ref _result, stringBuilder))
                        {
                            //We guarantee to evaluate at mostly one.
                            _conditionalChildrenIndex = _conditionalChildren.Length;
                        }
                        return _conditionalChildrenIndex >= _conditionalChildren.Length;
                    }
                case Symbols.Noop:
                    {
                        this.Bresult = false;
                        return true;
                    }
                default:
                    {
                        throw new InvalidConstraintException("Invalid symbol " + _symbol + " found at runtime");
                    }
            }
        }

        private void ResetConditionalChildren()
        {
            foreach (CallTree node in _conditionalChildren)
            {
                node.Reset();
            }
            _conditionalChildrenIndex = 0;
        }

        public void Reset()
        {
            //Reset the list of states for loops
            //_loopStateList = new List<VariableSet>();
            ResetPreEvaluatedArgs();
            ResetConditionalChildren();
            this._result = 0;
            this._evaluated = false;
            _sign = 0;
        }

        private void ResetPreEvaluatedArgs()
        {
            foreach (CallTree node in _preEvaluatedArgs)
            {
                node.Reset();
            }
            _preEvaluatedArgIndex = 0;
        }

        private bool EvaluateConstantArguments(ConstantsSet constants, StringBuilder stringBuilder)
        {
            bool hasPreEvaluatedConstant = false;
            foreach (CallTree callTree in _preEvaluatedArgs)
            {
                if (callTree.IsConstant)
                {
                    hasPreEvaluatedConstant = true;
                    if (!callTree.Tick(constants, stringBuilder))
                    {
                        throw new InvalidOperationException("Ticking pre-evaluated constant but Tick returns false");
                    }
                }
            }
            foreach (CallTree callTree in _conditionalChildren)
            {
                if (callTree.IsConstant)
                {
                    hasPreEvaluatedConstant = true;
                    if (!callTree.Tick(constants, stringBuilder))
                    {
                        throw new InvalidOperationException("Ticking conditional constant but Tick returns false");
                    }
                }
            }
            return hasPreEvaluatedConstant;
        }

        public CallTree Simplify(ConstantsSet constants, StringBuilder stringBuilder = null)
        {
            var callTree = SimplifyConstant(constants, stringBuilder);

            if (callTree != null)
            {
                return callTree;
            }

            callTree = ReplaceAssignmentToSelfOrNoopWithVariable(stringBuilder);

            if (callTree != null)
            {
                //We assigned a variable to itself or to Noop; that becomes the variable.
                return callTree;
            }

            bool hasAtLeastOneConstantArgument = EvaluateConstantArguments(constants, stringBuilder);
            if (hasAtLeastOneConstantArgument)
            {
                callTree = ReplaceExpressionWithConstant(constants, stringBuilder);
                if (callTree != null)
                {
                    //All children were constants. Replace with constant expression.
                    //so (3+3) => (9)
                    return callTree;
                }
            }

            callTree = SimplifyPreEvaluatedArgs(constants, stringBuilder);
            if (callTree != null)
            {
                //We managed to simplify one of the pre evaluated children.
                return callTree;
            }

            callTree = SimplifyConditionalChildren(constants, stringBuilder);
            if (callTree != null)
            {
                //we managed to simplify one of the conditional children.
                return callTree;
            }

            callTree = ReplaceWithConstantDueToRhslhsEquivalence(constants, stringBuilder);
            if (callTree != null)
            {
                //we managed to replace equality with constant as the code was identical of each side,
                //had no side effects and no loops (so a = a is true for all a, a+(x) = (a+(x) is also true etc.
                return callTree;
            }

            callTree = ReplaceSymmetricTernaryOperatorWithChain(constants, stringBuilder);
            if (callTree != null)
            {
                //We managed to replace an IF THEN ELSE statement with a chain as the code in both
                //branches were identical, i.e IF x <> 7 THEN y ELSE y becomes x <> 7 => y
                return callTree;
            }

            callTree = ReplaceLhsWithoutSideEffectsInChainWithLhs(stringBuilder);
            if (callTree != null)
            {
                //Here x <> 7 => Y was replaced with 7 as x <> 7 has no side effects.
                return callTree;
            }

            callTree = SimplifyNegatedExpression(constants, stringBuilder);
            if (callTree != null)
            {
                //Here NOT(x = y) was replaced with x<>y.
                return callTree;
            }

            if (!hasAtLeastOneConstantArgument)
            {
                //If no child is constant there is nothing nore to simplify.
                return null;
            }

            //At least one argument is constant.
            callTree = CommutativeMoveSinglyConstOperandFromRhStoLhs(stringBuilder);
            if (callTree != null)
            {
                //For commutative expressions where only RHS is constant move it to LHS
                //so (x+3) => (3+x).
                //Call simplify recursively to see if the normalisation actually has any effect.
                CallTree simplifiedCallTree = callTree.Simplify(constants, stringBuilder);
                if (simplifiedCallTree != null)
                {
                    callTree = simplifiedCallTree;
                }
                return callTree;
            }

            callTree = GroupConstantsTogetherInAssociativeChain(stringBuilder);
            if (callTree != null)
            {
                //For associative expressions where LHS is constant and RHS consit of constant LHS but nonconst RHS, move the
                //second constant as to the RHS of LHS. I.e. (3+(3+x)) => ((3+3)+x)
                //so LHS can be replaced by a new constant (9) following ReplaceExpressionWithConstant in
                //next iteration. Note that ((x+3)+3) was first replaced with ((3+x)+3) using
                //CommutativeMoveSinglyConstOperandFromRhStoLhs and then (3+(3+x)) using the same rule.
                //Call simplify recursively to see if the normalisation actually has any effect.
                CallTree simplifiedCallTree = callTree.Simplify(constants, stringBuilder);
                if (simplifiedCallTree != null)
                {
                    callTree = simplifiedCallTree;
                }
                return callTree;
            }

            callTree = ReplaceTernaryIfWithNoopBranchWithBinaryIf(stringBuilder);
            if (callTree != null)
            {
                //In the IF THEN ELSE statement one alternative was NOOP so we replace this with I.F THEN instead,
                //negating the guard if NOOP was on the YES branch.
                return callTree;
            }

            callTree = SimplifyArithmeticExpression(constants, stringBuilder);
            if (callTree != null)
            {
                //1*exp => exp; 0+exp=>exp
                return callTree;
            }

            callTree = SimplifyRelationalExpression(constants, stringBuilder);
            if (callTree != null)
            {
                return callTree;
            }

            callTree = SimplifyDisjunctionAndConjunction(constants, stringBuilder);
            if (callTree != null)
            {
                //true AND exp becomes exp<>0
                //false AND exp becomes false if exp lacks side effects otherwise the chain exp=>false
                //true OR exp becomes true if exp lacks side effects otherwise the chain exp=>false
                //false OR exp become exp<>0
                return callTree;
            }


            callTree = ReplaceSubtractionWithConstantWithAdditionToNegative(constants, stringBuilder);
            if (callTree != null)
            {
                //exp-constant => -constant+exp
                return callTree;
            }

            callTree = SimplifyConditionalEvaluation(stringBuilder);
            if (callTree != null)
            {
                return callTree;
            }
            return null;
        }

        private CallTree SimplifyConstant(ConstantsSet constants, StringBuilder stringBuilder)
        {
            if (IsConstant)
            {
                //Cannot be further simplified
                if (!Tick(constants, stringBuilder))
                {
                    throw new InvalidOperationException("CallTree IsConstant but Tick() returns false");
                }

                if (_symbol == Symbols.DoubleLiteral)
                {
                    if (Result == Math.Floor(Result))
                    {
                        stringBuilder?.AppendLine($"Replacing double {Result} with integer");
                        return CreateConstantCallTree((int) Result, constants);
                    }
                }
            }
            return null;
        }

        private CallTree SimplifyRelationalExpression(ConstantsSet constants, StringBuilder stringBuilder)
        {
            CallTree callTree = null;
            if (IsRelational)
            {
                //If operation is <> but both sides are booleans then make this into XOR
                if (_symbol == Symbols.Neq && Lhs.IsBoolean && Rhs.IsBoolean)
                {
                    return CreateBinaryCalltree(Symbols.Xor, Lhs, Rhs);
                }

                Symbols symbol = _symbol;
                callTree = SimplifyRelationalExpression(constants, symbol, Lhs, Rhs, stringBuilder);
                ReorderRelationOperator(ref symbol);
                callTree = callTree ?? SimplifyRelationalExpression(constants, symbol, Rhs, Lhs, stringBuilder);
            }
            return callTree;
        }


        private CallTree SimplifyRelationalExpression(ConstantsSet constants, Symbols symbol, CallTree lhs,
                                                      CallTree rhs, StringBuilder stringBuilder)
        {
            if (rhs.IsBoolean)
            {
                if (lhs.IsConstant && lhs.IsPositive)
                {
                    if (lhs.Result > 1.0m)
                    {
                        switch (symbol)
                        {
                            case Symbols.Eq:
                            case Symbols.Lt:
                            case Symbols.Lteq:
                                stringBuilder?.AppendLine("Replacing constant above 1 = | < | <= bool expression with false");
                                return CreateChain(constants, rhs, false);
                            case Symbols.Gt:
                            case Symbols.Gteq:
                            case Symbols.Neq:
                                stringBuilder?.AppendLine("Replacing constant above 1 <> | > | >= bool expression with true");
                                return CreateChain(constants, rhs, true);
                        }
                    }

                    else if (lhs.Result < 1.0m)
                    {
                        switch (symbol)
                        {
                            case Symbols.Eq:
                                stringBuilder?.AppendLine("Replacing constant above 0 but less than 1 = bool expression with false");
                                return CreateChain(constants, rhs, false);
                            case Symbols.Gt:
                            case Symbols.Gteq:
                                stringBuilder?.AppendLine("Replacing constant above 0 but less than 0 >|>= bool expression with negation of the bool expression");
                                return rhs.WrapExpressionInNegation();
                            case Symbols.Lt:
                            case Symbols.Lteq:
                                stringBuilder?.AppendLine("Replacing constant above 0 but less than 0 <|<= bool experssion with the bool expression");
                                return rhs;
                            case Symbols.Neq:
                                stringBuilder?.AppendLine("Replacing constant above 0 but less than 0 <> bool with true");
                                return CreateChain(constants, rhs, true);
                        }
                    }
                    else
                    {
                        switch (symbol)
                        {
                            case Symbols.Eq:
                            case Symbols.Lteq:
                                stringBuilder?.AppendLine("Replacing 1 = or <= bool with the bool");
                                return rhs;
                            case Symbols.Gt:
                            case Symbols.Neq:
                                stringBuilder?.AppendLine("Replacing 1 > or >= bool with the negation of the bool expression");
                                return rhs.WrapExpressionInNegation();
                            case Symbols.Lt:
                                stringBuilder?.AppendLine("Replacing 1 < bool expression with false");
                                return CreateChain(constants, rhs, false);
                            case Symbols.Gteq:
                                stringBuilder?.AppendLine("Replacing 1 >= bool expression with true");
                                return CreateChain(constants, rhs, true);
                        }
                    }
                }
                else if (lhs.IsNegative)
                {
                    switch (symbol)
                    {
                        case Symbols.Eq:
                        case Symbols.Gt:
                        case Symbols.Gteq:
                            stringBuilder?.AppendLine("Replacing -const =  | > | >= bool expression with false");
                            return CreateChain(constants, rhs, false);
                        case Symbols.Neq:
                        case Symbols.Lt:
                        case Symbols.Lteq:
                            stringBuilder?.AppendLine("Replacing -const <>  | < | <= bool expression with true");
                            return CreateChain(constants, rhs, true);
                    }
                }
                else if (lhs.IsZero)
                {
                    switch (symbol)
                    {
                        case Symbols.Eq:
                        case Symbols.Gteq:
                            stringBuilder?.AppendLine("Replacing 0 = | >= bool expression negation of the boolean expression");
                            return rhs.WrapExpressionInNegation();
                        case Symbols.Gt:
                            stringBuilder?.AppendLine("Replacing 0 > bool expression with false");
                            return CreateChain(constants, rhs, false);
                        case Symbols.Lteq:
                            stringBuilder?.AppendLine("Replacing 0 <= bool expression with true");
                            return CreateChain(constants, rhs, true);
                        case Symbols.Neq:
                        case Symbols.Lt:
                            stringBuilder?.AppendLine("Replacing 0 <> | 0 <  bool expression with the bool expression");
                            return rhs;
                    }
                }
            }
            return null;
        }

        private CallTree SimplifyNegatedExpression(ConstantsSet constants, StringBuilder stringBuilder)
        {
            if (_symbol == Symbols.Not)
            {
                if (Operand._symbol == Symbols.Not)
                {
                    //Double negation
                    //not(not exp) => exp if exp is boolean, else it becomes 0 <> exp or 1 or 0 if exp was a constant
                    stringBuilder?.AppendLine("Replacing NOT(NOT(exp)) with exp<>0");
                    return Operand.Operand.LimitToZerOrOne(constants, stringBuilder);
                }

                Symbols negatedSymbol = Operand._symbol;
                if (negatedSymbol == Symbols.Xor && Operand.Lhs.IsBoolean && Operand.Rhs.IsBoolean)
                {
                    //We interpret Xor as Neq here for two booleans so the result of negation will be Eq.
                    stringBuilder?.AppendLine("Replacing boolexp1 XOR boolexp2 with boolexp1 <> boolexp2");
                    negatedSymbol = Symbols.Neq;
                }

                if (NegateRelationalOperator(ref negatedSymbol))
                {

                    stringBuilder?.AppendLine("Replacing NOT(exp1 RELOP exp2) with exp1 Inverse_relop exp2");
                    return CreateBinaryCalltree(negatedSymbol, Operand.Lhs,
                                                Operand.Rhs);
                }
                if ((negatedSymbol == Symbols.And || negatedSymbol == Symbols.Or) &&
                    (Operand.Lhs._symbol == Symbols.Not && Operand.Rhs._symbol == Symbols.Not))
                {
                    //NOT(NOT a AND NOT b) => a OR b
                    //NOT(NOT a OR NOT b) => a AND b

                    stringBuilder?.AppendLine(
                        "Replacing negated AND or OR expression having two negated terms with opposite operator.");
                    return
                        CreateBinaryCalltree(
                            negatedSymbol == Symbols.And ? Symbols.Or : Symbols.And,
                            Operand.Lhs.Operand, Operand.Rhs.Operand);
                }
            }
            return null;
        }

        private static bool NegateRelationalOperator(ref Symbols symbol)
        {
            switch (symbol)
            {
                case Symbols.Eq:
                    symbol = Symbols.Neq;
                    break;
                case Symbols.Gt:
                    symbol = Symbols.Lteq;
                    break;
                case Symbols.Gteq:
                    symbol = Symbols.Lt;
                    break;
                case Symbols.Lt:
                    symbol = Symbols.Gteq;
                    break;
                case Symbols.Lteq:
                    symbol = Symbols.Gt;
                    break;
                case Symbols.Neq:
                    symbol = Symbols.Eq;
                    break;
                default:
                    return false;
            }
            return true;
        }

        private static void ReorderRelationOperator(ref Symbols symbol)
        {
            switch (symbol)
            {
                case Symbols.Eq:
                    symbol = Symbols.Eq;
                    break;
                case Symbols.Gt:
                    symbol = Symbols.Lt;
                    break;
                case Symbols.Gteq:
                    symbol = Symbols.Lteq;
                    break;
                case Symbols.Lt:
                    symbol = Symbols.Gt;
                    break;
                case Symbols.Lteq:
                    symbol = Symbols.Gteq;
                    break;
                case Symbols.Neq:
                    symbol = Symbols.Neq;
                    break;
            }
        }

        private CallTree SimplifyDisjunctionAndConjunction(ConstantsSet constants, StringBuilder stringBuilder)
        {
            if ((_symbol == Symbols.And || _symbol == Symbols.Or || _symbol == Symbols.Xor))
            {
                if (Lhs.IsConstant)
                {
                    bool lhs = Lhs.Bresult;
                    if (_symbol == Symbols.Xor)
                    {
                        //true XOR exp => NOT(exp)
                        //false XOR exp => exp <> 0
                        stringBuilder?.AppendLine(
                            "Replacing true XOR exp with NOT(exp) or replacing false XOR exp with exp<>0");
                        return lhs ? Rhs.WrapExpressionInNegation() : Rhs.LimitToZerOrOne(constants, stringBuilder);
                    }
                    if (((_symbol == Symbols.And && !lhs) || (_symbol == Symbols.Or && lhs)))
                    {
                        //We can eliminate RHS as it would never have been evaluated (short circuit).
                        //(false AND exp) => false
                        //(true OR exp) => true
                        stringBuilder?.AppendLine(
                            "Replacing false AND exp with false or relacing true OR exp with true");
                        return CreateConstantCallTree(lhs, constants);
                    }

                    if (((_symbol == Symbols.And && lhs) || (_symbol == Symbols.Or && !lhs)))
                    {
                        //We can eliminate the constant LHS term and convert RHS to a boolean expression.
                        //(true AND exp) => (exp <> 0)
                        //false OR exp) => (exp <> 0)
                        stringBuilder?.AppendLine("Replacing true AND exp or false OR exp with exp <> 0");
                        return Rhs.LimitToZerOrOne(constants, stringBuilder);
                    }
                }

                if (Rhs.IsConstant)
                {
                    bool rhs = Rhs.Bresult;
                    if (_symbol == Symbols.Xor)
                    {
                        //exp XOR true => NOT(exp)
                        //exp XOR false => exp <> 0
                        stringBuilder?.AppendLine(
                            "Replacing exp XOR true XOR with NOT(exp) or replacing exp XOR false with exp<>0");
                        return rhs ? Lhs.WrapExpressionInNegation() : Lhs.LimitToZerOrOne(constants, stringBuilder);
                    }
                    if (((_symbol == Symbols.And && !rhs) || (_symbol == Symbols.Or && rhs)))
                    {
                        //Let's this evaluate to a chain that discards the value and renders the constant RHS.
                        //(If LHS lacks side effects it might be removed later according to ReplaceLhsWithoutSideEffectsInChainWithLhs())
                        //(exp AND false) => (exp;false)
                        //(exp OR true) => (exp;true)
                        stringBuilder?.AppendLine(
                            "Replacing false AND exp with false or relacing true OR exp with true");
                        return CreateChain(constants, Lhs, rhs);
                    }

                    if (((_symbol == Symbols.And && rhs) || (_symbol == Symbols.Or && !rhs)))
                    {
                        //We can eliminate the constant RHS term and convert LHS to a boolean expression.
                        //(exp AND true) => (exp <> 0)
                        //(exp OR false) => (exp <> 0)
                        stringBuilder?.AppendLine("Replacing exp AND tru or exp OR false with exp <> 0");
                        return Lhs.LimitToZerOrOne(constants, stringBuilder);
                    }
                }
            }
            return null;
        }

        private CallTree CreateChain(CallTree lhs, CallTree rhs)
        {
            return lhs.LacksSideEffects ? rhs : CreateBinaryCalltree(Symbols.Chain, lhs, rhs);
        }

        private CallTree CreateChain(ConstantsSet constants, CallTree lhs, bool rhs)
        {
            return CreateChain(lhs, CreateConstantCallTree(rhs, constants));
        }

        private CallTree ReplaceLhsWithoutSideEffectsInChainWithLhs(StringBuilder stringBuilder)
        {
            if (_symbol == Symbols.Chain && Lhs.LacksSideEffects)
            {
                stringBuilder?.AppendLine("Replacing exp1=>exp2 with exp2");
                return Rhs;
            }
            return null;
        }

        private CallTree SimplifyArithmeticExpression(ConstantsSet constants, Symbols symbol, CallTree lhs, CallTree rhs, StringBuilder stringBuilder)
        {
            if (!IsArithmeticExpression)
            {
                return null;
            }

            if (lhs.IsConstant)
            {
                if (symbol == Symbols.Add && lhs.IsZero)
                {
                    //0 + exp => exp
                    stringBuilder?.AppendLine("Replacing 0+exp with exp");
                    return rhs;
                }

                if (lhs.IsZero && (symbol == Symbols.Mul || (symbol == Symbols.Div && rhs.ResultIsNeverZero)))
                {
                    //0*exp and 0/exp are both 0
                    //(If RHS lacks side effects it might be removed later according to ReplaceLhsWithoutSideEffectsInChainWithLhs())
                    stringBuilder?.AppendLine("Replacing 0*exp or 0/exp with 0");
                    return lhs;
                }

                if (symbol == Symbols.Mul && lhs.Result == 1.0m)
                {
                    //1.0 * exp => exp
                    stringBuilder?.AppendLine("Replacing 1*exp with exp");

                    return rhs;
                }
            }

            if (rhs.IsConstant)
            {
                if (symbol == Symbols.Div)
                {
                    if (rhs.Result == 1.0m)
                    {
                        //exp / 1.0 => exp
                        stringBuilder?.AppendLine("Replacing exp/1.0 with exp");
                        return lhs;
                    }

                    //Special semantics to avoid handling non-normal numbers: exp / 0.0 => exp
                    if (rhs.IsZero)
                        //But we check by seeing if dividing 1.0 with rhs actually gives NaN.
                    {
                        stringBuilder?.AppendLine("Replacing exp/0.0 with exp");
                        return lhs;
                    }
                }
            }
            return null;
        }

        private CallTree SimplifyArithmeticExpression(ConstantsSet constants, StringBuilder stringBuilder)
        {
            CallTree callTree = null;
            if (_preEvaluatedArgs.Length == 2)
            {
                callTree = SimplifyArithmeticExpression(constants, _symbol, Lhs, Rhs, stringBuilder);
                if (IsCommutative)
                {
                    callTree = callTree ?? SimplifyArithmeticExpression(constants, _symbol, Rhs, Lhs, stringBuilder);
                }
            }
            return callTree;
        }

        private CallTree ReplaceSubtractionWithConstantWithAdditionToNegative(ConstantsSet constants, StringBuilder stringBuilder)
        {
            if (_symbol == Symbols.Sub)
            {
                //exp - constant => -constant + exp;
                switch (Rhs._symbol)
                {
                    case Symbols.DoubleLiteral:
                        stringBuilder?.AppendLine("Replacing exp-DoubleLiteral with -DoubleLiteral+exp");
                        return CreateBinaryCalltree(Symbols.Add,
                                                    CreateConstantCallTree(-Rhs.Result,
                                                                           constants, stringBuilder),
                                                    Lhs);

                    case Symbols.IntegerLiteral:
                        stringBuilder?.AppendLine("Replacing exp-IntegerLiteral with -IntegerLiteral+exp");
                        //If Rhs is 0 the next simplification will take care of it.
                        return CreateBinaryCalltree(Symbols.Add,
                                                    CreateConstantCallTree((int) -Rhs.Result,
                                                                           constants),
                                                    Lhs);


                    case Symbols.Noop:
                        stringBuilder?.AppendLine("Replacing exp-Noop with Noop");
                        //exp - Noop will evaluate to Noop so we just change the symbol to chain
                        return new CallTree(Symbols.Chain, _varnumber, _preEvaluatedArgs);
                }
            }
            return null;
        }

        private CallTree ReplaceTernaryIfWithNoopBranchWithBinaryIf(StringBuilder stringBuilder)
        {
            if (_symbol == Symbols.Ifelse)
            {
                if (_conditionalChildren[1]._symbol == Symbols.Noop)
                {
                    //IF guard THEN exp ELSE Noop => IF guard THEN exp
                    stringBuilder?.AppendLine("Replacing IF exp1 THEN exp2 ELSE Noop with IF exp1 THEN exp2");
                    return new CallTree(Symbols.If, _varnumber, _preEvaluatedArgs,
                                        new[] {_conditionalChildren[0]});
                }

                if (_conditionalChildren[0]._symbol == Symbols.Noop)
                {
                    //IF guard THEN Noop ELSE exp => IF NOT guard THEN exp
                    stringBuilder?.AppendLine("Replacing IF exp1 THEN Noop ELSE exp2 with IF NOT(exp1) THEN exp2");
                    return new CallTree(Symbols.If, _varnumber,
                                        new[] {_preEvaluatedArgs[0].WrapExpressionInNegation()},
                                        new[] {_conditionalChildren[0]});
                }
            }
            return null;
        }

        private CallTree WrapExpressionInNegation()
        {
            return new CallTree(Symbols.Not, _varnumber, new[] {this});
        }

        private CallTree ReplaceWithConstantDueToRhslhsEquivalence(ConstantsSet constants, StringBuilder stringBuilder)
        {
            if (LacksSideEffects)
            {
                //exp1 = exp2 => true
                //exp1 => exp2 => true
                //exp1 <= exp2 => true
                //exp1 <> exp2 => false
                //exp1 < exp2 => false
                //exp1 > exp2 => false
                //exp1 / exp2 => 1
                //exp1 AND exp1 => exp1 <> 0
                //exp2 AND exp2 => exp2 <> 0
                //exp1 XOR exp1 => false 
                switch (_symbol)
                {
                    case Symbols.Eq:
                    case Symbols.Gteq:
                    case Symbols.Lteq:
                        //An inequality is true if sides are equal (unless exp is NaN as NaN <> NaN)
                        {
                            if (LhsEqualsRhs())
                            {
                                stringBuilder?.AppendLine("Replacing exp == | >= | <= exp with true");
                                return CreateConstantCallTree(true, constants);
                            }
                            break;
                        }
                    case Symbols.Gt:
                    case Symbols.Lt:
                    case Symbols.Neq:
                        //An inequality is false if sides are equal (unless exp is NaN as NaN <> NaN)
                        {
                            if (LhsEqualsRhs())
                            {
                                stringBuilder?.AppendLine("Replacing exp <> | < | > exp with false");
                                return CreateConstantCallTree(false, constants);
                            }
                            break;
                        }
                    case Symbols.Div:
                        //(exp / exp) => 1 (unless exp is 0 where result is also 0)
                        {
                            if (LhsEqualsRhs() && Rhs.ResultIsNeverZero)
                            {
                                stringBuilder?.AppendLine("Replacing exp/exp with 1");
                                return CreateConstantCallTree(1, constants);
                            }
                            break;
                        }
                    case Symbols.Or:
                    case Symbols.And:
                    case Symbols.Xor:
                        {
                            if (LhsEqualsRhs())
                            {
                                if (_symbol == Symbols.Xor) //NaN is interpreted as true here.
                                {
                                    stringBuilder?.AppendLine("Replacing exp XOR exp with false");
                                    return CreateConstantCallTree(false, constants);
                                }
                                //a AND a => a, a OR a => a, a XOR a => false, a XOR(NOT(a)) => true
                                return Lhs.LimitToZerOrOne(constants, stringBuilder);
                            }
                            if ((Lhs._symbol == Symbols.Not && Lhs.Operand.Equals(Rhs)) ||
                                (Rhs._symbol == Symbols.Not && Rhs.Operand.Equals(Lhs)))
                            {
                                stringBuilder?.AppendLine("Replacing exp relop NOT(exp) with constant");
                                //One side is the negation of the other. Or or Xor both become true.
                                return CreateConstantCallTree(_symbol != Symbols.And, constants);
                            }
                            break;
                        }
                    case Symbols.Sub:
                    case Symbols.Add:
                        return SummariseTerms(constants, stringBuilder);
                }
            }
            return null;
        }

        private CallTree SummariseTerms(ConstantsSet constants, StringBuilder stringBuilder)
        {
            if (LhsEqualsRhs())
            {
                if (_symbol == Symbols.Sub)
                {
                    //(exp-exp) = 0
                    stringBuilder?.AppendLine("Replacing exp-exp with 0");

                    return CreateConstantCallTree(0, constants);
                }
                if (_symbol == Symbols.Add)
                {
                    //exp+exp => 2*exp (Nan + Nan = Nan, 2*Nan = Nan)
                    stringBuilder?.AppendLine("Replacing exp+exp with 2*exp");
                    return CreateBinaryCalltree(Symbols.Mul, CreateConstantCallTree(2, constants),
                                                Lhs);
                }
            }

            CallTree callTree = AddSingleTerm(constants, Lhs, Rhs, stringBuilder);
            return callTree ?? SummariseMultipliedTerms(stringBuilder);
        }

        private CallTree AddSingleTerm(ConstantsSet constants, CallTree lhs, CallTree rhs, StringBuilder stringBuilder)
        {
            if (lhs._symbol == Symbols.Mul)
            {
                if (rhs.Equals(lhs.Rhs))
                {
                    //(n*exp)+exp => ((n+1)*exp)
                    //(n*exp)-exp => ((n-1)*exp)
                    stringBuilder?.AppendLine("Replacing (n*exp)-+exp with (n+-1)*exp");
                    CallTree newLhs = CreateBinaryCalltree(_symbol, lhs.Lhs,
                                                           CreateConstantCallTree(1, constants));
                    return CreateBinaryCalltree(lhs._symbol, newLhs, rhs);
                }

                if (rhs.Equals(lhs.Lhs))
                {
                    //(exp*n)+exp => (n+1)*exp
                    //(exp*n)-exp => (n-1)*exp
                    stringBuilder?.AppendLine("Replacing (exp*n)-+exp with (n+-1)*exp");
                    CallTree newLhs = CreateBinaryCalltree(_symbol, CreateConstantCallTree(1, constants),
                                                           lhs.Rhs);
                    return CreateBinaryCalltree(lhs._symbol, newLhs, rhs);
                }
            }

            if (rhs._symbol == Symbols.Mul)
            {
                if (lhs.Equals(rhs.Rhs))
                {
                    //(exp+(n*exp) => (1+n)*exp
                    //(exp-(n*exp) => (1-n)*exp
                    stringBuilder?.AppendLine("Replacing exp+-(n*exp) with (1+-n)*exp");
                    CallTree newLhs = CreateBinaryCalltree(_symbol,
                        CreateConstantCallTree(1, constants),
                        rhs.Lhs);

                    return CreateBinaryCalltree(rhs._symbol, newLhs, rhs.Rhs);
                }

                if (lhs.Equals(rhs.Lhs))
                {
                    //(exp+(exp*n) => (1+n)*exp
                    //(exp-(exp*n) => (1-n)*exp
                    stringBuilder?.AppendLine("Replacing exp+-(exp*n) with (1+-n)*exp");
                    CallTree newLhs = CreateBinaryCalltree(_symbol,
                        CreateConstantCallTree(1, constants),
                        rhs.Rhs);

                    return CreateBinaryCalltree(rhs._symbol, newLhs, rhs.Lhs);
                }
            }

            return null;
        }

        private CallTree SummariseMultipliedTerms(StringBuilder stringBuilder)
        {
            if (Lhs._symbol == Symbols.Mul && Rhs._symbol == Symbols.Mul)
            {
                CallTree equalTerms;
                CallTree addedLhsTerm;
                CallTree addedRhsTerm;
                if (Lhs.Lhs.Equals(Rhs.Lhs))
                {
                    stringBuilder?.AppendLine("Replacing (exp*n)+-(exp*m) with (n+-m)*exp");
                    equalTerms = Lhs.Lhs;
                    addedLhsTerm = Lhs.Rhs;
                    addedRhsTerm = Rhs.Rhs;
                }
                else if (Lhs.Lhs.Equals(Rhs.Rhs))
                {
                    stringBuilder?.AppendLine("Replacing (exp*n)+-(m*exp) with (n+-m)*exp");
                    equalTerms = Lhs.Lhs;
                    addedLhsTerm = Lhs.Rhs;
                    addedRhsTerm = Rhs.Lhs;
                }
                else if (Lhs.Rhs.Equals(Rhs.Lhs))
                {
                    stringBuilder?.AppendLine("Replacing (n*exp)+-(m*exp) with (n+-m)*exp");
                    equalTerms = Lhs.Rhs;
                    addedLhsTerm = Lhs.Lhs;
                    addedRhsTerm = Rhs.Rhs;
                }
                else if (Lhs.Rhs.Equals(Rhs.Rhs))
                {
                    stringBuilder?.AppendLine("Replacing (n*exp)+-(exp*m) with (n+-m)*exp");
                    equalTerms = Lhs.Rhs;
                    addedLhsTerm = Lhs.Lhs;
                    addedRhsTerm = Rhs.Lhs;
                }
                else
                {
                    return null;
                }

                return CreateBinaryCalltree(Lhs._symbol,
                                            CreateBinaryCalltree(_symbol, addedLhsTerm,
                                                                 addedRhsTerm), equalTerms);
            }
            return null;
        }

        private bool LhsEqualsRhs()
        {
            return Lhs.Equals(Rhs);
        }

        private CallTree ReplaceAssignmentToSelfOrNoopWithVariable(StringBuilder stringBuilder)
        {
            if (_symbol == Symbols.AssignWorkingVariable)
            {
                if ((Operand._symbol == Symbols.WorkingVariable ||
                     Operand._symbol == Symbols.AssignWorkingVariable) &&
                    Operand._qualifier == _qualifier)
                {
                    //(a := a) => a
                    //a:=(a:=x) => (a:=x)
                    stringBuilder?.AppendLine($"Replacing assignment of Y{Operand._qualifier} to itself with itself");
                    return Operand;
                }

                if (Operand._symbol == Symbols.Noop)
                {
                    //(a := Noop) => a
                    stringBuilder?.AppendLine($"Replacing assignment of Y{Operand._qualifier} to Noop with itself");
                    return new CallTree(Symbols.WorkingVariable, _varnumber, _qualifier);
                }
            }

            return null;
        }

        private CallTree CreateBinaryCalltree(Symbols symbol, CallTree lhs, CallTree rhs)
        {
            if (Program.GetSyntacticArity(symbol) != 2)
            {
                throw new ArgumentException(string.Format("{0} is not a binary operator", symbol));
            }

            switch (Program.GetDependencyArity(symbol))
            {
                case 1:
                    return new CallTree(symbol, _varnumber, new[] {lhs}, new[] {rhs});
                case 2:
                    return new CallTree(symbol, _varnumber, new[] {lhs, rhs});
                default:
                    throw new ArgumentException(string.Format("{0} is not a binary operator", symbol));
            }
        }

        private CallTree GroupConstantsTogetherInAssociativeChain(StringBuilder stringBuilder)
        {
            if (IsAssociative)
            {
                if (Lhs.IsConstant && Rhs._symbol == _symbol && Rhs.Lhs.IsConstant && !Rhs.Rhs.IsConstant)
                {
                    /* for all associative operations Op where (expr1 Op expr2 = expr2 Op expr1):
                 constant1 Op (constant2 Op nonconst_expr) => ((constant1 Op constant2) Op conconst_expr) */
                    stringBuilder?.AppendLine("Moving constant term from RHS.LHS to LHS.RHS of associative chain");
                    return CreateBinaryCalltree(_symbol,
                                                CreateBinaryCalltree(_symbol, Lhs,
                                                                     Rhs.Lhs),
                                                Rhs.Rhs);
                }
                if (Rhs.IsConstant && Lhs._symbol == _symbol && Lhs.Rhs.IsConstant && !Lhs.Lhs.IsConstant)
                {
                    stringBuilder?.AppendLine("Moving constant term from RHS to LHS.LHS of associative chain");
                    //Mirror of the above
                    return CreateBinaryCalltree(_symbol,
                                                CreateBinaryCalltree(_symbol, Rhs,
                                                                     Lhs.Rhs),
                                                Lhs.Rhs);
                }
            }
            return null;
        }

        private CallTree CommutativeMoveSinglyConstOperandFromRhStoLhs(StringBuilder stringBuilder)
        {
            if (IsSemanticallyCommutative)
            {
                if (Rhs.IsConstant && !Lhs.IsConstant)
                {
                    /*for all commutative operations Op where (expr1 Op expr2 = expr2 Op expr1):
                 (nonconst_expr Op constant1) => (constant1 Op nonconst_expr)*/
                    stringBuilder?.AppendLine("Moving constant RHS term of Commutative operator to LHS");
                    //For commutative operations where exactly one operand is a constant we move it to the LHS
                    return CreateBinaryCalltree(_symbol, Rhs, Lhs);
                }
            }
            return null;
        }

        private CallTree ReplaceSymmetricTernaryOperatorWithChain(ConstantsSet constants, StringBuilder stringBuilder)
        {
            if (_symbol == Symbols.Ifelse)
            {
                if (_conditionalChildren[0].Equals(_conditionalChildren[1]))
                {
                    //IF exp1 THEN exp2 ELSE exp2 => exp1=>exp2
                    //If both branches of the ternary operator are identical then convert to chain as the result never varies
                    stringBuilder?.AppendLine("Replacing IF exp1 THEN exp2 ELSE exp2 with exp2");
                    return CreateChain(_preEvaluatedArgs[0], _conditionalChildren[0]);
                }

                if (_preEvaluatedArgs[0].Equals(_conditionalChildren[0]) && _conditionalChildren[1].IsConstant &&
                    _conditionalChildren[1].IsZero)
                {
                    //If term THEN term ELSE 0 has the exact same value as term.
                    if (LacksSideEffects)
                    {
                        stringBuilder?.AppendLine("Replacing IF exp1 THEN exp1 ELSE 0 with exp1");
                        return _preEvaluatedArgs[0];
                    }
                }
                if (_preEvaluatedArgs[0].Equals(_conditionalChildren[1]) && !_conditionalChildren[1].IsZero)
                {
                    //IF term THEN .. ELSE term: else branch can be replaced with zero.
                    //However, if BOTH are already zero no substitution can take place
                    stringBuilder?.AppendLine("Replacing IF exp1 THEN exp2 ELSE exp1 with IF exp1 THEN exp2 ELSE 0");
                    return ReplaceConditionalChild(1, CreateChain(_conditionalChildren[1],
                                                                  CreateConstantCallTree(0, constants)));
                }
            }
            return null;
        }

        private CallTree SimplifyConditionalEvaluation(StringBuilder stringBuilder)
        {
            switch (_symbol)
            {
                case Symbols.If:
                    {
                        if (Lhs.IsConstant)
                        {
                            if (Lhs.Bresult)
                            {
                                //IF true THEN expr => expr
                                stringBuilder?.AppendLine("Replacing IF true THEN expr with expr");

                                return Rhs;
                            }
                            //IF false THEN expr => noop
                            stringBuilder?.AppendLine("Replacing IF false THEN expr with Noop");
                            return new CallTree(Symbols.Noop, _varnumber);
                        }
                        break;
                    }
                case Symbols.While:
                    {
                        if (_conditionalChildren[0].IsConstant && !_conditionalChildren[0].Bresult)
                        {
                            stringBuilder?.AppendLine("Replacing WHILE false DO expr with Noop");
                            //WHILE false do expr => noop
                            return new CallTree(Symbols.Noop, _varnumber);
                        }
                        break;
                    }
                    //Ternary operator(s):
                case Symbols.Ifelse:
                    {
                        if (_preEvaluatedArgs[0].IsConstant)
                        {
                            //IF true THEN expr1 ELSE expr2 => expr1
                            //IF false THEN expr1 ELSE expr2 => expr2
                            stringBuilder?.AppendLine(
                                "Replacing IF true|false THEN expr1 ELSE expr2 with expr1 or expr2");
                            return _preEvaluatedArgs[0].Bresult ? _conditionalChildren[0] : _conditionalChildren[1];
                        }

                        if (_preEvaluatedArgs[0]._symbol == Symbols.Not)
                        {
                            //IIF (not exp) THEN exp1 ELSE exp2 => IF exp THEN exp2 else EXP1
                            stringBuilder?.AppendLine(
                                "Replacing IF NOT(true|false) THEN expr1 ELSE expr2 with expr1 or expr2");
                            return new CallTree(_symbol, _varnumber, new[] {_preEvaluatedArgs[0].Operand},
                                                new[] {_conditionalChildren[1], _conditionalChildren[0]});
                        }
                        break;
                    }
            }
            return null;
        }

        private CallTree LimitToZerOrOne(ConstantsSet constants, StringBuilder stringBuilder)
        {
            //unless already of bool type
            if (IsBoolean)
            {
                return this;
            }

            if (ResultIsNeverZero)
            {
                stringBuilder?.AppendLine("Replacing expression that can never be zero with true");
                //If the result cannot be zero we replace this expression with an expression returning true
                return CreateChain(constants, this, true);
            }

            //Wrap this expression exp in "(0 <> exp)"
            stringBuilder?.AppendLine("Wrapping expression in 0 <> exp to be used as boolean");
            return new CallTree(Symbols.Neq, _varnumber,
                                new[] {CreateConstantCallTree(0, constants), this});
        }

        private CallTree UnwrapLimitationToZeroOrOne(CallTree lhs, CallTree rhs, StringBuilder stringBuilder)
        {
            if (_symbol == Symbols.Neq && lhs.IsZero)
            {
                stringBuilder?.AppendLine("Unwrapping 0 <> exp as exp");
                //(0 <> exp) => exp
                return rhs;
            }

            if (_symbol == Symbols.Eq && lhs.IsZero)
            {
                stringBuilder?.AppendLine("Replacing 0 = exp with NOT(exp)");
                //(0 = exp) => NOT(exp)
                return rhs.WrapExpressionInNegation();
            }

            return null;
        }

        private CallTree UnwrapBooleanExpression(ConstantsSet constants, StringBuilder stringBuilder)
        {
            if (_symbol == Symbols.Eq || _symbol == Symbols.Neq)
            {
                CallTree callTree = UnwrapLimitationToZeroOrOne(Lhs, Rhs, stringBuilder);
                return callTree ?? UnwrapLimitationToZeroOrOne(Rhs, Lhs, stringBuilder);
            }

            if (_symbol == Symbols.If)
            {
                if (LacksSideEffects)
                {
                    if (_preEvaluatedArgs[0]._symbol == Symbols.Not &&
                        _preEvaluatedArgs[0].Operand.Equals(_conditionalChildren[0]))
                    {
                        stringBuilder?.AppendLine(
                            "Replacing IF exp THEN NOT(exp) with false");
                        //if not X then X is always zero/false and may be replaced with constant if there are no side effects.
                        return CreateConstantCallTree(false, constants);
                    }

                    if (_conditionalChildren[0]._symbol == Symbols.Not &&
                        _conditionalChildren[0].Operand.Equals(_preEvaluatedArgs[0]))
                    {
                        stringBuilder?.AppendLine(
                            "Replacing IF NOT(exp) THEN exp with false");

                        //if not X then X is always zero/false and may be replaced with constant zero if there are no side effects.
                        return CreateConstantCallTree(0, constants);
                    }

                    if (_preEvaluatedArgs[0].Equals(_conditionalChildren[0]))
                    {
                        //If term then term can never be zero because if term is zero the result is NaN.
                        stringBuilder?.AppendLine("Replacing IF term THEN term with term");
                        return CreateChain(constants, this, true);
                    }
                }
            }
            if (_symbol == Symbols.Ifelse)
            {
                if (_conditionalChildren[0].IsConstant && _conditionalChildren[1].IsConstant)
                {
                    if (_conditionalChildren[0].Bresult && !_conditionalChildren[1].Bresult)
                    {
                        //This is just a conversion to bool. We want to use this expression as a bool anyway.
                        //IF exp THEN 3 ELSE 0 => exp
                        stringBuilder?.AppendLine("Replacing IF exp THEN true ELSE false with exp");
                        return _preEvaluatedArgs[0];
                    }

                    if (!_conditionalChildren[0].Bresult && _conditionalChildren[1].Bresult)
                    {
                        //This is just a negated bool expression. We want to use this experssion as a bool anyway.
                        //IF exp THEN 0 ELSE 3 => NOT(exp)
                        stringBuilder?.AppendLine("Replacing IF exp THEN false ELSE true with NOT(exp)");
                        return _preEvaluatedArgs[0].WrapExpressionInNegation();
                    }


                    if (_conditionalChildren[0].Bresult == _conditionalChildren[1].Bresult)
                    {
                        //Here the result of the guard is discarded and we just use the boolean result of either child.
                        //IF exp THEN 2 ELSE 3 => exp=>1
                        //IF exp THEN 0 ELSE 0 => exp=>0
                        stringBuilder?.AppendLine("Replacing IF exp THEN true|false ELSE true|false with true|false");
                        return CreateChain(constants, _preEvaluatedArgs[0], _conditionalChildren[0].Bresult);
                    }
                }
            }


            return null;
        }

        private CallTree CreateConstantCallTree(bool constant, ConstantsSet constants)
        {
            return CreateConstantCallTree(constant ? 1 : 0, constants);
        }

        private CallTree CreateConstantCallTree(decimal constant, ConstantsSet constants, StringBuilder stringBuilder)
        {
            if (constant == (int)(constant))
            {
                return CreateConstantCallTree((int) constant, constants);
            }

            var callTree = new CallTree(Symbols.DoubleLiteral, _varnumber,
                                        constants.Doubles.Include(constant));
            //Cannot be further simplified
            if (!callTree.Tick(constants, stringBuilder))
            {
                throw new InvalidOperationException("CallTree IsConstant but Tick() returns false");
            }
            return callTree;
        }

        private CallTree CreateConstantCallTree(int constant, ConstantsSet constants)
        {
            return new CallTree(Symbols.IntegerLiteral, _varnumber, constants.Integers.Include(constant));
        }

        private CallTree ReplaceExpressionWithConstant(ConstantsSet constants, StringBuilder stringBuilder)
        {
            //If all args are constants and there are no conditional args we can
            //tick this exactly once and use the result as a new constant that we return.
            switch (_symbol)
            {
                case Symbols.InputArgument:
                case Symbols.WorkingVariable:
                case Symbols.AssignWorkingVariable:
                case Symbols.Mov:
                case Symbols.While:
                    return null;
            }
            foreach (CallTree callTree in _preEvaluatedArgs)
            {
                if (!callTree.IsConstant)
                {
                    return null;
                }
            }

            foreach (CallTree callTree in _conditionalChildren)
            {
                if (!callTree.IsConstant)
                {
                    return null;
                }
            }

            /*for all operations on constants:
                (constant1 Op constant2) => constant3
                NOT true => false
                NOT false => true
             */
            _preEvaluatedArgIndex = _preEvaluatedArgs.Length;
            if (!Tick(constants, stringBuilder))
            {
                throw new InvalidOperationException("All pre-evaluated arguments are constants but Tick() returns false");
            }

            stringBuilder?.AppendLine("Replacing expression with only constant arguments with constant");
            return CreateConstantCallTree(Result, constants, stringBuilder);
        }

        private CallTree SimplifyPreEvaluatedArgs(ConstantsSet constants, StringBuilder stringBuilder)
        {
            for (int i = 0; i < _preEvaluatedArgs.Length; ++i)
            {
                CallTree simplyfiedCallTree = _preEvaluatedArgs[i].Simplify(constants, stringBuilder);
                if (TakesBooleanPreEvaluatedArgs)
                {
                    CallTree unwrappedCallTree =
                        (simplyfiedCallTree ?? _preEvaluatedArgs[i]).UnwrapBooleanExpression(constants, stringBuilder);
                    simplyfiedCallTree = unwrappedCallTree ?? simplyfiedCallTree;
                }

                if (simplyfiedCallTree != null)
                {
                    return ReplacePreEvaluatedArg(i, simplyfiedCallTree);
                }
            }
            return null;
        }

        private CallTree SimplifyConditionalChildren(ConstantsSet constants, StringBuilder stringBuilder)
        {
            for (int i = 0; i < _conditionalChildren.Length; ++i)
            {
                CallTree simplyfiedCallTree = _conditionalChildren[i].Simplify(constants, stringBuilder);
                if (TakesBooleanConditionalChildren &&
                    (i == 0 || _symbol != Symbols.While))
                {
                    //For a while loop we can remove any comparisons with 0 around the guard expression
                    CallTree unwrappedCallTree =
                        (simplyfiedCallTree ?? _conditionalChildren[i]).UnwrapBooleanExpression(constants, stringBuilder);
                    simplyfiedCallTree = unwrappedCallTree ?? simplyfiedCallTree;
                }
                if (simplyfiedCallTree != null)
                {
                    return ReplaceConditionalChild(i, simplyfiedCallTree);
                }
            }
            return null;
        }

        public IEnumerable<int> Encode()
        {
            yield return Codec.EncodeSymbol(_varnumber, _symbol, _qualifier);
            foreach (CallTree callTree in _preEvaluatedArgs)
            {
                foreach (int code in callTree.Encode())
                {
                    yield return code;
                }
            }
            foreach (CallTree callTree in _conditionalChildren)
            {
                foreach (int code in callTree.Encode())
                {
                    yield return code;
                }
            }
        }

        public bool Equals(CallTree other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (other._symbol != _symbol)
            {
                return false;
            }

            if (other._qualifier != _qualifier)
            {
                return false;
            }

            if (other._preEvaluatedArgs.Length != _preEvaluatedArgs.Length)
            {
                return false;
            }
            for (int i = 0; i < _preEvaluatedArgs.Length; ++i)
            {
                if (!other._preEvaluatedArgs[i].Equals(_preEvaluatedArgs[i]))
                {
                    return false;
                }
            }

            if (other._conditionalChildren.Length != _conditionalChildren.Length)
            {
                return false;
            }

            for (int i = 0; i < _conditionalChildren.Length; ++i)
            {
                if (!other._conditionalChildren[i].Equals(_conditionalChildren[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (CallTree)) return false;
            return Equals((CallTree) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (_conditionalChildren != null ? _conditionalChildren.GetHashCode() : 0);
                result = (result*397) ^ (_preEvaluatedArgs != null ? _preEvaluatedArgs.GetHashCode() : 0);
                result = (result*397) ^ _qualifier;
                result = (result*397) ^ _symbol.GetHashCode();
                result = (result*397) ^ _varnumber;
                result = (result*397) ^ _conditionalChildrenIndex;
                result = (result*397) ^ _preEvaluatedArgIndex;
                result = (result*397) ^ _result.GetHashCode();
                return result;
            }
        }

        public CallTree PurgeUnassignedVariables(ConstantsSet constants, List<int> assignedVariables,
            StringBuilder stringBuilder)
        {
            if (IsConstant || _symbol == Symbols.InputArgument)
            {
                //A constant or input argument can have no unassigned variables to purge.
                return null;
            }

            if (_symbol == Symbols.WorkingVariable)
            {
                if ((assignedVariables.IndexOf(_qualifier) < 0))
                {
                    //Ha! This variable is used but has NOT been assiged! Replace with constant zero!
                    return CreateConstantCallTree(0, constants);
                }
                //If it is already assigned we cannot do more.
                return null;
            }

            if (_symbol == Symbols.AssignWorkingVariable)
            {
                if (Operand.IsConstant)
                {
                    Operand.Tick(constants, stringBuilder);
                }

                if (assignedVariables.IndexOf(_qualifier) < 0)
                {
                    if (Operand.IsZero)
                    {
                        //Assignment to 0 does not make the unassigned variable more assigned than it was.
                        //So replace this assignmet of unassigned to 0 with 0.
                        return CreateConstantCallTree(0, constants);
                    }
                    //Otherwise we now consider it assigned.
                    assignedVariables.Add(_qualifier);
                }
                return null;
            }

            if (_symbol == Symbols.While)
            {
                //If this is a while loop we have to consider variables assigned in the body or guard as already assigned
                //when we are about to evaluate the body or the guard!
                FindAssignedVariables(constants, assignedVariables, stringBuilder);
            }

            //For a IF THEN ELSE the guard can initialise variables that are also initialised in either branch, but their
            //initialisation in one branch is not visible from the other.
            if (_symbol == Symbols.Ifelse)
            {
                CallTree callTree = _preEvaluatedArgs[0].PurgeUnassignedVariables(constants, assignedVariables, stringBuilder);
                if (callTree != null)
                {
                    return CreateTernaryCallTree(callTree, _conditionalChildren[0], _conditionalChildren[1]);
                }
                var yesBranchAssignedVariables = new List<int>(assignedVariables);
                callTree = _conditionalChildren[0].PurgeUnassignedVariables(constants, yesBranchAssignedVariables, stringBuilder);
                if (callTree != null)
                {
                    Merge(assignedVariables, yesBranchAssignedVariables);
                    return CreateTernaryCallTree(_preEvaluatedArgs[0], callTree, _conditionalChildren[1]);
                }

                var noBranchAssignedVariables = new List<int>(assignedVariables);
                callTree = _conditionalChildren[1].PurgeUnassignedVariables(constants, noBranchAssignedVariables, stringBuilder);
                if (callTree != null)
                {
                    Merge(assignedVariables, noBranchAssignedVariables);
                    return CreateTernaryCallTree(_preEvaluatedArgs[0], _conditionalChildren[0], callTree);
                }

                //Now we have to merge the assigned variables with those that were assigned on either side.
                //(i.e. after the if then else we have to consider all three sides to have been possibly executed).
                Merge(assignedVariables, yesBranchAssignedVariables);
                Merge(assignedVariables, noBranchAssignedVariables);
            }
            else
            {
                //For all other symbols we assume that all children are executed in order.
                for (int i = 0; i < _preEvaluatedArgs.Length; ++i)
                {
                    CallTree callTree = _preEvaluatedArgs[i].PurgeUnassignedVariables(constants, assignedVariables, stringBuilder);
                    if (callTree != null)
                    {
                        return ReplacePreEvaluatedArg(i, callTree);
                    }
                }
                for (int i = 0; i < _conditionalChildren.Length; ++i)
                {
                    CallTree callTree = _conditionalChildren[i].PurgeUnassignedVariables(constants, assignedVariables, stringBuilder);
                    if (callTree != null)
                    {
                        return ReplaceConditionalChild(i, callTree);
                    }
                }
            }
            //Now, for a If Then Else 
            return null;
        }

        private CallTree ReplaceConditionalChild(int i, CallTree simplyfiedCallTree)
        {
            var callTree = new CallTree(this);
            callTree._conditionalChildren[i] = simplyfiedCallTree;
            return callTree;
        }

        private CallTree ReplacePreEvaluatedArg(int i, CallTree simplyfiedCallTree)
        {
            var callTree = new CallTree(this);
            callTree._preEvaluatedArgs[i] = simplyfiedCallTree;
            return callTree;
        }

        private static void Merge(IList<int> variables, IEnumerable<int> assignedVariables)
        {
            foreach (int variable in assignedVariables)
            {
                if (variables.IndexOf(variable) < 0)
                {
                    variables.Add(variable);
                }
            }
        }

        private CallTree CreateTernaryCallTree(CallTree guard, CallTree yes, CallTree no)
        {
            return new CallTree(Symbols.Ifelse, _varnumber, new[] {guard}, new[] {yes, no});
        }

        private void FindAssignedVariables(ConstantsSet constants, IList<int> assignedVariables, StringBuilder stringBuilder)
        {
            if (_symbol == Symbols.AssignWorkingVariable)
            {
                if (Operand.IsConstant)
                {
                    Operand.Tick(constants, stringBuilder);
                }
                if (!Operand.IsZero)
                {
                    //Assignment to 0 does not make the unassigned variable more assigned than it was, otherwise it is now assigned.
                    if (assignedVariables.IndexOf(_qualifier) < 0)
                    {
                        assignedVariables.Add(_qualifier);
                    }
                }
            }
            else
            {
                foreach (CallTree callTree in _preEvaluatedArgs)
                {
                    callTree.FindAssignedVariables(constants, assignedVariables, stringBuilder);
                }
                foreach (CallTree callTree in _conditionalChildren)
                {
                    callTree.FindAssignedVariables(constants, assignedVariables, stringBuilder);
                }
            }
        }

        public CallTree PurgeUnusedAssignmentsToVariables(ConstantsSet constants, List<int> usedVariables,
            StringBuilder stringBuilder)
        {
            if (IsConstant || _symbol == Symbols.InputArgument)
            {
                //A constant or input argument can have no unassigned variables to purge.
                return null;
            }

            if (_symbol == Symbols.WorkingVariable)
            {
                if ((usedVariables.IndexOf(_qualifier) < 0))
                {
                    //This variable is used.
                    usedVariables.Add(_qualifier);
                }
                //If it is actually used we cannot do more.
                return null;
            }

            if (_symbol == Symbols.AssignWorkingVariable)
            {
                if (usedVariables.IndexOf(_qualifier) < 0)
                {
                    //This variable is assigned but never used! Just use the operand as value.
                    return Operand;
                }
                //This assigned variable is actually used, so we cannot do more.
                return null;
            }

            if (_symbol == Symbols.While)
            {
                //If this is a while loop we have to consider variables used in the guard or the body
                //as already used when we are about to evaluate the body or the guard!
                FindUsedVariables(constants, usedVariables, stringBuilder);
            }

            //For a IF THEN ELSE the guard can initialise variables that are also initialised in either branch, but their
            //initialisation in one branch is not visible from the other.
            if (_symbol == Symbols.Ifelse)
            {
                var noBranchUsedVariables = new List<int>(usedVariables);
                CallTree callTree = _conditionalChildren[1].PurgeUnusedAssignmentsToVariables(constants,
                                                                                              noBranchUsedVariables, stringBuilder);
                if (callTree != null)
                {
                    Merge(usedVariables, noBranchUsedVariables);
                    return CreateTernaryCallTree(_preEvaluatedArgs[0], _conditionalChildren[0], callTree);
                }

                var yesBranchUsedVariables = new List<int>(usedVariables);
                callTree = _conditionalChildren[0].PurgeUnusedAssignmentsToVariables(constants, yesBranchUsedVariables, stringBuilder);
                if (callTree != null)
                {
                    Merge(usedVariables, yesBranchUsedVariables);
                    return CreateTernaryCallTree(_preEvaluatedArgs[0], callTree, _conditionalChildren[1]);
                }

                //Now we have to merge the used variables with those that were used on either side.
                //(i.e. after the if then else we have to consider all three sides to have been possibly executed).
                Merge(usedVariables, noBranchUsedVariables);
                Merge(usedVariables, yesBranchUsedVariables);

                callTree = _preEvaluatedArgs[0].PurgeUnusedAssignmentsToVariables(constants, usedVariables, stringBuilder);
                if (callTree != null)
                {
                    return CreateTernaryCallTree(callTree, _conditionalChildren[0], _conditionalChildren[1]);
                }
            }
            else
            {
                for (int i = _conditionalChildren.Length - 1; i >= 0; --i)
                {
                    CallTree callTree = _conditionalChildren[i].PurgeUnusedAssignmentsToVariables(constants,
                                                                                                  usedVariables, stringBuilder);
                    if (callTree != null)
                    {
                        return ReplaceConditionalChild(i, callTree);
                    }
                }

                //For all other symbols we assume that all children are executed in order.
                for (int i = _preEvaluatedArgs.Length - 1; i >= 0; --i)
                {
                    CallTree callTree = _preEvaluatedArgs[i].PurgeUnusedAssignmentsToVariables(constants, usedVariables, stringBuilder);
                    if (callTree != null)
                    {
                        return ReplacePreEvaluatedArg(i, callTree);
                    }
                }
            }
            //Now, for a If Then Else 
            return null;
        }

        private void FindUsedVariables(ConstantsSet constants, IList<int> usedVariables, StringBuilder stringBuilder)
        {
            if (_symbol == Symbols.WorkingVariable)
            {
                if (usedVariables.IndexOf(_qualifier) < 0)
                {
                    usedVariables.Add(_qualifier);
                }
            }
            else
            {
                foreach (CallTree callTree in _preEvaluatedArgs)
                {
                    callTree.FindUsedVariables(constants, usedVariables, stringBuilder);
                }
                foreach (CallTree callTree in _conditionalChildren)
                {
                    callTree.FindAssignedVariables(constants, usedVariables, stringBuilder);
                }
            }
        }

        public CallTree PurgeRedundantUsagesOfAssignedVariables(ConstantsSet constants,
                                                                Dictionary<int, CallTree> replaceableAssignments)
        {
            //Whenever we can prove the invariant, that the value of a variable is equivalent to an expression being side-effects
            //neutral, then we can replace usages of that variable with the expression. This can initially lead to longer code
            //but as a final step, side effects neutral expressions of a sufficient length, occuring repeatedly in the
            //code, can be replaced with variables assigned to that value.
            if (IsConstant || _symbol == Symbols.InputArgument)
            {
                //A constant or input argument is not a working variable.
                return null;
            }

            if (_symbol == Symbols.WorkingVariable)
            {
                CallTree callTree;
                if ((replaceableAssignments.TryGetValue(_qualifier, out callTree)))
                {
                    //This variable is used but has ONLY been assiged to the callTree which will always render the same
                    //result. Replace with this call tree.
                    return callTree;
                }
                //If variable can have different values we have to keep it.
                return null;
            }

            //Now go through all children to remove any variable who's value we can determine as identical to an expression
            for (int i = 0; i < _preEvaluatedArgs.Length; ++i)
            {
                CallTree callTree = _preEvaluatedArgs[i].PurgeRedundantUsagesOfAssignedVariables(constants,
                                                                                                 replaceableAssignments);
                if (callTree != null)
                {
                    return ReplacePreEvaluatedArg(i, callTree);
                }
            }

            for (int i = 0; i < _conditionalChildren.Length; ++i)
            {
                CallTree callTree = _conditionalChildren[i].PurgeRedundantUsagesOfAssignedVariables(constants,
                                                                                                    replaceableAssignments);
                if (callTree != null)
                {
                    return ReplaceConditionalChild(i, callTree);
                }
            }

            //Now try to determine the ultimate value of certain varibles after this expression.
            FindUltimateVariableAssignment(replaceableAssignments);

            return null;
        }

        private void FindUltimateVariableAssignment(IDictionary<int, CallTree> replaceableAssignments)
        {
            if (_symbol == Symbols.AssignWorkingVariable)
            {
                if (Operand.SideEffectsNeutral)
                {
                    //We found a variable assignment and the Operand is side effects neutral. We can prove that from now on,
                    //the variable will be assigned to the same expression and it's usages can be subsituted for the
                    //expression until a further assignmet occurs.
                    replaceableAssignments[_qualifier] = Operand;
                }
                else
                {
                    //Because the expression was not side effects neutral, indicate that from now on,
                    //the value of the variable is unknown and it cannot be replaced with anything.
                    replaceableAssignments.Remove(_qualifier);
                }
            }
            else
            {
                //If the pre-evaluated (unconditional children) assigns the variable to anything, then the final assignment will prevail.
                foreach (CallTree callTree in _preEvaluatedArgs)
                {
                    callTree.FindUltimateVariableAssignment(replaceableAssignments);
                }

                if (_symbol != Symbols.Ifelse)
                {
                    //But if the conditioal children ever assigns it to anything ELSE than we only know it is, then we
                    //cannot know the final value, so it has to be cleared from the table of replaceable variables.
                    //However, the exception is an IfElse statement where both branches do the SAME assignment.
                    foreach (CallTree callTree in _conditionalChildren)
                    {
                        callTree.ClearChangingVariables(replaceableAssignments);
                    }
                }
                else
                {
                    //If the yes branch and the no branch of the if then else does the exact same ultimate assignments
                    //then we can keep them (intersection of two sets)
                    var yesBranchReplaceableAssignments = new Dictionary<int, CallTree>(replaceableAssignments);
                    _conditionalChildren[0].FindUltimateVariableAssignment(yesBranchReplaceableAssignments);
                    var noBranchReplaceableAssignments = new Dictionary<int, CallTree>(replaceableAssignments);
                    _conditionalChildren[1].FindUltimateVariableAssignment(noBranchReplaceableAssignments);

                    replaceableAssignments.Clear();
                    //This is an ad-hoc implementation of an set intersection operation.
                    foreach (var kvp in yesBranchReplaceableAssignments)
                    {
                        CallTree callTree;
                        if (noBranchReplaceableAssignments.TryGetValue(kvp.Key, out callTree) &&
                            callTree.Equals(kvp.Value))
                        {
                            replaceableAssignments[kvp.Key] = kvp.Value;
                        }
                        else
                        {
                            replaceableAssignments.Remove(kvp.Key);
                        }
                    }
                }
            }
        }

        private void ClearChangingVariables(IDictionary<int, CallTree> replaceableAssignments)
        {
            if (_symbol == Symbols.AssignWorkingVariable)
            {
                if (replaceableAssignments.TryGetValue(_qualifier, out var callTree) && !callTree.Equals(Operand))
                {
                    //Here we found an assignment that CHANGES the current value of the variable.
                    //Make sure it is no longer marked as replaceable.
                    replaceableAssignments.Remove(_qualifier);
                }
            }
            else
            {
                //Handle this recursively.
                foreach (CallTree callTree in _preEvaluatedArgs)
                {
                    callTree.ClearChangingVariables(replaceableAssignments);
                }
                foreach (CallTree callTree in _conditionalChildren)
                {
                    callTree.ClearChangingVariables(replaceableAssignments);
                }
            }
        }

        #region Nested type: PossibleSign

        [Flags]
        private enum PossibleSign
        {
            Positive = 1,
            Negative = 2,
            Zero = 4
        };

        #endregion
    }
}