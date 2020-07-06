using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Common;
using gp;
using Tokenizer;

namespace Parser
{
    public class Parser
    {
        private readonly ConstantsSet _constantsSet;

        public static Program Parse(IEnumerable<Token> tokens)
        {
            var parser = new Parser(tokens);
            var parseTree = parser.Parse();
            if (parser._tokenEnumerator.HasNext)
            {
                throw new ParseException(string.Format("Unexpected token \"{0}\" when expecting binary operator.", parser._tokenEnumerator.Next.Lexeme));
            }
            return new Program(parser._variableCount, parseTree.GenerateCode(parser._variableCount), parser._constantsSet, parser._workingVariableCount);
        }

        private Parser(IEnumerable<Token> tokens)
        {
            _tokenEnumerator = new Lookahead<Token>(tokens);
            _constantsSet = new ConstantsSet();
        }

        private ParseTree Parse()
        {

            var operandStack = new Stack<ParseTree>();
            var operatorStack = new Stack<Token>();

            do
            {
                if (!_tokenEnumerator.MoveNext())
                {
                    throw new ParseException("Unexpected End of token stream while expecting operand.");
                }

                operandStack.Push(ParseOperand());
                if (TryConsumeOperator())
                {
                    while (operatorStack.Count > 0 && !_tokenEnumerator.Current.HasHigherPrecedence(operatorStack.Peek()))
                    {
                        PopOperatorStack(operandStack, operatorStack);
                    }
                    operatorStack.Push(_tokenEnumerator.Current);
                }
                else
                {
                    //No more operators.
                    while (operatorStack.Count > 0)
                    {
                        PopOperatorStack(operandStack, operatorStack);
                    }
                }
            } while (operatorStack.Count > 0 || operandStack.Count > 1);

            return operandStack.Pop();
        }

        private void PopOperatorStack(Stack<ParseTree> operandStack, Stack<Token> operatorStack)
        {
            Token operatorToken = operatorStack.Pop();
            if (operandStack.Count < 2)
            {
                throw new ParseException(string.Format("Unexpected token \"{0}\" when expecting operand for operator1 \"{1}\".", _tokenEnumerator.Current.Lexeme, operatorStack.Peek().Lexeme));
            }
            var rhs = operandStack.Pop();
            var lhs = operandStack.Pop();
            operandStack.Push(new ParseTree(operatorToken.Symbol, new[] { lhs, rhs }));
        }

        private bool TryConsumeOperator()
        {
            return (_tokenEnumerator.HasNext && _tokenEnumerator.Next.IsOperator() && _tokenEnumerator.MoveNext());
        }

        /// <summary>
        /// operand => (&lt;expression&gt;) | &lt;literal&gt; | &lt;variable&gt;
        /// </summary>
        /// <returns></returns>
        private ParseTree ParseOperand()
        {
            if (_tokenEnumerator.Current.TokenType == Token.ETokenTypes.LeftPar)
            {
                var parseTree = Parse();
                Consume(_tokenEnumerator, Token.ETokenTypes.RightPar);
                return parseTree;
            }
            switch (_tokenEnumerator.Current.Keyword)
            {
                case Token.EKeywords.Not:
                    return new ParseTree(Symbols.Not, Parse().AsEnumerable());
                case Token.EKeywords.If:
                    return ParseIfStatement();
                case Token.EKeywords.While:
                    return ParseWhileDo();
                case Token.EKeywords.Noop:
                    return new ParseTree(Symbols.Noop);
            }
            switch (_tokenEnumerator.Current.VariableType)
            {
                case Token.EVariableTypes.WorkingVariable:
                    int variableNumber = _tokenEnumerator.Current.VariableNumber;
                    _workingVariableCount = Math.Max(_workingVariableCount, variableNumber + 1);
                    if (TryConsume(Token.ETokenTypes.Assignment))
                    {
                        return new ParseTree(Symbols.AssignWorkingVariable, Parse().AsEnumerable(), variableNumber);
                    }
                    return new ParseTree(Symbols.WorkingVariable, variableNumber);
                case Token.EVariableTypes.InputVariable:
                    _variableCount = Math.Max(_variableCount, _tokenEnumerator.Current.VariableNumber + 1);
                    return new ParseTree(Symbols.InputArgument, _tokenEnumerator.Current.VariableNumber);
            }

            switch (_tokenEnumerator.Current.ConstantType)
            {
                case Token.EConstantTypes.IntegerConstant:
                    return new ParseTree(Symbols.IntegerLiteral, _constantsSet.Integers.Include(int.Parse(_tokenEnumerator.Current.Lexeme)));
                case Token.EConstantTypes.FloatConstant:
                    return new ParseTree(Symbols.DoubleLiteral, _constantsSet.Doubles.Include(decimal.Parse(_tokenEnumerator.Current.Lexeme, CultureInfo.GetCultureInfo("en-GB").NumberFormat)));
                    
            }
            throw new ParseException(string.Format("Unexpected token {0} when expecting operand.", _tokenEnumerator.Current.Lexeme));
        }

