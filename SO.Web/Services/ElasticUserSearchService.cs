using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.EntityFrameworkCore;
using SO.Core;
using SO.Data;

namespace SO.Web.Services;

public class ElasticUserSearchService
{
    private readonly ElasticsearchClient _client;
    private readonly AppDbContext _db;
    private const string IndexName = "users";

    public ElasticUserSearchService(ElasticsearchClient client, AppDbContext db)
    {
        _client = client;
        _db = db;
    }

    public async Task CreateIndexAsync()
    {
        var indexExists = await _client.Indices.ExistsAsync(IndexName);

        if (indexExists.Exists)
            return;

        await _client.Indices.CreateAsync(IndexName, c => c
            .Mappings(m => m
                .Properties<UserEntity>(p => p
                    .Keyword(k => k.Id)
                    .Text(t => t.FirstName)
                    .Text(t => t.LastName)
                    .Text(t => t.Email)
                    .Text(t => t.TechStack)
                    .Text(t => t.Address)
                    .Text(t => t.College)
                    .Text(t => t.University)
                    .Text(t => t.Program)
                )
            )
        );
    }

    public async Task IndexUsersAsync()
    {
        await CreateIndexAsync();

        const int batchSize = 1000;
        var skip = 0;
        var hasMore = true;

        while (hasMore)
        {
            var users = await _db.Set<UserEntity>()
                .AsNoTracking()
                .Skip(skip)
                .Take(batchSize)
                .ToListAsync();

            if (!users.Any()) break;

            var bulkRequest = new BulkRequest(IndexName)
            {
                Operations = new List<IBulkOperation>()
            };

            foreach (var user in users)
            {
                bulkRequest.Operations.Add(new BulkIndexOperation<UserEntity>(user)
                {
                    Id = user.Id.ToString()
                });
            }

            var bulkResponse = await _client.BulkAsync(bulkRequest);

            if (bulkResponse.Errors)
            {
                foreach (var item in bulkResponse.ItemsWithErrors)
                {
                    Console.WriteLine($"❌ Failed to index document {item.Id}: {item.Error?.Reason}");
                }
            }

            skip += batchSize;
            hasMore = users.Count == batchSize;
        }
    }

    public async Task<List<UserEntity>> SearchUsersAsync(string query, int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<UserEntity>();

        var searchResponse = await _client.SearchAsync<UserEntity>(s => s
            .Index(IndexName)
            .Size(maxResults)
            .Query(q => q
                .MultiMatch(m => m
                    .Query(query)
                    .Fields(new[]
                    {
                        "firstName^3",
                        "lastName^3",
                        "techStack^2",
                        "email^2",
                        "address",
                        "college",
                        "university",
                        "program"
                    })
                    .Fuzziness(new Fuzziness("AUTO"))
                    .Type(TextQueryType.BestFields)
                )
            )
        );

        if (!searchResponse.IsValidResponse)
        {
            Console.WriteLine($"Search failed: {searchResponse.ElasticsearchServerError?.Error?.Reason}");
            return new List<UserEntity>();
        }

        return searchResponse.Documents.ToList();
    }

    public async Task IndexUserAsync(UserEntity user)
    {
        var response = await _client.IndexAsync(user, i => i
            .Index(IndexName)
            .Id(user.Id.ToString())
        );

        if (!response.IsValidResponse)
        {
            Console.WriteLine($"Failed to index user {user.Id}: {response.ElasticsearchServerError?.Error?.Reason}");
        }
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var request = new DeleteRequest("users", userId.ToString());
        var response = await _client.DeleteAsync(request);


        if (!response.IsValidResponse)
        {
            Console.WriteLine($"Failed to delete user {userId}: {response.ElasticsearchServerError?.Error?.Reason}");
        }
    }

    public async Task UpdateUserAsync(UserEntity user)
    {
        await IndexUserAsync(user);
    }
}