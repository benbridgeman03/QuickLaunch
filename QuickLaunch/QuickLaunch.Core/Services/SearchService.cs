using QuickLaunch.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickLaunch.Core.Services
{
    public class SearchService
    {
        private readonly FileIndexer _indexer;

        public SearchService(FileIndexer fileIndexer)
        {
            _indexer = fileIndexer;
        }

        public List<SearchResultItem> SearchItem(string query)
        {
            int relevance = 0;

            var results = _indexer.Items
                .Where(i =>
                    !string.IsNullOrEmpty(i.FileName) && (
                        i.FileName.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (!string.IsNullOrEmpty(i.Desc) &&
                            i.Desc.Contains(query, StringComparison.OrdinalIgnoreCase))
                        || SearchService.GetFuzzyScore(query, i.FileName) > 50
                        || (!string.IsNullOrEmpty(i.Desc) &&
                            SearchService.GetFuzzyScore(query, i.Desc) > 50)
                    )
                )
                .Select(i =>
                {
                    int nameScore = SearchService.GetFuzzyScore(query, i.FileName);
                    int descScore = string.IsNullOrEmpty(i.Desc) ? 0 : SearchService.GetFuzzyScore(query, i.Desc);

                    int relevance = Math.Max(nameScore, descScore);

                    if (string.Equals(i.FileName, query, StringComparison.OrdinalIgnoreCase))
                        relevance += 50;
                    else if (i.FileName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                        relevance += 20;

                    return new
                    {
                        Item = i,
                        TotalScore = i.Score + relevance,
                        Relevance = relevance
                    };
                })
                .OrderByDescending(x => x.TotalScore)
                .Take(3)
                .Select(x => new SearchResultItem
                {
                    Display = $"{(string.IsNullOrWhiteSpace(x.Item.Desc)
                                    ? x.Item.FileName
                                    : x.Item.Desc)} – {x.Item.Type} - {x.TotalScore}",
                    Item = x.Item
                })
                .ToList();


            return results;
        }

        public static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t.Length;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int[,] dp = new int[s.Length + 1, t.Length + 1];

            for (int i = 0; i <= s.Length; i++)
                dp[i, 0] = i;
            for (int j = 0; j <= t.Length; j++)
                dp[0, j] = j;

            for (int i = 1; i <= s.Length; i++)
            {
                for (int j = 1; j <= t.Length; j++)
                {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                        dp[i - 1, j - 1] + cost
                    );
                }
            }

            return dp[s.Length, t.Length];
        }

        public static int GetFuzzyScore(string query, string target)
        {
            int distance = LevenshteinDistance(query.ToLower(), target.ToLower());

            int maxLen = Math.Max(query.Length, target.Length);

            int score = (int)((1.0 - (double)distance / maxLen) * 100);
            return Math.Max(score, 0);
        }


    }
}
