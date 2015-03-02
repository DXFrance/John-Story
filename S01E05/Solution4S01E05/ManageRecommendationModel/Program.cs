using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManageRecommendationModel
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string email = ConfigurationManager.AppSettings["azureDatamarket.email"];
                string key = ConfigurationManager.AppSettings["azureDatamarket.key"];

                if (email == null || key == null)
                    throw new ApplicationException("Please fill azureDatamarket.email and azureDatamarket.key in the configuration file");

                RecommendationModel model = null;

                string modelId = ConfigurationManager.AppSettings["recommendationModel.id"];
                if (modelId == null)
                {
                    model = new RecommendationModel(email, key);
                    modelId = model.CreateModel();
                    Console.WriteLine("Please update configuration with the following in appSettings:");
                    Console.WriteLine("<add key=\"recommendationModel.id\" value=\"{0}\" />", modelId);
                }
                else
                {
                    model = new RecommendationModel(email, key, modelId);
                }
                Console.WriteLine("using model {0}", model.ModelId);


            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex);
            }
            finally
            { 
                Console.WriteLine("---- done ----");
                Console.ReadLine();
            }
        }
    }
}
