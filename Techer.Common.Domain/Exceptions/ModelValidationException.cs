using System;
using System.Collections.Generic;

namespace Techer.Common.Domain.Exceptions
{
    public class ModelValidationException : Exception
    {
        public List<string> Failures { get; set; }

        public ModelValidationException(List<string> failures) : base("Ocorreram erros de validação")
        {
            Failures = failures;
        }
    }
}
