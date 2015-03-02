using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManageRecommendationModel
{
    /// <summary>
    /// hold catalog item info  (partial)
    /// </summary>
    public class CatalogItem
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, Name: {1}", Id, Name);
        }
    }
}
