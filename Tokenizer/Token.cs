using System;
using System.Collections.Generic;
using Common;

namespace Tokenizer
{
    public class Token : IEquatable<Token>
    {
       
        public enum ETokenTypes
        {
            IntegerLiteral,
            FloatLiteral,
            LeftPar,
            RightPar,
            Multiplication,
            Division,
            Plus,
            Minus,
            Assignment,
            Equals,
            Lt,
            Lteq,
            Gt,
            GtEq,
            Neq,
            Identifier,
            Chain,
            Comma
        };

        public enum EKeywords
        {
            None,
            If,
            Then,
            Else,
            While,
            Do,
            Not,
            And,
            Or,
            Xor,
            Noop
        }

        public enum EVariableTypes
        {
            None,
            InputVariable,
            WorkingVariable,
        }

        public enum EConstantTypes
        {
            None,
            IntegerConstant,
            FloatConstant,
        }

        static Token()
        {
            Keywords = GetKeywords();
            VariableTypes = GetVariableTypes();
            OperatorPrecendences = GetOperatorPrecendences();
            SymbolTable = GetSymbolTable();
            BoolOpTable = GetBoolOpTable();
        }

        public string Lexeme
        {
            get { return _lexeme; }
        }

        public ETokenTypes TokenType
        {
            get { return _tokenType; }
        }

        public EKeywords Keyword
        {
            get { return _keyword; }
        }

        public EVariableTypes VariableType
        {
            get { return _variableType; }
        }

        public EConstantTypes ConstantType
        {
            get { return _constantType;  }
        }

        public int VariableNumber
        {
            get { return _variableNumber; }
        }

        public int OperatorPrecedence
        {
            get { return _operatorPrecedence; }
        }

        public Symbols Symbol
        {
            get { return _symbol; }
        }

        public Token(ETokenTypes tokenType, string lexeme)
        {
            _tokenType = tokenType;
            _lexeme = lexeme;
            _keyword = EKeywords.None;
            _variableType = EVariableTypes.None;
            _variableNumber = 0;
            _constantType = EConstantTypes.None;
            _operatorPrecedence = -1;
            _symbol = Symbols.Noop;
            switch (_tokenType)
            {
                case ETokenTypes.Identifier:
                    {
                        EKeywords keyword;
                        if (Keywords.TryGetValue(_lexeme, out keyword))
                        {
                            _keyword = keyword;
                        }
                        else
                        {
                            int variableNumber;
                            if (int.TryParse(_lexeme.Substring(1), out variableNumber))
                            {
                                EVariableTypes variableType;
                                if (VariableTypes.TryGetValue(_lexeme[0], out variableType))
                                {
                                    _variableType = variableType;
                                    _variableNumber = variableNumber;
                                }
                            }
                        }
                        if (_keyword == EKeywords.None && _variableType == EVariableTypes.None)
                        {
                            throw new TokenizerException(string.Format("\"{0}\" is not a valid identifier: It's neither a keyword, an input variable Xn or a working variable Yn.", _lexeme));
                        }
                    }
                    break;
                case ETokenTypes.IntegerLiteral:
                    _constantType = EConstantTypes.IntegerConstant;
                    break;
                case ETokenTypes.FloatLiteral:
                    _constantType = EConstantTypes.FloatConstant;
                    break;
            }
            Symbols operatorSymbol;
            if (BoolOpTable.TryGetValue(_keyword, out operatorSymbol) || SymbolTable.TryGetValue(_tokenType, out operatorSymbol))
            {
                _symbol = operatorSymbol;
                _operatorPrecedence = OperatorPrecendences[_symbol];
            }
        }

        public bool Equals(Token obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj._lexeme, _lexeme) && Equals(obj._tokenType, _tokenType);
        }

        private string CategorizeIdentifier()
        {
            if (IsKeyword())
            {
                return "Keyword";
            }

            if (IsInputVariable())
            {
                return string.Format("InputVariable {0}", VariableNumber);
            }

            if (IsWorkingVariable())
            {
                return string.Format("WorkingVariable {0}", VariableNumber);
            }

            return "";
        }

        public override string ToString()
        {
            return string.Format("{0} \"{1}\"{2}", _tokenType, _lexeme,
                (_tokenType == ETokenTypes.Identifier ? string.Format(" ({0})", CategorizeIdentifier()) : ""));
        }

