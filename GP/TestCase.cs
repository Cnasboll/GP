using System;

namespace GP
{
    public class TestCase
    {

        #region Public

        public TestCase(Input input, Output expectedOutput)
        {
            this.input = input;
            this.expectedOutput = expectedOutput;
        }

        public double AnalyzeDifference(Output actualOutput)
        {
            Console.WriteLine("For testcase: {0}", input);
            bool bAreEqual = true;
            if (actualOutput.Result != expectedOutput.Result)
            {
                Console.WriteLine("Expected Result = {0}, Actual = {1}, Diff = {2}", expectedOutput.Result, actualOutput.Result, Math.Abs(actualOutput.Result-expectedOutput.Result));
                bAreEqual = false;
            }
            for (int i = 0; i < expectedOutput.Length; ++i)
            {
                if (actualOutput[i] != expectedOutput[i])
                {
                    Console.WriteLine("Expected Y{0} = {1}, Actual = {2}, Diff = {3}", i, expectedOutput[i], actualOutput[i], Math.Abs(actualOutput[i]-actualOutput[i]));
                    bAreEqual = false;
                }
            }

            if (bAreEqual)
            {
                Console.WriteLine("No difference found!");
                return 0;
            }
            double dEcludianDistance = expectedOutput.Distance(actualOutput);
            Console.WriteLine("Ecludian distance: {0}", dEcludianDistance);
            return dEcludianDistance;
        }

        #endregion

        #region Private

        private readonly Input input;
        private readonly Output expectedOutput;

        #endregion
    }
}
