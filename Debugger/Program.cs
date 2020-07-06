using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using gp;

namespace Debugger
{
    class Program
    {
        static void Main(string[] args)
        {
           // var originalCode = "((((0 = (0 - ((4 <= X0) / 9.5))) OR ((1.5 XOR 0) - (X0 AND 1.5))) XOR (((2 OR 7.1) / Y0)=>(Y0 / (Y1 = Y1)))) - ((((X0 + X0) = (Y1=>0)) XOR (Y0 = (3 >= X0))) > Y0))";
            //var simplifiedCode = "((((NOT (0 - ((4 <= X0) / 9.5))) OR ((1.5 XOR 0) - (X0 AND 1.5))) XOR (((2 OR 7.1) / Y0)=>(Y0 / (Y1 = Y1)))) - ((((X0 + X0) = (Y1=>0)) XOR (Y0 = (3 >= X0))) > Y0))";

            var fin = new StreamReader(args[0]);
            string code = fin.ReadToEnd();
            double argument = double.Parse(args[1]);

            CallTree.PrintDebugInfo = true;


            Console.WriteLine("Running {0}", code);
            RunProgram(code, argument);
        }

        static void RunProgram(string code, double input)
        {
            var program = Parser.Parser.Parse(Tokenizer.Tokenizer.Tokenize(code));

            var runTimeState = new RuntimeState(1);
            runTimeState.Inputs[0] = input;
            int pc = 0;
            var callTree = new CallTree(program, ref pc);

            while (!callTree.Tick(runTimeState, program.Constants))
            {

            }
        }
    }
}

