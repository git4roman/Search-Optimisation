using FuzzySharp;
using Microsoft.EntityFrameworkCore;
using SO.Core;
using SO.Data;

namespace SO.Web.Services;

public class UserSearchService
    {
        private readonly AppDbContext _db;

        public UserSearchService(AppDbContext db)
        {
            _db = db;
        }

        public List<UserEntity> SearchUsers(string query, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<UserEntity>();

            var tokens = query
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLower())
                .ToList();

            var users = _db.Set<UserEntity>().AsNoTracking().ToList();

            var scoredUsers = users.Select(u => new
            {
                User = u,
                Score = GetMatchScore(u, tokens)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .Select(x => x.User)
            .ToList();

            return scoredUsers;
        }

        private int GetMatchScore(UserEntity user, List<string> tokens)
        {
            int score = 0;

            var weights = new Dictionary<string, int>
            {
                { "FirstName", 3 },
                { "LastName", 3 },
                { "TechStack", 2 },
                { "Address", 1 },
                { "College", 1 },
                { "University", 1 },
                { "Email", 2 },
                { "Program", 1 }
            };

            foreach (var token in tokens)
            {
                score += MatchField(user.FirstName, token, weights["FirstName"]);
                score += MatchField(user.LastName, token, weights["LastName"]);
                score += MatchField(user.TechStack, token, weights["TechStack"]);
                score += MatchField(user.Address, token, weights["Address"]);
                score += MatchField(user.College, token, weights["College"]);
                score += MatchField(user.University, token, weights["University"]);
                score += MatchField(user.Email, token, weights["Email"]);
                score += MatchField(user.Program, token, weights["Program"]);
            }

            return score;
        }

        private int MatchField(string fieldValue, string token, int weight)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
                return 0;

            fieldValue = fieldValue.ToLower();

            if (fieldValue.Contains(token))
                return 10 * weight;

            int fuzzyScore = Fuzz.PartialRatio(fieldValue, token); 
            if (fuzzyScore >= 70) 
                return (fuzzyScore / 10) * weight; 

            return 0;
        }
    }
