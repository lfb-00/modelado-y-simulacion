using System.Globalization;

namespace dotnet_webapp.Services;

/// <summary>
/// Evaluador de expresiones matemáticas con soporte para funciones y operadores
/// </summary>
public class FunctionParser
{
    private readonly string _expression;
    private int _pos;
    private string _text = string.Empty;
    private double _xValue;

    public FunctionParser(string expression)
    {
        _expression = expression ?? string.Empty;
    }

    /// <summary>
    /// Evalúa la expresión para un valor dado de x
    /// </summary>
    public double Evaluate(double x)
    {
        _text = _expression.Replace(" ", string.Empty).ToLowerInvariant();
        _pos = 0;
        _xValue = x;
        double result = ParseExpression();
        SkipWhitespace();
        if (_pos < _text.Length)
        {
            throw new ArgumentException($"Símbolo inesperado en la expresión: '{_text[_pos]}'");
        }
        return result;
    }

    private double ParseExpression()
    {
        double value = ParseTerm();
        while (true)
        {
            if (Match('+'))
            {
                value += ParseTerm();
            }
            else if (Match('-'))
            {
                value -= ParseTerm();
            }
            else
            {
                break;
            }
        }
        return value;
    }

    private double ParseTerm()
    {
        double value = ParseFactor();
        while (true)
        {
            if (Match('*'))
            {
                value *= ParseFactor();
            }
            else if (Match('/'))
            {
                value /= ParseFactor();
            }
            else if (IsImplicitMultiplicationAhead())
            {
                value *= ParseFactor();
            }
            else
            {
                break;
            }
        }
        return value;
    }

    private bool IsImplicitMultiplicationAhead()
    {
        SkipWhitespace();
        if (_pos >= _text.Length)
        {
            return false;
        }

        char c = _text[_pos];
        return c == '(' || IsLetter(c) || char.IsDigit(c) || c == '.';
    }

    private double ParseFactor()
    {
        double baseValue = ParseUnary();

        // Exponenciación asociativa a derecha: 2^x^2 = 2^(x^2)
        if (Match('^'))
        {
            double exponent = ParseFactor();
            return Math.Pow(baseValue, exponent);
        }

        return baseValue;
    }

    private double ParseUnary()
    {
        if (Match('+'))
        {
            return ParseUnary();
        }
        if (Match('-'))
        {
            return -ParseUnary();
        }
        return ParsePrimary();
    }

    private double ParsePrimary()
    {
        SkipWhitespace();

        if (Match('('))
        {
            double value = ParseExpression();
            if (!Match(')'))
            {
                throw new ArgumentException("Falta un paréntesis de cierre.");
            }
            return value;
        }

        if (IsLetter(Peek()))
        {
            string name = ParseName();
            if (Match('('))
            {
                if (name == "pow")
                {
                    double first = ParseExpression();
                    if (!Match(','))
                    {
                        throw new ArgumentException("Falta la coma en pow(a,b).");
                    }
                    double second = ParseExpression();
                    if (!Match(')'))
                    {
                        throw new ArgumentException("Falta un paréntesis de cierre en pow.");
                    }
                    return Math.Pow(first, second);
                }

                double arg = ParseExpression();
                if (!Match(')'))
                {
                    throw new ArgumentException("Falta un paréntesis de cierre.");
                }
                return EvaluateFunction(name, arg);
            }

            if (name == "x")
            {
                return _xValue;
            }

            return EvaluateConstant(name);
        }

        return ParseNumber();
    }

    private double ParseNumber()
    {
        SkipWhitespace();
        int start = _pos;
        while (_pos < _text.Length && (char.IsDigit(_text[_pos]) || _text[_pos] == '.'))
        {
            _pos++;
        }
        if (start == _pos)
        {
            throw new ArgumentException("Número esperado.");
        }
        string number = _text[start.._pos];
        if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
        {
            throw new ArgumentException($"Número inválido: {number}");
        }
        return result;
    }

    private double EvaluateFunction(string name, double arg)
    {
        return name switch
        {
            "sin" => Math.Sin(arg),
            "cos" => Math.Cos(arg),
            "tan" => Math.Tan(arg),
            "exp" => Math.Exp(arg),
            "log" => Math.Log(arg),
            "ln" => Math.Log(arg),
            "sqrt" => Math.Sqrt(arg),
            "abs" => Math.Abs(arg),
            _ => throw new ArgumentException($"Función desconocida: {name}"),
        };
    }

    private double EvaluateConstant(string name)
    {
        return name switch
        {
            "pi" => Math.PI,
            "e" => Math.E,
            _ => throw new ArgumentException($"Constante desconocida: {name}"),
        };
    }

    private string ParseName()
    {
        int start = _pos;
        while (_pos < _text.Length && IsLetter(_text[_pos]))
        {
            _pos++;
        }
        return _text[start.._pos];
    }

    private bool Match(char expected)
    {
        SkipWhitespace();
        if (_pos < _text.Length && _text[_pos] == expected)
        {
            _pos++;
            return true;
        }
        return false;
    }

    private char Peek()
    {
        SkipWhitespace();
        return _pos < _text.Length ? _text[_pos] : '\0';
    }

    private void SkipWhitespace()
    {
        while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos]))
        {
            _pos++;
        }
    }

    private static bool IsLetter(char c) => char.IsLetter(c);
}
