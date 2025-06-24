using System.ClientModel;

namespace MVCPrject
{
    internal class MistralClient
    {
        private ApiKeyCredential apiKeyCredential;
        private MistralClientOptions mistralClientOptions;

        public MistralClient(ApiKeyCredential apiKeyCredential, MistralClientOptions mistralClientOptions)
        {
            this.apiKeyCredential = apiKeyCredential;
            this.mistralClientOptions = mistralClientOptions;
        }
    }
}