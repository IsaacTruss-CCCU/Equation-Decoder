namespace Decoder.Internal
{
    // Represents each possible operator
    internal enum MathOperators
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Pow,
        Mod,
        LeftShift,
        RightShift,
        Error // Used to return an error should it arise
    }

    // Calculates the result of a calculation
    internal class calculate : IDisposable
    {
        private verify verify = new verify();
        private bracketHandler bracketHandler = new bracketHandler();

        private bool disposed = false;
        private object Lock = new object();

        public double getResult(MathOperators operation, double num1, double num2)
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

        public (List<MathOperators>, List<double>) splitEquation(string equation)
        {
            List<MathOperators> operators = new List<MathOperators>();
            List<double> numbers = new List<double>();

            if (equation.Contains("("))
            {
                equation = bracketHandler.handle(equation); // Handles the brackets
            }

            string[] stringSplitEquation = equation.Split(" ");

            for (int i = 0; i < stringSplitEquation.Length; i++)
            {
                // Error is represented as a 0 in the enum so if 0 gets parsed it will return an error
                if ((verify.verifyOpertor(stringSplitEquation[i])) && (stringSplitEquation[i] != "0"))
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
                        operators.Add(identifiedOperator); // Adds the operator to the oeprators list
                    }
                }

                else
                {
                    numbers.Add(Convert.ToDouble(stringSplitEquation[i])); // Adds the number to the numbers list
                }
            }

            return (operators, numbers); // Returns both lists
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
                bracketHandler?.Dispose();
            }

            disposed = true;
        }

        ~calculate()
        {
            Dispose(false);
        }
    }

    internal class verify : IDisposable
    {

        private bool disposed = false;
        private object Lock = new object();

        // Dictionary to be used to store the enum operator and the string operator equivelent
        private Dictionary<MathOperators, string> possibleOperators = new Dictionary<MathOperators, string>
        {
            { MathOperators.Add, "+" },
            { MathOperators.Subtract, "-" },
            { MathOperators.Multiply, "*" },
            { MathOperators.Divide, "/" },
            { MathOperators.Pow, "^" },
            { MathOperators.Mod, "%" },
            { MathOperators.LeftShift, "<<" },
            { MathOperators.RightShift, ">>" },
            { MathOperators.Error, "0" }
        };

        public bool verifyOpertor(string chosenOperator)
        {
            return possibleOperators.ContainsValue(chosenOperator); // Verifies if the string is an opperator
        }

        public MathOperators identifyOperator(string chosenOperator)
        {
            MathOperators operatorToReturn = MathOperators.Error; // Ensures an error is returned if none are correct

            Parallel.ForEach(possibleOperators, possibleOperator =>
            {
                if (string.Compare(possibleOperator.Value, chosenOperator, true) == 0)
                {
                    operatorToReturn = possibleOperator.Key; // Selects the operator should it be valid
                }
            });

            return operatorToReturn;
        }

        public bool verifyBrackets(char[] charSplitEquation)
        {
            int openCounter = 0;
            int closedCounter = 0;

            Parallel.For(0, charSplitEquation.Length, // Processes each option in the for loop in parrallel to increase the speed, especially on bigger problems
                i => {
                    if (charSplitEquation[i] == '(')
                    {
                        openCounter++;
                    }
                    else if (charSplitEquation[i] == ')')
                    {
                        closedCounter++;
                    }
            });

            if (closedCounter == openCounter)
            {
                return true;
            }

            return false;
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
                disposed = true;
            }
        }

        ~verify()
        {
            Dispose(false);
        }
    }
}