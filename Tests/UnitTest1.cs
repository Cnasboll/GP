using System;
using System.Collections.Generic;
using gp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tokenizer;

namespace Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class UnitTest1
    {
        public UnitTest1()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestInfinities()
        {
            //
            // TODO: Add test logic	here
            //
            //We try all four arithmetic operations with 0.0 1.0, Nan and Positive infinity on each side.

            Assert.IsTrue(Double.IsNaN(0.0/0.0));
            Assert.AreEqual(0.0, 0.0 / 1.0);
            Assert.AreEqual(0.0, 0.0 / -1.0);
            Assert.IsTrue(Double.IsNaN(0.0 /Double.NaN));
            Assert.AreEqual(0.0, 0.0 / Double.PositiveInfinity);
            Assert.AreEqual(0.0, 0.0 / Double.NegativeInfinity);

            Assert.IsTrue(Double.IsPositiveInfinity(1.0 / 0.0));
            Assert.AreEqual(1.0, 1.0 / 1.0);
            Assert.AreEqual(-1.0, 1.0 / -1.0);
            Assert.IsTrue(Double.IsNaN(1.0 / Double.NaN));
            Assert.AreEqual(0.0, 1.0 / Double.PositiveInfinity);
            Assert.AreEqual(0.0, 1.0 / Double.NegativeInfinity);

            Assert.AreEqual(0.0, 10.0 / Double.PositiveInfinity);
            Assert.AreEqual(0.0, 10.0 / Double.NegativeInfinity);

            Assert.IsTrue(Double.IsNegativeInfinity(-1.0 / 0.0));
            Assert.AreEqual(-1.0, -1.0 / 1.0);
            Assert.AreEqual(-1.0, 1.0 / -1.0);
            Assert.IsTrue(Double.IsNaN(1.0 / Double.NaN));
            Assert.AreEqual(0.0, -1.0 / Double.PositiveInfinity);
            Assert.AreEqual(0.0, -1.0 / Double.NegativeInfinity);

            Assert.IsTrue(Double.IsNaN(Double.NaN / 0.0));
            Assert.IsTrue(Double.IsNaN(Double.NaN / 1.0));
            Assert.IsTrue(Double.IsNaN(Double.NaN / -1.0));
            Assert.IsTrue(Double.IsNaN(Double.NaN / Double.NaN));
            Assert.IsTrue(Double.IsNaN(Double.NaN / Double.PositiveInfinity));
            Assert.IsTrue(Double.IsNaN(Double.NaN / Double.NegativeInfinity));

            Assert.IsTrue(Double.IsPositiveInfinity(Double.PositiveInfinity / 0.0));
            Assert.IsTrue(Double.IsPositiveInfinity(Double.PositiveInfinity / 1.0));
            Assert.IsTrue(Double.IsNegativeInfinity(Double.PositiveInfinity / -1.0));
            Assert.IsTrue(Double.IsNaN(Double.PositiveInfinity / Double.NaN));
            Assert.IsTrue(Double.IsNaN(Double.PositiveInfinity / Double.PositiveInfinity));
            Assert.IsTrue(Double.IsNaN(Double.PositiveInfinity / Double.NegativeInfinity));


            Assert.IsTrue(Double.IsNegativeInfinity(Double.NegativeInfinity / 0.0));
            Assert.IsTrue(Double.IsNegativeInfinity(Double.NegativeInfinity / 1.0));
            Assert.IsTrue(Double.IsPositiveInfinity(Double.NegativeInfinity / -1.0));
            Assert.IsTrue(Double.IsNaN(Double.NegativeInfinity / Double.NaN));
            Assert.IsTrue(Double.IsNaN(Double.NegativeInfinity / Double.PositiveInfinity));
            Assert.IsTrue(Double.IsNaN(Double.NegativeInfinity / Double.NegativeInfinity));

            Assert.AreEqual(0.0, 0.0 * 0.0);
            Assert.AreEqual(0.0, 0.0 * 1.0);
            Assert.AreEqual(0.0, 0.0 * -1.0);
            Assert.IsTrue(Double.IsNaN(0.0 * Double.NaN));
            Assert.IsTrue(Double.IsNaN(0.0 * Double.PositiveInfinity));
            Assert.IsTrue(Double.IsNaN(0.0 * Double.NegativeInfinity));

            Assert.AreEqual(0.0, 1.0 * 0.0);
            Assert.AreEqual(1.0, 1.0 * 1.0);
            Assert.AreEqual(-1.0, 1.0 * -1.0);
            Assert.IsTrue(Double.IsNaN(1.0 * Double.NaN));
            Assert.IsTrue(Double.IsPositiveInfinity(1.0 * Double.PositiveInfinity));
            Assert.IsTrue(Double.IsNegativeInfinity(1.0 * Double.NegativeInfinity));

            Assert.AreEqual(0.0, -1.0 * 0.0);
            Assert.AreEqual(-1.0, -1.0 * 1.0);
            Assert.AreEqual(1.0, -1.0 * -1.0);
            Assert.IsTrue(Double.IsNaN(-1.0 * Double.NaN));
            Assert.IsTrue(Double.IsNegativeInfinity(-1.0 * Double.PositiveInfinity));
            Assert.IsTrue(Double.IsPositiveInfinity(-1.0 * Double.NegativeInfinity));

            Assert.IsTrue(Double.IsNaN(Double.NaN * 0.0));
            Assert.IsTrue(Double.IsNaN(Double.NaN * 1.0));
            Assert.IsTrue(Double.IsNaN(Double.NaN * -1.0));
            Assert.IsTrue(Double.IsNaN(Double.NaN * Double.NaN));
            Assert.IsTrue(Double.IsNaN(Double.NaN * Double.PositiveInfinity));
            Assert.IsTrue(Double.IsNaN(Double.NaN * Double.NegativeInfinity));


            Assert.IsTrue(Double.IsNaN(Double.PositiveInfinity * 0.0));
            Assert.IsTrue(Double.IsPositiveInfinity(Double.PositiveInfinity * 1.0));
            Assert.IsTrue(Double.IsNegativeInfinity(Double.PositiveInfinity * -1.0));
            Assert.IsTrue(Double.IsNaN(Double.PositiveInfinity * Double.NaN));
            Assert.IsTrue(Double.IsPositiveInfinity(Double.PositiveInfinity * Double.PositiveInfinity));
            Assert.IsTrue(Double.IsNegativeInfinity(Double.PositiveInfinity * Double.NegativeInfinity));

            Assert.IsTrue(Double.IsNaN(Double.NegativeInfinity * 0.0));
            Assert.IsTrue(Double.IsNegativeInfinity(Double.NegativeInfinity * 1.0));
            Assert.IsTrue(Double.IsPositiveInfinity(Double.NegativeInfinity * -1.0));
            Assert.IsTrue(Double.IsNaN(Double.NegativeInfinity * Double.NaN));
            Assert.IsTrue(Double.IsNegativeInfinity(Double.NegativeInfinity * Double.PositiveInfinity));
            Assert.IsTrue(Double.IsPositiveInfinity(Double.NegativeInfinity * Double.NegativeInfinity));

            Assert.AreEqual(0.0, 0.0 + 0.0);
            Assert.AreEqual(1.0, 0.0 + 1.0);
            Assert.IsTrue(Double.IsNaN(0.0 + Double.NaN));
            Assert.IsTrue(Double.IsPositiveInfinity(0.0 + Double.PositiveInfinity));
            Assert.IsTrue(Double.IsNegativeInfinity(0.0 + Double.NegativeInfinity));

            Assert.AreEqual(1.0, 1.0 + 0.0);
            Assert.AreEqual(2.0, 1.0 + 1.0);
            Assert.IsTrue(Double.IsNaN(1.0 + Double.NaN));
            Assert.IsTrue(Double.IsPositiveInfinity(1.0 + Double.PositiveInfinity));
            Assert.IsTrue(Double.IsNegativeInfinity(1.0 + Double.NegativeInfinity));

            Assert.IsTrue(Double.IsNaN(Double.NaN + 0.0));
            Assert.IsTrue(Double.IsNaN(Double.NaN + 1.0));
            Assert.IsTrue(Double.IsNaN(Double.NaN + Double.NaN));
            Assert.IsTrue(Double.IsNaN(Double.NaN + Double.PositiveInfinity));
            Assert.IsTrue(Double.IsNaN(Double.NaN + Double.NegativeInfinity));


            Assert.IsTrue(Double.IsPositiveInfinity(Double.PositiveInfinity + 0.0));
            Assert.IsTrue(Double.IsPositiveInfinity(Double.PositiveInfinity + 1.0));
            Assert.IsTrue(Double.IsNaN(Double.PositiveInfinity + Double.NaN));
            Assert.IsTrue(Double.IsPositiveInfinity(Double.PositiveInfinity + Double.PositiveInfinity));
            Assert.IsTrue(Double.IsNaN(Double.PositiveInfinity + Double.NegativeInfinity));

            Assert.IsTrue(Double.IsNegativeInfinity(Double.NegativeInfinity + 0.0));
            Assert.IsTrue(Double.IsNegativeInfinity(Double.NegativeInfinity + 1.0));
            Assert.IsTrue(Double.IsNaN(Double.NegativeInfinity + Double.NaN));
            Assert.IsTrue(Double.IsNaN(Double.NegativeInfinity + Double.PositiveInfinity));
            Assert.IsTrue(Double.IsNegativeInfinity(Double.NegativeInfinity + Double.NegativeInfinity));

            Assert.AreEqual(0.0, 0.0 - 0.0);
            Assert.AreEqual(-1.0, 0.0 - 1.0);
            Assert.IsTrue(Double.IsNaN(0.0 - Double.NaN));
            Assert.IsTrue(Double.IsNegativeInfinity(0.0 - Double.PositiveInfinity));
            Assert.IsTrue(Double.IsPositiveInfinity(0.0 - Double.NegativeInfinity));

            Assert.AreEqual(1.0, 1.0 - 0.0);
            Assert.AreEqual(0.0, 1.0 - 1.0);
            Assert.IsTrue(Double.IsNaN(1.0 - Double.NaN));
            Assert.IsTrue(Double.IsNegativeInfinity(1.0 - Double.PositiveInfinity));
            Assert.IsTrue(Double.IsPositiveInfinity(1.0 - Double.NegativeInfinity));

            Assert.IsTrue(Double.IsNaN(Double.NaN - 0.0));
            Assert.IsTrue(Double.IsNaN(Double.NaN - 1.0));
            Assert.IsTrue(Double.IsNaN(Double.NaN - Double.NaN));
            Assert.IsTrue(Double.IsNaN(Double.NaN - Double.PositiveInfinity));
            Assert.IsTrue(Double.IsNaN(Double.NaN - Double.NegativeInfinity));


            Assert.IsTrue(Double.IsPositiveInfinity(Double.PositiveInfinity - 0.0));
            Assert.IsTrue(Double.IsPositiveInfinity(Double.PositiveInfinity - 1.0));
            Assert.IsTrue(Double.IsNaN(Double.PositiveInfinity - Double.NaN));
            Assert.IsTrue(Double.IsNaN(Double.PositiveInfinity - Double.PositiveInfinity));
            Assert.IsTrue(Double.IsPositiveInfinity(Double.PositiveInfinity - Double.NegativeInfinity));

            Assert.IsTrue(Double.IsNegativeInfinity(Double.NegativeInfinity - 0.0));
            Assert.IsTrue(Double.IsNegativeInfinity(Double.NegativeInfinity - 1.0));
            Assert.IsTrue(Double.IsNaN(Double.NegativeInfinity - Double.NaN));
            Assert.IsTrue(Double.IsNegativeInfinity(Double.NegativeInfinity - Double.PositiveInfinity));
            Assert.IsTrue(Double.IsNaN(Double.NegativeInfinity - Double.NegativeInfinity));

            Assert.AreEqual(1000.0, 1000.0);
            Assert.IsFalse(1000.0 == Double.NaN);
            Assert.IsFalse(1000.0 == Double.PositiveInfinity);
            Assert.IsFalse(1000.0 == Double.NegativeInfinity);
            Assert.IsFalse(Double.NaN == 1000.0);
            Assert.IsFalse(Double.NaN == Double.NaN);
            Assert.IsFalse(Double.NaN == Double.PositiveInfinity);
            Assert.IsFalse(Double.NaN == Double.NegativeInfinity);
            Assert.IsFalse(Double.PositiveInfinity == 1000.0);
            Assert.IsFalse(Double.PositiveInfinity == Double.NaN);
            Assert.IsTrue(Double.PositiveInfinity == Double.PositiveInfinity);
            Assert.IsFalse(Double.PositiveInfinity == Double.NegativeInfinity);
            Assert.IsFalse(Double.NegativeInfinity == 1000.0);
            Assert.IsFalse(Double.NegativeInfinity == Double.NaN);
            Assert.IsFalse(Double.NegativeInfinity == Double.PositiveInfinity);
            Assert.IsTrue(Double.NegativeInfinity == Double.NegativeInfinity);
            Assert.IsTrue(1000.0 <= 1000.0);
            Assert.IsFalse(1000.0 <= Double.NaN);
            Assert.IsTrue(1000.0 <= Double.PositiveInfinity);
            Assert.IsFalse(1000.0 <= Double.NegativeInfinity);
            Assert.IsFalse(Double.NaN <= 1000.0);
            Assert.IsFalse(Double.NaN <= Double.NaN);
            Assert.IsFalse(Double.NaN <= Double.PositiveInfinity);
            Assert.IsFalse(Double.NaN <= Double.NegativeInfinity);
            Assert.IsFalse(Double.PositiveInfinity <= 1000.0);
            Assert.IsFalse(Double.PositiveInfinity <= Double.NaN);
            Assert.IsTrue(Double.PositiveInfinity <= Double.PositiveInfinity);
            Assert.IsFalse(Double.PositiveInfinity <= Double.NegativeInfinity);
            Assert.IsTrue(Double.NegativeInfinity <= 1000.0);
            Assert.IsFalse(Double.NegativeInfinity <= Double.NaN);
            Assert.IsTrue(Double.NegativeInfinity <= Double.PositiveInfinity);
            Assert.IsTrue(Double.NegativeInfinity <= Double.NegativeInfinity);
            Assert.IsTrue(1000.0 >= 1000.0);
            Assert.IsFalse(1000.0 >= Double.NaN);
            Assert.IsFalse(1000.0 >= Double.PositiveInfinity);
            Assert.IsTrue(1000.0 >= Double.NegativeInfinity);
            Assert.IsFalse(Double.NaN >= 1000.0);
            Assert.IsFalse(Double.NaN >= Double.NaN);
            Assert.IsFalse(Double.NaN >= Double.PositiveInfinity);
            Assert.IsFalse(Double.NaN >= Double.NegativeInfinity);
            Assert.IsTrue(Double.PositiveInfinity >= 1000.0);
            Assert.IsFalse(Double.PositiveInfinity >= Double.NaN);
            Assert.IsTrue(Double.PositiveInfinity >= Double.PositiveInfinity);
            Assert.IsTrue(Double.PositiveInfinity >= Double.NegativeInfinity);
            Assert.IsFalse(Double.NegativeInfinity >= 1000.0);
            Assert.IsFalse(Double.NegativeInfinity >= Double.NaN);
            Assert.IsFalse(Double.NegativeInfinity >= Double.PositiveInfinity);
            Assert.IsTrue(Double.NegativeInfinity >= Double.NegativeInfinity);
            Assert.IsFalse(1000.0 < 1000.0);
            Assert.IsFalse(1000.0 < Double.NaN);
            Assert.IsTrue(1000.0 < Double.PositiveInfinity);
            Assert.IsFalse(1000.0 < Double.NegativeInfinity);
            Assert.IsFalse(Double.NaN < 1000.0);
            Assert.IsFalse(Double.NaN < Double.NaN);
            Assert.IsFalse(Double.NaN < Double.PositiveInfinity);
            Assert.IsFalse(Double.NaN < Double.NegativeInfinity);
            Assert.IsFalse(Double.PositiveInfinity < 1000.0);
            Assert.IsFalse(Double.PositiveInfinity < Double.NaN);
            Assert.IsFalse(Double.PositiveInfinity < Double.PositiveInfinity);
            Assert.IsFalse(Double.PositiveInfinity < Double.NegativeInfinity);
            Assert.IsTrue(Double.NegativeInfinity < 1000.0);
            Assert.IsFalse(Double.NegativeInfinity < Double.NaN);
            Assert.IsTrue(Double.NegativeInfinity < Double.PositiveInfinity);
            Assert.IsFalse(Double.NegativeInfinity < Double.NegativeInfinity);
            Assert.IsFalse(1000.0 > 1000.0);
            Assert.IsFalse(1000.0 > Double.NaN);
            Assert.IsFalse(1000.0 > Double.PositiveInfinity);
            Assert.IsTrue(1000.0 > Double.NegativeInfinity);
            Assert.IsFalse(Double.NaN > 1000.0);
            Assert.IsFalse(Double.NaN > Double.NaN);
            Assert.IsFalse(Double.NaN > Double.PositiveInfinity);
            Assert.IsFalse(Double.NaN > Double.NegativeInfinity);
            Assert.IsTrue(Double.PositiveInfinity > 1000.0);
            Assert.IsFalse(Double.PositiveInfinity > Double.NaN);
            Assert.IsFalse(Double.PositiveInfinity > Double.PositiveInfinity);
            Assert.IsTrue(Double.PositiveInfinity > Double.NegativeInfinity);
            Assert.IsFalse(Double.NegativeInfinity > 1000.0);
            Assert.IsFalse(Double.NegativeInfinity > Double.NaN);
            Assert.IsFalse(Double.NegativeInfinity > Double.PositiveInfinity);
            Assert.IsFalse(Double.NegativeInfinity > Double.NegativeInfinity);
            Assert.IsFalse(1000.0 != 1000.0);
            Assert.IsTrue(1000.0 != Double.NaN);
            Assert.IsTrue(1000.0 != Double.PositiveInfinity);
            Assert.IsTrue(1000.0 != Double.NegativeInfinity);
            Assert.IsTrue(Double.NaN != 1000.0);
            Assert.IsTrue(Double.NaN != Double.NaN);
            Assert.IsTrue(Double.NaN != Double.PositiveInfinity);
            Assert.IsTrue(Double.NaN != Double.NegativeInfinity);
            Assert.IsTrue(Double.PositiveInfinity != 1000.0);
            Assert.IsTrue(Double.PositiveInfinity != Double.NaN);
            Assert.IsFalse(Double.PositiveInfinity != Double.PositiveInfinity);
            Assert.IsTrue(Double.PositiveInfinity != Double.NegativeInfinity);
            Assert.IsTrue(Double.NegativeInfinity != 1000.0);
            Assert.IsTrue(Double.NegativeInfinity != Double.NaN);
            Assert.IsTrue(Double.NegativeInfinity != Double.PositiveInfinity);
            Assert.IsFalse(Double.NegativeInfinity != Double.NegativeInfinity);

        }

        [TestMethod]
        public void TestTokenizer()
        {
            var code =
                "((((0 = (0 - ((4 <= X0) / 9.5))) OR ((1.5 XOR 0) - (X0 AND 1.5))) XOR (((2 OR 7.1) / Y0)=>(Y0 / (Y1 = Y1)))) - ((((X0 + X0) = (Y1=>0)) XOR (Y0 = (3 >= X0))) > Y0))";
            IEnumerable<Token> tokens = Tokenizer.Tokenizer.Tokenize(code);
            foreach (var token in tokens)
            {
                token.ToString();
            }
        }

        [TestMethod]
        public void SimplyfyingTest()
        {
            var targets = @"1
1 3
2 6
3 11
4 18
5 ? ";

            Problem problem = Problem.Parse(targets);

            var code =
                "((X0 * (((0 * 7.3) * (((((8.2 / (((9 + ((6 / 6.8) / X0)) / (X0 + (6.8 - (X0 / 9)))) + (0.9 + (X0 * X0)))) - ((((9 + 7) / X0) / 7) / X0)) + 3.5) / (6.3 / (X0 * (3.5 + X0)))) / (8 / 6.3))) + X0)) + 2)";


            var program = Parser.Parser.Parse(Tokenizer.Tokenizer.Tokenize(code));

            FitnessEvaluation fitnessEvaluation = new FitnessEvaluation(program, problem);

            while (!fitnessEvaluation.Tick(false))
            {
                
            }

            /*Simplification rendered another program: Fitness of simplified program is 0
Here is a trace of the simplification for debugging:
((X0 * (((0 * 7.3) * (((((8.2 / (((9 + ((6 / 6.8) / X0)) / (X0 + (6.8 - (X0 / 9)))) + (0.9 + (X0 * X0)))) - ((((9 + 7) / X0) / 7) / X0)) + 3.5) / (6.3 / (X0 * (3.5 + X0)))) / (8 / 6.3))) + X0)) + 2)
Replacing expression with only constant arguments with constant
After removing redundant code:
((X0 * ((0 * (((((8.2 / (((9 + ((6 / 6.8) / X0)) / (X0 + (6.8 - (X0 / 9)))) + (0.9 + (X0 * X0)))) - ((((9 + 7) / X0) / 7) / X0)) + 3.5) / (6.3 / (X0 * (3.5 + X0)))) / (8 / 6.3))) + X0)) + 2)
[5] = 27 before simplification = 0, expected result = ?*/


            var simplifiedCode =
                "((X0 * ((0 * (((((8.2 / (((9 + ((6 / 6.8) / X0)) / (X0 + (6.8 - (X0 / 9)))) + (0.9 + (X0 * X0)))) - ((((9 + 7) / X0) / 7) / X0)) + 3.5) / (6.3 / (X0 * (3.5 + X0)))) / (8 / 6.3))) + X0)) + 2)";

            var simplifiedprogram = Parser.Parser.Parse(Tokenizer.Tokenizer.Tokenize(simplifiedCode));

            Assert.IsFalse(program.AnalyzeTestCasedifferences(fitnessEvaluation, simplifiedprogram));
        }

        [TestMethod]
        public void SimplifyingTest2()
        {
            var targets = @"1
1 3
2 6
3 11
4 18
5 ? ";

            Problem problem = Problem.Parse(targets);

            var code = "(X0 / (4 * ((9.66 - X0) / (26 - (4.2 * X0)))))";

            var program = Parser.Parser.Parse(Tokenizer.Tokenizer.Tokenize(code));

            var fitness = new FitnessEvaluation(program, problem);
            while (!fitness.Tick())
            {

            }

            var simplifiedCode = program.Simplify(fitness).ToString();

            Assert.AreEqual(code, simplifiedCode);

        }

        [TestMethod]
        public void SimplifyingTest3()
        {
            var targets = @"1
1 3
2 6
3 11
4 18
5 ? ";

            Problem problem = Problem.Parse(targets);

            string code = "(((X0 + ((2 * X0) - X0)) / 0.9) - (X0 / (((0.9 / 5.3) + 5.3) / ((0.7 / 3) * 3))))";

            /*(((X0 + ((2 * X0) - X0)) / 0.9) - (X0 / (((0.9 / 5.3) + 5.3) / ((0.7 / 3) * 3))))
Replacing (n*exp)-+exp with (n+-1)*exp
After removing redundant code:
(((X0 + ((1 - 2) * X0)) / 0.9) - (X0 / (((0.9 / 5.3) + 5.3) / ((0.7 / 3) * 3))));*/

            var program = Parser.Parser.Parse(Tokenizer.Tokenizer.Tokenize(code));

            var fitness = new FitnessEvaluation(program, problem);
            while (!fitness.Tick())
            {

            }

            var simplifiedCode = program.Simplify(fitness).ToString();
            Assert.AreEqual("(((2 * X0) / 0.9) - (X0 / 7.81401617250674))", simplifiedCode);
            //TODO: If finding another rule:
            //Assert.AreEqual("((X0 / 0.45) - (X0 / 7.81401617250674)")
            //(-5 + (8 * (IF X0 THEN X0))) shall be simplified as (-5 + (8 * X0)) as (IF exp THEN exp) => exp!


/*Simplification rendered another program: Fitness of simplified program is 19,7937582732972
Here is a trace of the simplification for debugging:
(NOT ((6 OR (3 >= 3)) * (((6.9 / X0) >= X0) <= ((6 = 6.9) / (X0 * X0)))))
Replacing expression with only constant arguments with constant
After removing redundant code:
(NOT ((6 OR 1) * (((6.9 / X0) >= X0) <= ((6 = 6.9) / (X0 * X0)))))
Replacing expression with only constant arguments with constant
After removing redundant code:
(NOT (1 * (((6.9 / X0) >= X0) <= ((6 = 6.9) / (X0 * X0)))))
Replacing expression with only constant arguments with constant
After removing redundant code:
(NOT (1 * (((6.9 / X0) >= X0) <= (0 / (X0 * X0)))))
Replacing 1*exp with exp
After removing redundant code:
(NOT (((6.9 / X0) >= X0) <= (0 / (X0 * X0))))
Replacing NOT(exp1 RELOP exp2) with exp1 Inverse_relop exp2
After removing redundant code:
(((6.9 / X0) >= X0) > (0 / (X0 * X0)))
[0] = 0 but before simplification = 1, expected result = 0
Still waiting for a better program...*/
}
}
}
