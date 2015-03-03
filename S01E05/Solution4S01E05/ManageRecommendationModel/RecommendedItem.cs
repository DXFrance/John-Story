using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManageRecommendationModel
{
    /// <summary>
    /// Utility class holding a recommended item information.
    /// </summary>
    public class RecommendedItem
    {
        public string Name { get; set; }
        public string Rating { get; set; }
        public string Reasoning { get; set; }
        public string Id { get; set; }

        public override string ToString()
        {
            return string.Format("Name: {0}, Id: {1}, Rating: {2}, Reasoning: {3}", Name, Id, Rating, Reasoning);
        }
    }

}
