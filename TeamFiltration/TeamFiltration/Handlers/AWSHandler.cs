using Amazon.APIGateway;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TeamFiltration.Models.TeamFiltration;

namespace TeamFiltration.Handlers
{
    public class AWSHandler
    {
        /*
            All credit to the peeps as Black Hills Information Security, this is simply a C# implementation of their FireProx tool.
            https://github.com/ustayready/fireprox
        */
        private static GlobalArgumentsHandler _globalProperties { get; set; }
        private static DatabaseHandler _databaseHandler { get; set; }
        private static BasicAWSCredentials _basicAWSCredentials { get; set; }
        private static SessionAWSCredentials _sessionAWSCredentials { get; set; }
        private static AWSCredentials _AWSCredentials { get; set; }


        public async Task<bool> DeleteFireProxEndpoint(string fireProxId, string region)
        {
            var amazonAPIGatewayClient = new AmazonAPIGatewayClient(_AWSCredentials, Amazon.RegionEndpoint.GetBySystemName(region));

            Amazon.APIGateway.Model.DeleteRestApiResponse deleteRestApiResponse = await amazonAPIGatewayClient.DeleteRestApiAsync(new Amazon.APIGateway.Model.DeleteRestApiRequest() { RestApiId = fireProxId });

            if (deleteRestApiResponse.HttpStatusCode == System.Net.HttpStatusCode.Accepted)
            {
                _databaseHandler.WriteLog(new Log("FIREPROX", $"Deleted endpoint https://{fireProxId}.execute-api.{region}.amazonaws.com/fireprox/", ""));

                _databaseHandler.DeleteFireProxEndpoint(fireProxId);

                return true;
            }


            return false;
        }
        
