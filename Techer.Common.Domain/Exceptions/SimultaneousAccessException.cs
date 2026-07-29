using System;

namespace Techer.Common.Domain.Exceptions
{
    public class SimultaneousAccessException : Exception
    {
        public SimultaneousAccessException() : base("Acesso simultâneo.")
        {

        }
    }
}
