using System;
using System.Collections.Generic;
using System.Text;
using Tokenizer;

namespace Tokenizer
{
    public static class Tokenizer
    {
        public static IEnumerable<Token> Tokenize(IEnumerable<char> text)
        {

            var stateMachine = new StateMachine();

            foreach (var symbol in text)
            {
                foreach (Token token in stateMachine.Accept(symbol))
                {
                    yield return token;
                }
            }

            foreach (Token token in stateMachine.AcceptEndOfStream())
            {
                yield return token;
            }
        }

        #region Private
        class StateMachine
        {

            #region Public
            
            static StateMachine()
            {
                TransitionTable = CreateTransitionTable();
                DefaultTransitionTable = CreateDefaultTransitionTable();
                AcceptStateTable = CreateAcceptStateTable();
            }


            public IEnumerable<Token> Accept(char symbol)
            {
                EState nextState;
                if (TransitionTable.TryGetValue(Tuple.Create(_state, Categorize(symbol)), out nextState))
                {
                    _state = nextState;
                    if (_state != EState.Start)
                    {
                        _buffer.Append(symbol);
                        Token token;
                        if (InterpretAcceptState(out token))
                        {
                            yield return token;
                        }
                    }
                }
                else if (DefaultTransitionTable.TryGetValue(_state, out nextState))
                {
                    _state = nextState;
                    Token token;
                    if (InterpretAcceptState(out token))
                    {
                        yield return token;
                    }

                    //Don't consume the symbol, run the symbol again.
                    foreach (var t in Accept(symbol))
                        yield return t;
                }
                else
                {
                    throw new TokenizerException(string.Format("Unexpected symbol '{0}' in state {1}", symbol, _state));
                }

            }

            public IEnumerable<Token> AcceptEndOfStream()
            {
                if (_state != EState.Start)
                {
                    EState nextState;
                    EState oldState = _state;
                    if (DefaultTransitionTable.TryGetValue(_state, out nextState))
                    {
                        _state = nextState;
                        Token token;
                        if (InterpretAcceptState(out token))
                        {
                            yield return token;
                        }
                        else
                        {
                            throw new TokenizerException(string.Format("Internal error: DefaultTransitionTable[{0}] gave {1} but InterpretAcceptState returns false.", oldState, _state));
                        }
                    }
                    else
                    {
                        throw new TokenizerException(string.Format("Unexpected end of stream in state {0}", _state));
                    }
                }
            }
            
            #endregion


            #region Private

            private bool InterpretAcceptState(out Token token)
            {
                Token.ETokenTypes tokenType;
                if (AcceptStateTable.TryGetValue(_state, out tokenType))
                {
                    token = new Token(tokenType, _buffer.ToString());
                    _state = EState.Start;
                    _buffer = new StringBuilder();
                    return true;
                }
                token = null;
                return false;
            }


            private static ESymbolClasses Categorize(char symbol)
            {
                if (char.IsLetter(symbol))
                {
                    return ESymbolClasses.Letter;
                }

                if (char.IsDigit(symbol))
                {
                    return ESymbolClasses.Digit;
                }

                if (char.IsWhiteSpace(symbol))
                {
                    return ESymbolClasses.Whitespace;
                }
                
                switch (symbol)
                {
                    case '.':
                        return ESymbolClasses.Dot;
                    case '<':
                        return ESymbolClasses.Lt;
                    case '>':
                        return ESymbolClasses.Gt;
                    case '=':
                        return ESymbolClasses.Eq;
                    case '+':
                        return ESymbolClasses.PlusOp;
                    case '*':
                        return ESymbolClasses.Mulop;
                    case '-':
                        return ESymbolClasses.MinusOp;
                    case '/':
                        return ESymbolClasses.DivOp;
                    case ':':
                        return ESymbolClasses.Colon;
                    case '(':
                        return ESymbolClasses.LPar;
                    case ')':
                        return ESymbolClasses.RPar;
                    default:
                        throw new TokenizerException(string.Format("Unexpected symbol '{0}'", symbol));
                }

            }

            private enum EState
            {
                Start,
                Identifier,
                NumberLiteral,
                FloatLiteral,
                Lt,
                Gt,
                AcceptEq,
                AcceptLPar,
                AcceptRPar,
                Colon,
                AcceptIdentifier,
                AcceptDivOp,
                AcceptMulOp,
                AcceptPlusOp,
                MinusOp,
                AcceptMinusOp,
                NumberLiteralDot,
                AcceptNumberLiteral,
                AcceptFloatLiteral,
                AcceptCommaOp,
                AcceptAssigmentOp,
                AcceptColon,
                AcceptGt,
                AcceptLt,
                Eq,
                AcceptChain,
                AcceptLtEq,
                AcceptNeq,
                AcceptGtEq
            };

            private enum ESymbolClasses
            {
                Letter,
                Digit,
                Lt,
                Gt,
                Eq,
                PlusOp,
                Mulop,
                MinusOp,
                DivOp,
                Colon,
                LPar,
                RPar,
                Dot,
                Whitespace,
                Comma
            }