        /*
        public async Task ListFireProxEndpoint()
        {
            
            foreach (var item in _globalProperties.AWSRegions)
            {
                var amazonAPIGatewayClient = new AmazonAPIGatewayClient(_AWSCredentials, Amazon.RegionEndpoint.GetBySystemName(item));

                Amazon.APIGateway.Model.GetRestApisResponse getRestApisResponse = await amazonAPIGatewayClient.GetRestApisAsync(new Amazon.APIGateway.Model.GetRestApisRequest() { });
            }
        }
        */
        public async Task<(Amazon.APIGateway.Model.CreateDeploymentRequest, Models.AWS.FireProxEndpoint)> CreateFireProxEndPoint(string url, string title, string region)
        {
            var amazonAPIGatewayClient = new AmazonAPIGatewayClient(_AWSCredentials, Amazon.RegionEndpoint.GetBySystemName(region));

            if (url.EndsWith('/'))
                url = url.Substring(0, url.Length - 1);

            string versionDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            string template = @"
        {
          ""swagger"": ""2.0"",
          ""info"": {
            ""version"": ""{{version_date}}"",
            ""title"": ""{{title}}""
          },
          ""basePath"": ""/"",
          ""schemes"": [
            ""https""
          ],
          ""paths"": {
            ""/"": {
              ""get"": {
                ""parameters"": [
                  {
                    ""name"": ""proxy"",
                    ""in"": ""path"",
                    ""required"": true,
                    ""type"": ""string""
                  },
                  {
                    ""name"": ""X-My-X-Forwarded-For"",
                    ""in"": ""header"",
                    ""required"": false,
                    ""type"": ""string""
                  }
                ],
                ""responses"": {},
                ""x-amazon-apigateway-integration"": {
                  ""uri"": ""{{url}}/"",
                  ""responses"": {
                    ""default"": {
                      ""statusCode"": ""200""
                    }
                  },
                  ""requestParameters"": {
                    ""integration.request.path.proxy"": ""method.request.path.proxy"",
                    ""integration.request.header.X-Forwarded-For"": ""method.request.header.X-My-X-Forwarded-For""
                  },
                  ""passthroughBehavior"": ""when_no_match"",
                  ""httpMethod"": ""ANY"",
                  ""cacheNamespace"": ""irx7tm"",
                  ""cacheKeyParameters"": [
                    ""method.request.path.proxy""
                  ],
                  ""type"": ""http_proxy""
                }
              }
            },
            ""/{proxy+}"": {
              ""x-amazon-apigateway-any-method"": {
                ""produces"": [
                  ""application/json""
                ],
                ""parameters"": [
                  {
                    ""name"": ""proxy"",
                    ""in"": ""path"",
                    ""required"": true,
                    ""type"": ""string""
                  },
                  {
                    ""name"": ""X-My-X-Forwarded-For"",
                    ""in"": ""header"",
                    ""required"": false,
                    ""type"": ""string""
                  }
                ],
                ""responses"": {},
                ""x-amazon-apigateway-integration"": {
                  ""uri"": ""{{url}}/{proxy}"",
                  ""responses"": {
                    ""default"": {
                      ""statusCode"": ""200""
                    }
                  },
                  ""requestParameters"": {
              ""integration.request.path.proxy"": ""method.request.path.proxy"",
                    ""integration.request.header.X-Forwarded-For"": ""method.request.header.X-My-X-Forwarded-For""
                  },
                  ""passthroughBehavior"": ""when_no_match"",
                  ""httpMethod"": ""ANY"",
                  ""cacheNamespace"": ""irx7tm"",
                  ""cacheKeyParameters"": [
                    ""method.request.path.proxy""
                  ],
                  ""type"": ""http_proxy""
                }
              }
            }
          }
        }";
            template = template.Replace("{{url}}", url);
            template = template.Replace("{{title}}", "teamfiltration_fireprox_" + title);
            template = template.Replace("{{version_date}}", versionDate);

            var templateBytes = Encoding.UTF8.GetBytes(template);

            var importRestApiAsyncResponse = await amazonAPIGatewayClient.ImportRestApiAsync(
                new Amazon.APIGateway.Model.ImportRestApiRequest()
                {
                    Parameters = new Dictionary<string, string> {
                        { "endpointConfigurationTypes", "REGIONAL" }

                    },
                    Body = new System.IO.MemoryStream(templateBytes)
                }
            );

            Amazon.APIGateway.Model.CreateDeploymentRequest createDeploymentRequest = new Amazon.APIGateway.Model.CreateDeploymentRequest()
            {
                Description = "TeamFiltration FireProx Prod",
                StageDescription = "TeamFiltration FireProx Prod Stage",
                StageName = "fireprox",
                RestApiId = importRestApiAsyncResponse.Id
            };

            Amazon.APIGateway.Model.CreateDeploymentResponse createDeploymentAsyncResponse = await amazonAPIGatewayClient.CreateDeploymentAsync(createDeploymentRequest);


            _databaseHandler.WriteLog(new Log("FIREPROX", $"Created endpoint https://{createDeploymentRequest.RestApiId}.execute-api.{region}.amazonaws.com/fireprox/", ""));

            var fireproxEndpoint = new Models.AWS.FireProxEndpoint()
            {
                Deleted = false,
                Active = true,
                URL = url,
                FireProxURL = $"https://{createDeploymentRequest.RestApiId}.execute-api.{region}.amazonaws.com/fireprox/",
                Region = region,
                RestApiId = createDeploymentRequest.RestApiId
            };

            _databaseHandler.WriteFireProxEndpoint(fireproxEndpoint);


            return (createDeploymentRequest, fireproxEndpoint);
        }

        public AWSHandler(string AWSAccessKey, string AWSSecretKey, string AWSSessionToken, DatabaseHandler databaseHandler)
        {
            _databaseHandler = databaseHandler;

            if (!string.IsNullOrEmpty(AWSAccessKey) && !string.IsNullOrEmpty(AWSSecretKey) && string.IsNullOrEmpty(AWSSessionToken))
            {

                _basicAWSCredentials = new BasicAWSCredentials(
                    AWSAccessKey,
                    AWSSecretKey
                    );
                _AWSCredentials = _basicAWSCredentials;
            }
            else if (!string.IsNullOrEmpty(AWSAccessKey) && !string.IsNullOrEmpty(AWSSecretKey) && !string.IsNullOrEmpty(AWSSessionToken))
            {
                _sessionAWSCredentials = new SessionAWSCredentials(
                    AWSAccessKey,
                    AWSSecretKey,
                    AWSSessionToken
                    );
                _AWSCredentials = _sessionAWSCredentials;
            }

        }
    }
}