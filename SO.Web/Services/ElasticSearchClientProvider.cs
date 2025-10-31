using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

public class ElasticSearchClientProvider
{
    private readonly ElasticsearchClient _client;

    public ElasticSearchClientProvider(IConfiguration config)
    {
        var uri = new Uri(config["ElasticSearch:Url"]); 
        var settings = new ElasticsearchClientSettings(uri)
            .DefaultIndex("users");
          

        if (config["ElasticSearch:Username"] != null)
        {
            settings = settings.Authentication(
                new BasicAuthentication(
                    config["ElasticSearch:Username"],
                    config["ElasticSearch:Password"]
                )
            );
        }

        if (config["ElasticSearch:CertificateFingerprint"] != null)
        {
            settings = settings.CertificateFingerprint(config["ElasticSearch:CertificateFingerprint"]);
        }

        _client = new ElasticsearchClient(settings);
    }

    public ElasticsearchClient Client => _client;
}