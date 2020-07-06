namespace Common
{
    public static class Codec
    {
        public static int EncodeSymbol(int varnumber, Symbols symbol)
        {
            return EncodeSymbol(varnumber, symbol, null);
        }

        public static int EncodeSymbol(int varnumber, Symbols symbol, int? qualifier)
        {
            switch (symbol)
            {
                case Symbols.InputArgument:
                    return (int)Symbols.InputArgument + (qualifier ?? 0);
                case Symbols.WorkingVariable:
                    return (int)Symbols.InputArgument + varnumber + (4 * (qualifier ?? 0));
                case Symbols.AssignWorkingVariable:
                    return (int)Symbols.InputArgument + varnumber + (4 * (qualifier ?? 0)) + 1;
                case Symbols.DoubleLiteral:
                    return (int)Symbols.InputArgument + varnumber + (4 * (qualifier ?? 0)) + 2;
                case Symbols.IntegerLiteral:
                    return (int)Symbols.InputArgument + varnumber + (4 * (qualifier ?? 0)) + 3;
                default:
                    return (int)symbol;
            }
        }
    }
}
