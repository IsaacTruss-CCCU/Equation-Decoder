using System.Text;

namespace Decoder.Internal
{
    internal class bracketHandler : IDisposable
    {

        private object Lock = new object();
        private bool disposed = false;

        private verify verify = new verify();

        public string handle(string equation)
        {
            bool bracketFlag = true;
            string equationToUse = equation;

            // While brackets are in the equation it splits it up solves the sub equation and then build the new equation and check if brackets are still present
            while (bracketFlag)
            {
                char[] charSplitArray = equationToUse.ToCharArray();
                bool bracketCountCheck = verify.verifyBrackets(charSplitArray);
                if (bracketCountCheck)
                {
                    int openBracketPos = findOpenBracketPosition(charSplitArray);
                    int closedBracketPos = findClosedBracketPosition(charSplitArray, openBracketPos);

                    // Necessary for the span
                    int subEquationStart = openBracketPos + 1;
                    int lengthOfSpan = closedBracketPos - subEquationStart;

                    if (openBracketPos == (closedBracketPos + 1))
                    {
                        throw new ArgumentException("Can't have empty brackets.");
                    }

                    ReadOnlySpan<char> charArraySpan = charSplitArray.AsSpan(subEquationStart, lengthOfSpan);

                    string subEquation = getSubEquation(charArraySpan);
                    string subEquationResult = getSubEquatonResult(subEquation);

                    equationToUse = getNewEquation(charSplitArray, openBracketPos, closedBracketPos, subEquationResult);

                    bracketFlag = equationToUse.Contains("(");
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Incorrect use of brackets.");
                }
            }

            return equationToUse;
        }

        private int findOpenBracketPosition(char[] charArray) // Gets the position of the most nested open bracket
        {
            int pos = 0;

            for (int i = 0; i < charArray.Count(); i++)
            {
                if (charArray[i] == '(')
                {
                    pos = i;
                }
            }

            return pos;
        }

        // Gets the position of the appropriate closed bracket
        private int findClosedBracketPosition(char[] charArray, int openBracketPos)
        {
            int pos = 0;
            bool found = false;

            // Used to cancel the parrallel for if the processor count is reached or if manually cancelled
            CancellationTokenSource cts = new();
            ParallelOptions options = new()
            {
                CancellationToken = cts.Token,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            try
            {
                Parallel.For(0, charArray.Count(), options, (i, state) =>
                {
                    if ((charArray[i] == ')') && (i > openBracketPos) && (found == false))
                    {
                        pos = Convert.ToInt32(i);
                        found = true;
                        cts.Cancel();
                        state.Stop();
                    }
                });
            }
            catch (OperationCanceledException)
            {
                if (!found)
                {
                    for (int i = 0; i < charArray.Count(); i++)
                    {
                        if ((charArray[i] == ')') && (i > openBracketPos) && (found == false))
                        {
                            pos = Convert.ToInt32(i);
                            found = true;
                        }
                    }
                }
            }

            return pos;
        }

        // Takes the math problem from between the brackets and outputs a string with the "sub equation"
        private string getSubEquation(ReadOnlySpan<char> charSlice)
        {
            var sb = new StringBuilder(charSlice.Length); // Initializes the String Builder array with number of spaces it will need to help reduce overhead

            for (int i = 0; i < charSlice.Length; i++)
            {
                sb.Append(charSlice[i]);
            }

            string subEquation = sb.ToString();

            return subEquation;
        }

        // Gets the result of the sub equation and returns the result
        private string getSubEquatonResult(string equation)
        {
            var (operators, numbers) = splitEquation(equation);

            int numberCounter = 2;
            bool firstRun = true;

            double result = 0;
            double num1 = 0;
            double num2 = 0;

            for (int i = 0; i < operators.Count; i++)
            {
                if (firstRun)
                {
                    num1 = numbers[0];
                    num2 = numbers[1];
                    result = getResult(operators[i], num1, num2);
                    firstRun = false;
                }
                else
                {
                    num1 = result;
                    num2 = numbers[numberCounter];
                    numberCounter++;
                    result = getResult(operators[i], num1, num2);
                }
            }

            return Convert.ToString(result);
        }

        // Inserts the result of the sube equation in place of the brackets
        private string getNewEquation(char[] charArray, int openBracketPos, int closedBracketPos, string result)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < charArray.Length; i++)
            {
                if ((i >= openBracketPos) && (i < closedBracketPos))
                {

                }

                else if (i == closedBracketPos)
                {
                    sb.Append(result);
                }

                else
                {
                    sb.Append(charArray[i]);
                }

            }

            string newEquation = sb.ToString();

            return newEquation;
        }

        // Splits the equation to two lists operators and numbers
        private (List<MathOperators>, List<double>) splitEquation(string equation)
        {
            List<MathOperators> operators = new List<MathOperators>();
            List<double> numbers = new List<double>();

            string[] stringSplitEquation = equation.Split(" ");

            for (int i = 0; i < stringSplitEquation.Length;i++)
            {
                if (verify.verifyOpertor(stringSplitEquation[i]))
                {
                    MathOperators identifiedOperator = verify.identifyOperator(stringSplitEquation[i]);
                    if ((identifiedOperator == MathOperators.Error) && (string.Compare(stringSplitEquation[i], "0", true) != 0))
                    {
                        throw new FormatException("Invalid operator");
                    }
                    else if (string.Compare(stringSplitEquation[i], "0", true) == 0)
                    {
                        numbers.Add(Convert.ToDouble(stringSplitEquation[i]));
                    }
                    else
                    {
                        operators.Add(identifiedOperator);
                    }
                }

                else
                {
                    numbers.Add(Convert.ToDouble(stringSplitEquation[i]));
                }
            }

            return (operators, numbers);
        }

        private double getResult(MathOperators operation, double num1, double num2)
        {
            // Uses the operator selected by the user to select the appropriate calculation
            return operation switch
            {
                MathOperators.Add => num1 + num2,
                MathOperators.Subtract => num1 - num2,
                MathOperators.Multiply => num1 * num2,
                MathOperators.Divide => num2 != 0 // Stops a divide by zero error
                    ? num1 / num2
                    : throw new DivideByZeroException("Cannot divide by zero."), // Throws an error if num2 is zero
                MathOperators.Pow => Math.Pow(num1, num2),
                MathOperators.Mod => num1 % num2,
                MathOperators.LeftShift => num2 != 0 
                ? binaryShift(operation, num1, num2)
                : throw new InvalidOperationException("Cannot bitwise with 0"),
                MathOperators.RightShift => num2 != 0
                ? binaryShift(operation, num1, num2)
                : throw new InvalidOperationException("Cannot bitwise with 0"),
                _ => throw new FormatException("Invalid operator") // Defualt response if none of the operators are selected
            };
        }

        // I used this method of calculating the shift as i am not constrained by the need to use and integer with >> or << operators
        private double binaryShift(MathOperators operation, double num1, double num2)
        {
            double result = 0;

            if (operation == MathOperators.LeftShift)
            {
                result = num1 * Math.Pow(2, num2);
            }
            else if (operation == MathOperators.RightShift)
            {
                result = num1 / Math.Pow(2, num2);
            }

            return result;
        }

        public void Dispose()
        {
            lock (Lock)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if ((!disposed) && disposing)
            {
                verify?.Dispose();

                disposed = true;
            }
        }

        ~bracketHandler()
        {
            Dispose(false);
        }
    }
}