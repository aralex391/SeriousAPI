using System;
using System.Collections.Generic;

namespace SeriousAPI.Models
{
    public class SearchResultDTO
    {
        public IEnumerable<Product> products { get; set; }

        public IEnumerable<String> categories { get; set; }

    }
}
