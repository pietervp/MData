using System;
using System.Collections.Generic;
using System.Linq;

namespace MData.Core.Base
{
    public class FoundMultipleLogicImplementationsException : Exception
    {
        public Type DomainType { get; set; }
        public IEnumerable<Type> Candidates { get; set; }
        
        public FoundMultipleLogicImplementationsException(Type domainType, IEnumerable<Type> candidates)
        {
            DomainType = domainType;
            Candidates = candidates;
        }

        public override string Message
        {
            get
            {
                return string.Format("Found multiple logical implementations for {0}: '{1}'", DomainType.Name, Candidates.Select(x => MDataExtensions.GetName(x)).Aggregate((x, y) => x + ", " + y));
            }
        }
    }
}