        bool IEquatable<Token>.Equals(Token other)
        {
            return Equals(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (Token)) return false;
            return Equals((Token) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_lexeme != null ? _lexeme.GetHashCode() : 0)*397) ^ _tokenType.GetHashCode();
            }
        }

        public static bool operator ==(Token left, Token right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Token left, Token right)
        {
            return !Equals(left, right);
        }

        public bool IsKeyword()
        {
            return _keyword != EKeywords.None;
        }

        public bool IsInputVariable()
        {
            return _variableType == EVariableTypes.InputVariable;
        }

        public bool IsWorkingVariable()
        {
            return _variableType == EVariableTypes.WorkingVariable;
        }
        
        public bool HasHigherPrecedence(Token rhs)
        {
            return OperatorPrecedence > rhs.OperatorPrecedence;
        }

        static private IDictionary<string, EKeywords> GetKeywords()
        {
            return new Dictionary<string, EKeywords>{
            {"IF", EKeywords.If},
            {"THEN", EKeywords.Then},
            {"ELSE", EKeywords.Else},
            {"WHILE", EKeywords.While},
            {"DO", EKeywords.Do},
            {"NOT", EKeywords.Not},
            {"AND", EKeywords.And},
            {"OR", EKeywords.Or},
            {"XOR", EKeywords.Xor},
            {"Noop", EKeywords.Noop}};
        }

        static private IDictionary<char, EVariableTypes> GetVariableTypes()
        {
            return new Dictionary<char, EVariableTypes>{
            {'X', EVariableTypes.InputVariable},
            {'Y', EVariableTypes.WorkingVariable}};
        }

        static private IDictionary<Symbols, int> GetOperatorPrecendences()
        {
            return new Dictionary<Symbols, int>{
            {Symbols.Chain, 0},
            {Symbols.Or, 1},
            {Symbols.Xor, 1},
            {Symbols.And, 2},
            {Symbols.Eq, 3},
            {Symbols.Neq, 3},
            {Symbols.Lt, 4},
            {Symbols.Lteq, 4},
            {Symbols.Gt, 4},
            {Symbols.Gteq, 4},
            {Symbols.Add, 5},
            {Symbols.Sub, 5},
            {Symbols.Mul, 6},
            {Symbols.Div, 6}};   
        }

        static private IDictionary<ETokenTypes, Symbols> GetSymbolTable()
        {
            return new Dictionary<ETokenTypes, Symbols>{
            {ETokenTypes.Chain, Symbols.Chain},
            {ETokenTypes.Equals, Symbols.Eq},
            {ETokenTypes.Neq, Symbols.Neq},
            {ETokenTypes.Lt, Symbols.Lt},
            {ETokenTypes.Lteq, Symbols.Lteq},
            {ETokenTypes.Gt, Symbols.Gt},
            {ETokenTypes.GtEq, Symbols.Gteq},
            {ETokenTypes.Plus, Symbols.Add},
            {ETokenTypes.Minus, Symbols.Sub},
            {ETokenTypes.Multiplication, Symbols.Mul},
            {ETokenTypes.Division, Symbols.Div}};
        }

        static private IDictionary<EKeywords, Symbols> GetBoolOpTable()
        {
            return new Dictionary<EKeywords, Symbols>{
            {EKeywords.And, Symbols.And},
            {EKeywords.Or, Symbols.Or},
            {EKeywords.Xor, Symbols.Xor}};
        }

        private readonly string _lexeme;
        private readonly ETokenTypes _tokenType;
        private static readonly IDictionary<string, EKeywords> Keywords;
        private static readonly IDictionary<char, EVariableTypes> VariableTypes;
        private static readonly IDictionary<Symbols, int> OperatorPrecendences;
        private static readonly IDictionary<ETokenTypes, Symbols> SymbolTable;
        private static readonly IDictionary<EKeywords, Symbols> BoolOpTable;
        private readonly EKeywords _keyword;
        private readonly EVariableTypes _variableType;
        private readonly EConstantTypes _constantType;
        private readonly int _variableNumber;
        private readonly int _operatorPrecedence;
        private readonly Symbols _symbol;

        public bool IsOperator()
        {
            return _operatorPrecedence >= 0;
        }
    }
}