        private ParseTree ParseWhileDo()
        {
            var guard = Parse();
            Consume(Token.EKeywords.Do);
            return new ParseTree(Symbols.While, new [] {guard, Parse()});
        }

        private ParseTree ParseIfStatement()
        {
            var guard = Parse();
            Consume(Token.EKeywords.Then);
            var yesBranch = Parse();
            if (TryConsume(Token.EKeywords.Else))
            {
                var noBranch = Parse();
                return new ParseTree(Symbols.Ifelse, new[] {guard, yesBranch, noBranch});
            }
            return new ParseTree(Symbols.If, new[] { guard, yesBranch });
        }

        private bool TryConsume(Token.EKeywords expected)
        {
            return (_tokenEnumerator.HasNext && _tokenEnumerator.Next.Keyword == expected && _tokenEnumerator.MoveNext());
        }

        private bool TryConsume(Token.ETokenTypes expected)
        {
            return (_tokenEnumerator.HasNext && _tokenEnumerator.Next.TokenType == expected && _tokenEnumerator.MoveNext());
        }

        private void Consume(Token.EKeywords expected)
        {
            if (!_tokenEnumerator.MoveNext())
            {
                throw new ParseException(string.Format("End of program when expecting '{0}'", expected));
            }
            if (_tokenEnumerator.Current.Keyword != expected)
            {
                throw new ParseException(string.Format("Found token '{0}' when expecting '{1}'", _tokenEnumerator.Current, expected));
            }
        }


        private static void Consume(IEnumerator<Token> tokens, Token.ETokenTypes expected)
        {
            if (!tokens.MoveNext())
            {
                throw new ParseException(string.Format("End of program when expecting '{0}'", expected));
            }
            if (tokens.Current.TokenType != expected)
            {
                throw new ParseException(string.Format("Found token '{0}' when expecting '{1}'", tokens.Current, expected));
            }
        }

        private readonly Lookahead<Token> _tokenEnumerator;
        private int _variableCount;
        private int _workingVariableCount;
    }

    public class ParseException : Exception
    {
        public ParseException(string s) : base(s)
        {
        }
    }

    public class ParseTree
    {
        private readonly Symbols _symbol;
        private readonly IEnumerable<ParseTree> _children;
        private readonly int? _qualifier;

        public ParseTree(Symbols symbol)
            : this(symbol, 0)
        {
        }

        public ParseTree(Symbols symbol, IEnumerable<ParseTree> children) : this(symbol, children, null)
        {
        }

        public ParseTree(Symbols symbol, int? qualifier)
            : this(symbol, new ParseTree[] { }, qualifier)
        {
        }

        public ParseTree(Symbols symbol, IEnumerable<ParseTree> children, int? qualifier)
        {
            _symbol = symbol;
            _children = children;
            _qualifier = qualifier;
        }

        public Symbols Symbol
        {
            get { return _symbol; }
        }

        public IEnumerable<ParseTree> Children
        {
            get { return _children; }
        }

        public int? Qualifier
        {
            get { return _qualifier; }
        }

        public override string ToString()
        {
            var childrenToString = new StringBuilder();
            foreach (var child in Children)
            {
                childrenToString.Append(child.ToString());
            }
            return string.Format("{0}({1})", _symbol, childrenToString);
        }

        public bool Equals(ParseTree obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj._symbol, _symbol) && Equals(obj._children, _children) && obj._qualifier.Equals(_qualifier);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (ParseTree)) return false;
            return Equals((ParseTree) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = _symbol.GetHashCode();
                result = (result*397) ^ (_children != null ? _children.GetHashCode() : 0);
                result = (result*397) ^ (_qualifier.HasValue ? _qualifier.Value : 0);
                return result;
            }
        }


        public IEnumerable<int> GenerateCode(int variableCount)
        {
           return Codec.EncodeSymbol(variableCount, _symbol, _qualifier).AsEnumerable()
               .Concat(_children.SelectMany(_ => _.GenerateCode(variableCount)));
        }
    }
}
