using System;
using System.Text.RegularExpressions;

namespace Lazy.Editor
{
    public class TextValidator
    {
        public enum ErrorType
        {
            Invalid = -1,
            Info = 0,
            Warning = 1,
            Error = 2
        }

        [NonSerialized] public ErrorType errorType = ErrorType.Invalid;

        [NonSerialized] private string _regEx = string.Empty;

        [NonSerialized] private Func<string, bool> _validationFunc;

        [NonSerialized] public string failureMsg = string.Empty;

        public TextValidator(ErrorType errorType, string failureMsg, string regEx)
        {
            this.errorType = errorType;
            this.failureMsg = failureMsg;
            _regEx = regEx;
        }

        public TextValidator(ErrorType errorType, string failureMsg, Func<string, bool> validationFunction)
        {
            this.errorType = errorType;
            this.failureMsg = failureMsg;
            _validationFunc = validationFunction;
        }

        public bool Validate(string srcString)
        {
            if (_regEx != string.Empty)
            {
                return Regex.IsMatch(srcString, _regEx);
            }

            return _validationFunc != null && _validationFunc(srcString);
        }
    }
}