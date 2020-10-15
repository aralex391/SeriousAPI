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
        public Dictionary<string, string> FilterDictionary { get; } 
        public FilterContainer(string[] filterArray)
        {
            FilterDictionary = new Dictionary<string, string>();
            foreach(string entry in filterArray)
            {
                AddEntry(entry);
            }
        }

        private void AddEntry(string entry)//fix handling of several values
        {
            string[] newEntry = entry.Split("^");
            FilterDictionary.Add(newEntry[0], newEntry[1]);
        }
    }
}
