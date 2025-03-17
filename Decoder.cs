using Decoder.Internal;

namespace Decoder.External
{
    public class equationDecoder : IDisposable
    {

        // Used for the Dispose methods
        private bool disposed = false;
        private object Lock = new object();

        private verify verify = new verify();
        private calculate calculate = new calculate();
        private bracketHandler bracketHandler = new bracketHandler();

        public double calculateResult(string originalEquation)
        {
            var (operators, numbers) = calculate.splitEquation(originalEquation); // Generates two lists

            int opCount = 2 + ((operators.Count - 1) * 2);

            // Ensures there is enough numebrs and operators to complete an equation
            if (opCount != numbers.Count)
            {
                throw new ArgumentException("Not enough numbers or operators");
            }

            int numberCounter = 2;
            bool firstRun = true;

            double result = 0;
            double num1 = 0;
            double num2 = 0;

            for (int i = 0; i < operators.Count; i++)
            {
                if (firstRun) // Since num1 and num2 will be 0 at the start
                {
                    num1 = numbers[0];
                    num2 = numbers[1];
                    result = calculate.getResult(operators[i], num1, num2);
                    firstRun = false;
                }
                else // Since result will be used as num1 as it will have been calculated
                {
                    num1 = result;
                    num2 = numbers[numberCounter];
                    numberCounter++;
                    result = calculate.getResult(operators[i], num1, num2);
                }
            }

            return result;

        }

        /*
         * Frees unmanaged resources and indicates the finalizer will not need to be run
         */
        public void Dispose()
        {
            // Synchronizes the disposal methods should threads be used in the program
            lock (Lock)
            {
                // Call's the cleaner 
                Dispose(true);

                // Indicates the finalizer is not needed
                GC.SuppressFinalize(this);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            // Only disposes if it is called to do so and has not already been done so
            if ((!disposed) && (disposing))
            {
                // Calls the dispose methods of the managed resources
                verify?.Dispose();
                calculate?.Dispose();
                bracketHandler?.Dispose();

                // Indicates that it has been disposed
                disposed = true;
            }

        }

        // The finalizer cleans up unmanaged resources
        // There are none but it is good practice to implement incase some are added in future
        ~equationDecoder()
        {
            Dispose(false);
        }
    }
}