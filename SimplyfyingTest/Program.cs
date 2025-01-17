﻿using System;
using System.Text;
using gp;

namespace SimplyfyingTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var problem = Problem.Read("Fitness\\sin-data.txt");
            
            var code =
            "((((0 = (0 - ((4 <= X0) / 9.5))) OR ((1.5 XOR 0) - (X0 AND 1.5))) XOR (((2 OR 7.1) / Y0)=>(Y0 / (Y1 = Y1)))) - ((((X0 + X0) = (Y1=>0)) XOR (Y0 = (3 >= X0))) > Y0))";

            var program = Parser.Parser.Parse(Tokenizer.Tokenizer.Tokenize(code));

            var fitness = new FitnessEvaluation(program, problem);
            var stringBuilder = new StringBuilder();
            fitness.Evaluate(stringBuilder);

            FitnessEvaluation simplifiedFitness;
            do
            {
                gp.Program simplifiedProgram = program.Simplify(fitness, stringBuilder);
                if (simplifiedProgram == null || simplifiedProgram.ToString() == program.ToString())
                {
                    break;
                }
                simplifiedFitness = new FitnessEvaluation(simplifiedProgram, problem);
                simplifiedFitness.Evaluate(false, stringBuilder);
                program = simplifiedProgram;
            } while (fitness.ErrorSum == simplifiedFitness.ErrorSum);

            Console.Out.Write(stringBuilder);
        }
    }
}