            private static IDictionary<Tuple<EState, ESymbolClasses>, EState> CreateTransitionTable()
            {
                var transitionTable = new Dictionary<Tuple<EState, ESymbolClasses>, EState>
                                          {
                                              {Tuple.Create(EState.Start, ESymbolClasses.Letter), EState.Identifier},
                                              {Tuple.Create(EState.Start, ESymbolClasses.Digit), EState.NumberLiteral},
                                              {Tuple.Create(EState.Start, ESymbolClasses.Colon), EState.Colon},
                                              {Tuple.Create(EState.Start, ESymbolClasses.Gt), EState.Gt},
                                              {Tuple.Create(EState.Start, ESymbolClasses.Lt), EState.Lt},
                                              {Tuple.Create(EState.Start, ESymbolClasses.Eq), EState.Eq},
                                              {Tuple.Create(EState.Start, ESymbolClasses.LPar), EState.AcceptLPar},
                                              {Tuple.Create(EState.Start, ESymbolClasses.RPar), EState.AcceptRPar},
                                              {Tuple.Create(EState.Start, ESymbolClasses.DivOp), EState.AcceptDivOp},
                                              {Tuple.Create(EState.Start, ESymbolClasses.Mulop), EState.AcceptMulOp},
                                              {Tuple.Create(EState.Start, ESymbolClasses.PlusOp), EState.AcceptPlusOp},
                                              {Tuple.Create(EState.Start, ESymbolClasses.MinusOp), EState.MinusOp},
                                              {Tuple.Create(EState.Start, ESymbolClasses.Comma), EState.AcceptCommaOp},
                                              {Tuple.Create(EState.Start, ESymbolClasses.Whitespace), EState.Start},
                                              {Tuple.Create(EState.Identifier, ESymbolClasses.Letter), EState.Identifier},
                                              {Tuple.Create(EState.Identifier, ESymbolClasses.Digit), EState.Identifier},
                                              {Tuple.Create(EState.NumberLiteral, ESymbolClasses.Digit),EState.NumberLiteral},
                                              {Tuple.Create(EState.NumberLiteral, ESymbolClasses.Dot),EState.NumberLiteralDot},
                                              {Tuple.Create(EState.NumberLiteralDot, ESymbolClasses.Digit),EState.FloatLiteral},
                                              {Tuple.Create(EState.FloatLiteral, ESymbolClasses.Digit), EState.FloatLiteral},
                                              {Tuple.Create(EState.Colon, ESymbolClasses.Eq), EState.AcceptAssigmentOp},
                                              {Tuple.Create(EState.Lt, ESymbolClasses.Eq), EState.AcceptLtEq},
                                              {Tuple.Create(EState.Lt, ESymbolClasses.Gt), EState.AcceptNeq},
                                              {Tuple.Create(EState.Gt, ESymbolClasses.Eq), EState.AcceptGtEq},
                                              {Tuple.Create(EState.Eq, ESymbolClasses.Gt), EState.AcceptChain},
                                              {Tuple.Create(EState.MinusOp, ESymbolClasses.Digit), EState.NumberLiteral}
                                          };


                return transitionTable;
            }


            private static IDictionary<EState, EState> CreateDefaultTransitionTable()
            {
                var defaultTransitionTable = new Dictionary<EState, EState>
                                                 {
                                                     {EState.Identifier, EState.AcceptIdentifier},
                                                     {EState.NumberLiteral, EState.AcceptNumberLiteral},
                                                     {EState.FloatLiteral, EState.AcceptFloatLiteral},
                                                     {EState.Colon, EState.AcceptColon},
                                                     {EState.Gt, EState.AcceptGt},
                                                     {EState.Lt, EState.AcceptLt},
                                                     {EState.Eq, EState.AcceptEq},
                                                     {EState.MinusOp, EState.AcceptMinusOp}
                                                 };

                return defaultTransitionTable;

            }

            private static IDictionary<EState, Token.ETokenTypes> CreateAcceptStateTable()
            {
                var acceptStateTable = new Dictionary<EState, Token.ETokenTypes>
                                           {
                                               {EState.AcceptChain, Token.ETokenTypes.Chain},
                                               {EState.AcceptAssigmentOp, Token.ETokenTypes.Assignment},
                                               {EState.AcceptCommaOp, Token.ETokenTypes.Comma},
                                               {EState.AcceptDivOp, Token.ETokenTypes.Division},
                                               {EState.AcceptEq, Token.ETokenTypes.Equals},
                                               {EState.AcceptFloatLiteral, Token.ETokenTypes.FloatLiteral},
                                               {EState.AcceptGt, Token.ETokenTypes.Gt},
                                               {EState.AcceptGtEq, Token.ETokenTypes.GtEq},
                                               {EState.AcceptIdentifier, Token.ETokenTypes.Identifier},
                                               {EState.AcceptLPar, Token.ETokenTypes.LeftPar},
                                               {EState.AcceptLt, Token.ETokenTypes.Lt},
                                               {EState.AcceptLtEq, Token.ETokenTypes.Lteq},
                                               {EState.AcceptMinusOp, Token.ETokenTypes.Minus},
                                               {EState.AcceptMulOp, Token.ETokenTypes.Multiplication},
                                               {EState.AcceptNeq, Token.ETokenTypes.Neq},
                                               {EState.AcceptNumberLiteral, Token.ETokenTypes.IntegerLiteral},
                                               {EState.AcceptPlusOp, Token.ETokenTypes.Plus},
                                               {EState.AcceptRPar, Token.ETokenTypes.RightPar}
                                           };

                return acceptStateTable;
            }

            private EState _state = EState.Start;
            private StringBuilder _buffer = new StringBuilder();

            private static  readonly IDictionary<Tuple<EState, ESymbolClasses>, EState> TransitionTable;
            private static readonly IDictionary<EState, EState> DefaultTransitionTable;
            private static readonly IDictionary<EState, Token.ETokenTypes> AcceptStateTable;

            #endregion


        }

        #endregion
    }

    public class TokenizerException : Exception
    {
        public TokenizerException(string s) : base(s)
        {
           
        }
    }
}