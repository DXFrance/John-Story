﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ManageRecommendationModel
{
    public class RecommendationModel
    {
        private const string ROOT_URI = "https://api.datamarket.azure.com/amla/recommendations/v2/";

        // API documentation is available at http://azure.microsoft.com/en-us/documentation/articles/machine-learning-recommendation-api-documentation/
        private const string BUILD_MODEL_URL = "BuildModel?modelId=%27{0}%27&userDescription=%27{1}%27&apiVersion=%271.0%27";
        private const string BUILD_STATUS_URL = "GetModelBuildsStatus?modelId=%27{0}%27&onlyLastBuild={1}&apiVersion=%271.0%27";
        private const string CREATE_MODEL_URL = "CreateModel?modelName=%27{0}%27&apiVersion=%271.0%27";
        private const string DELETE_MODEL_URL = "DeleteModel?id=%27{0}%27&apiVersion=%271.0%27";
        private const string GET_ALL_MODELS = "GetAllModels?apiVersion=%271.0%27";
        private const string GET_RECOMMENDATION_URL = "ItemRecommend?modelId=%27{0}%27&itemIds=%27{1}%27&numberOfResults={2}&includeMetadata={3}&apiVersion=%271.0%27";
        private const string IMPORT_CATALOG_URL = "ImportCatalogFile?modelId=%27{0}%27&filename=%27{1}%27&apiVersion=%271.0%27";
        private const string IMPORT_USAGE_URL = "ImportUsageFile?modelId=%27{0}%27&filename=%27{1}%27&apiVersion=%271.0%27";
        private const string UPDATE_MODEL_URL = "UpdateModel?id=%27{0}%27&apiVersion=%271.0%27";

        private const string MODEL_NAME = "entre_chats";
        
        private string _email;
        private string _key;
        private HttpClient _httpClient;
        private string _latestBuildId=null;

        public string ModelId { get; private set; }

        public RecommendationModel(string email, string key)
        {
            _email = email;
            _key = key;

            _httpClient = new HttpClient();
            var pass = GeneratePass(email, key);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", pass);
            _httpClient.BaseAddress = new Uri(ROOT_URI);
        }

        public RecommendationModel(string email, string key, string modelId)
            :this(email, key)
        {
            this.ModelId = modelId;
        }

        /// <summary>
        /// create the model with the given name.
        /// </summary>
        /// <param name="deleteExistingModelIfAny">
        /// if a model already exists, it will be deleted before a new one is created
        /// </param>
        /// <returns>The model id</returns>
        public string CreateModel(bool deleteExistingModelIfAny)
        {
            if (deleteExistingModelIfAny)
            {
                if (this.ModelId == null)
                    GetExistingModel(null);

                if (this.ModelId != null)
                    DeleteModel();
            }

            if (this.ModelId != null)
                throw new ApplicationException("Model was already created");

            var request = new HttpRequestMessage(HttpMethod.Post, String.Format(CREATE_MODEL_URL, MODEL_NAME));
            var response = _httpClient.SendAsync(request).Result;

            if (!response.IsSuccessStatusCode)
            {
                Exception firstException = new Exception(String.Format("Error {0}: Failed to create model {1}, \n reason {2}",
                    response.StatusCode, MODEL_NAME, ExtractErrorInfo(response)));

                if (deleteExistingModelIfAny)
                    throw firstException; // already removed a potential existing model. Throw the exception.
                else
                    GetExistingModel(firstException); // try to find existing model.

                return this.ModelId;
            }

            //process response if success
            string modelId = null;

            var node = XmlUtils.ExtractXmlElement(response.Content.ReadAsStreamAsync().Result, "//a:entry/a:content/m:properties/d:Id");
            if (node != null)
                modelId = node.InnerText;

            this.ModelId = modelId;
            return modelId;
        }

        private void GetExistingModel(Exception firstException)
        {
            // try to get the model. It may have already been created.
            var request = new HttpRequestMessage(HttpMethod.Get, GET_ALL_MODELS);
            var response = _httpClient.SendAsync(request).Result;

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(String.Format("Error {0}: Failed to get all models, \n reason {1}",
                    response.StatusCode, ExtractErrorInfo(response)), firstException);
            }

            var node = XmlUtils.ExtractXmlElement(response.Content.ReadAsStreamAsync().Result,
                string.Format("//a:entry[a:content/m:properties/d:Name='{0}']/a:content/m:properties/d:Id", MODEL_NAME));

            if (node == null)
            {
                throw new Exception(String.Format("Could not create nor find model {0}", MODEL_NAME),
                    firstException);
            }

            this.ModelId = node.InnerText;
        }

        public void DeleteModel()
        {
            var request = new HttpRequestMessage(HttpMethod.Delete,
                String.Format(DELETE_MODEL_URL, this.ModelId));
            var response = _httpClient.SendAsync(request).Result;

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    String.Format(
                        "Error {0}: Failed to delete model {1}, \n reason {2}",
                        response.StatusCode, this.ModelId, ExtractErrorInfo(response)));
            }

            this.ModelId = null;
        }

        public ImportReport ImportCatalog(string catalogFilePath)
        {
            return ImportFile(IMPORT_CATALOG_URL, catalogFilePath);
        }

        public ImportReport ImportUsage(string usageFilePath)
        {
            return ImportFile(IMPORT_USAGE_URL, usageFilePath);
        }

        public string BuildModel()
        {
            string description = "build of " + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var request = new HttpRequestMessage(HttpMethod.Post, String.Format(BUILD_MODEL_URL, this.ModelId, description));

            //setup the build parameters here we use a simple build without feature usage, for a complete list and 
            //explanation check the API document AT
            //http://azure.microsoft.com/en-us/documentation/articles/machine-learning-recommendation-api-documentation/#1113-recommendation-build-parameters
            request.Content = new StringContent("<BuildParametersList>" +
                                                "<NumberOfModelIterations>10</NumberOfModelIterations>" +
                                                "<NumberOfModelDimensions>20</NumberOfModelDimensions>" +
                                                "<ItemCutOffLowerBound>1</ItemCutOffLowerBound>" +
                                                "<EnableModelingInsights>false</EnableModelingInsights>" +
                                                "<UseFeaturesInModel>false</UseFeaturesInModel>" +
                                                "<ModelingFeatureList></ModelingFeatureList>" +
                                                "<AllowColdItemPlacement>true</AllowColdItemPlacement>" +
                                                "<EnableFeatureCorrelation>false</EnableFeatureCorrelation>" +
                                                "<ReasoningFeatureList></ReasoningFeatureList>" +
                                                "</BuildParametersList>");

            var response = _httpClient.SendAsync(request).Result;


            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(String.Format("Error {0}: Failed to start build for model {1}, \n reason {2}",
                    response.StatusCode, this.ModelId, ExtractErrorInfo(response)));
            }
            string buildId = null;
            //process response if success
            var node = XmlUtils.ExtractXmlElement(response.Content.ReadAsStreamAsync().Result, "//a:entry/a:content/m:properties/d:Id");
            if (node != null)
                buildId = node.InnerText;

            _latestBuildId = buildId;

            return buildId;
        }

        public BuildStatus GetLatestBuildStatus()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, String.Format(BUILD_STATUS_URL, this.ModelId, false));
            var response = _httpClient.SendAsync(request).Result;

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(String.Format("Error {0}: Failed to retrieve build for status for model {1} and build id {2}, \n reason {3}",
                    response.StatusCode, this.ModelId, _latestBuildId, ExtractErrorInfo(response)));
            }
            string buildStatusStr = null;
            var node = XmlUtils.ExtractXmlElement(response.Content.ReadAsStreamAsync().Result, string.Format("//a:entry/a:content/m:properties[d:BuildId='{0}']/d:Status", _latestBuildId));
            if (node != null)
                buildStatusStr = node.InnerText;

            BuildStatus buildStatus;
            if (!Enum.TryParse(buildStatusStr, true, out buildStatus))
            {
                throw new Exception(string.Format("Failed to parse build status for value {0} of build {1} for model {2}",
                    buildStatusStr, _latestBuildId, this.ModelId));
            }

            return buildStatus;
        }

        public IEnumerable<RecommendedItem> InvokeRecommendations(List<CatalogItem> seedItems)
        {
            List<CatalogItem> recommendations = new List<CatalogItem>();
            
            return GetRecommendation(seedItems.Select(i => i.Id).ToList(), 5);
        }

        /// <summary>
        /// Retrieve recommendation for the given item(s)
        /// </summary>
        /// <param name="modelId"></param>
        /// <param name="itemIdList"></param>
        /// <param name="numberOfResult">the number of result to include in the response</param>
        /// <param name="includeMetadata">true, means meta data will be returned too</param>
        /// <returns>a collection of recommended items</returns>
        private IEnumerable<RecommendedItem> GetRecommendation(List<string> itemIdList, int numberOfResult,
            bool includeMetadata = false)
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                String.Format(GET_RECOMMENDATION_URL, this.ModelId, string.Join(",", itemIdList), numberOfResult,
                    includeMetadata));
            var response = _httpClient.SendAsync(request).Result;

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    String.Format(
                        "Error {0}: Failed to retrieve recommendation for item list {1} and model {2}, \n reason {3}",
                        response.StatusCode, string.Join(",", itemIdList), this.ModelId, ExtractErrorInfo(response)));
            }
            var recoList = new List<RecommendedItem>();


            var nodeList = XmlUtils.ExtractXmlElementList(response.Content.ReadAsStreamAsync().Result, "//a:entry/a:content/m:properties");

            foreach (var node in (nodeList))
            {
                var item = new RecommendedItem();
                //cycle through the recommended items
                foreach (var child in ((XmlElement)node).ChildNodes)
                {
                    //cycle through properties
                    var nodeName = ((XmlNode)child).LocalName;
                    switch (nodeName)
                    {
                        case "Id":
                            item.Id = ((XmlNode)child).InnerText;
                            break;
                        case "Name":
                            item.Name = ((XmlNode)child).InnerText;
                            break;
                        case "Rating":
                            item.Rating = ((XmlNode)child).InnerText;
                            break;
                        case "Reasoning":
                            item.Reasoning = ((XmlNode)child).InnerText;
                            break;
                    }

                }
                recoList.Add(item);
            }
            return recoList;
        }

        private ImportReport ImportFile(string importUri, string filePath)
        {
            var filestream = new FileStream(filePath, FileMode.Open);
            var fileName = Path.GetFileName(filePath);
            var request = new HttpRequestMessage(HttpMethod.Post, String.Format(importUri, this.ModelId, fileName));

            request.Content = new StreamContent(filestream);
            var response = _httpClient.SendAsync(request).Result;


            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    String.Format("Error {0}: Failed to import file {1}, for model {2} \n reason {3}",
                        response.StatusCode, filePath, this.ModelId, ExtractErrorInfo(response)));
            }

            //process response if success
            var nodeList = XmlUtils.ExtractXmlElementList(response.Content.ReadAsStreamAsync().Result,
                "//a:entry/a:content/m:properties/*");

            ImportReport report = new ImportReport { Info = fileName };
            foreach (XmlNode node in nodeList)
            {
                if ("LineCount".Equals(node.LocalName))
                {
                    report.LineCount = int.Parse(node.InnerText);
                }
                if ("ErrorCount".Equals(node.LocalName))
                {
                    report.ErrorCount = int.Parse(node.InnerText);
                }
            }
            return report;
        }

        /// <summary>
        /// Generate the key to allow accessing DM API
        /// </summary>
        /// <param name="email">the user email</param>
        /// <param name="accountKey">the user account key</param>
        /// <returns></returns>
        private string GeneratePass(string email, string accountKey)
        {
            var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", email, accountKey));
            return Convert.ToBase64String(byteArray);
        }

        /// <summary>
        /// Extract error message from the httpResponse, (reason phrase + body)
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static string ExtractErrorInfo(HttpResponseMessage response)
        {
            //DM send the error message in body so need to extract the info from there
            string detailedReason = null;
            if (response.Content != null)
            {
                detailedReason = response.Content.ReadAsStringAsync().Result;
            }
            var errorMsg = detailedReason == null ? response.ReasonPhrase : response.ReasonPhrase + "->" + detailedReason;
            return errorMsg;

        }
    }
}
