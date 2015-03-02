using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManageRecommendationModel
{
    /// <summary>
    /// represent the build status
    /// </summary>
    public enum BuildStatus
    {
        Create,
        Queued,
        Building,
        Success,
        Error,
        Cancelling,
        Cancelled
    }
}
