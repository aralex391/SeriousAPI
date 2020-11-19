using Org.BouncyCastle.Asn1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SeriousAPI.Models
{
    public class FilterContainer
    {
        public Lookup<string, string> FilterLookup { get; } 
        public FilterContainer(string[] filterArray)
        {
            FilterLookup = (Lookup<string, string>)filterArray.ToLookup(
                key => key.Split("^")[0], value => value.Split("^")[1]
                );
        }
    }
}
