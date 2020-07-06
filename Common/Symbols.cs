namespace Common
{
    public enum Symbols
    {
        //Operators:
        Noop,
        Not,
        Add,
        Sub,
        Mul,
        Div,
        Lt,
        Lteq,
        Gt,
        Gteq,
        Eq,
        Neq,
        And,
        Or,
        Xor,
        Chain,
        If,
        While,
        // Mov,
        Ifelse,
        //Terminal symbols that need to be encoded with qualifiers:
        InputArgument,
        IntegerLiteral,
        DoubleLiteral,
        WorkingVariable,
        AssignWorkingVariable
    };
}