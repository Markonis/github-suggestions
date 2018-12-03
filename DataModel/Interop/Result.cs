using Domain.Errors;
using System.Collections.Generic;

namespace Domain.Interop
{
    public class Result<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public IList<Error> Errors;
    }

    public class Result
    {
        public bool Success { get; set; }
        public IList<Error> Errors;
    }
}
