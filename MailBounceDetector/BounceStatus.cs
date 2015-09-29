// developed by Rui Lopes (ruilopes.com). licensed under MIT.

namespace MailBounceDetector
{
    public sealed class BounceStatus
    {
        private int _code;
        private string _message;

        public BounceStatus(int code, string message)
        {
            _code = code;
            _message = message;
        }

        public int Code { get { return _code; } }
        public string Message { get { return _message; } }

        public override string ToString()
        {
            return $"{_code} {_message}";
        }
    }
}
