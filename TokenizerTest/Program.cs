using System;

namespace TokenizerTest
{
    class Program
    {
        static void Main()
        {
            var code =
            "((((0 = (0 - ((4 <= X0) / 9.5))) OR ((1.5 XOR 0) - (X0 AND 1.5))) XOR (((2 OR 7.1) / Y0)=>(Y0 / (Y1 = Y1)))) - ((((X0 + X0) = (Y1=>0)) XOR (Y0 = (3 >= X0))) > Y0))";
           
            var program = Parser.Parser.Parse(Tokenizer.Tokenizer.Tokenize(code));

            if (code == program.ToString())
            {
                Console.WriteLine("All the same!");
            }
            else
            {
                Console.WriteLine("1: {0}", code);
                Console.Write("2: {0}", program);
            }
        }
    }
}
