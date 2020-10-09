using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http;

namespace Codestellation.AspNetCore.Logging
{
    public readonly struct PredicateContainer
    {
        public readonly Predicate<HttpContext> Predicate;

        public PredicateContainer(Predicate<HttpContext> predicate)
        {
            Predicate = predicate;
        }
    }
}