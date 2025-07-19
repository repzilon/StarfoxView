using System;
using System.Runtime.Serialization;

namespace Starfox.Editor
{
    [Serializable]
    public class SFCodeException : Exception
    {
        public SFCodeException() { }
        public SFCodeException(string message) : base(message) { }
        public SFCodeException(string message, Exception inner) : base(message, inner) { }
        protected SFCodeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class SFCodeOptimizerNotFoundException : SFCodeException
    {
        public SFCodeOptimizerNotFoundException(SFOptimizerTypeSpecifiers OptimizerType) : base(
            $"The {OptimizerType} optimizer was not found in the Code Project.")
        {
        }
    }
}
