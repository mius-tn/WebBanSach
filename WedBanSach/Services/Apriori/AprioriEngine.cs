using System.Collections.Concurrent;

namespace WedBanSach.Services.Apriori;

public class AprioriEngine
{
    private readonly double _minSupport;
    private readonly double _minConfidence;
    private readonly double _minLift;
    private readonly int _maxItemsetSize;
    private int _totalTransactions;

    // Itemset string format: "Id1,Id2,Id3" (sorted numerically)
    // Support dictionary: Key = Itemset string, Value = frequency count
    private Dictionary<string, int> _frequentItemsets = new();
    
    // Dataset: List of transactions. Each transaction is a HashSet of BookIDs.
    private List<HashSet<int>> _dataset = new();

    public AprioriEngine(double minSupport, double minConfidence, double minLift, int maxItemsetSize)
    {
        _minSupport = minSupport;
        _minConfidence = minConfidence;
        _minLift = minLift;
        _maxItemsetSize = maxItemsetSize;
    }

    /// <summary>
    /// Load transactions into the engine. Each transaction is a list of item IDs.
    /// </summary>
    public void LoadTransactions(List<List<int>> transactions)
    {
        _dataset = transactions.Select(t => new HashSet<int>(t)).ToList();
        _totalTransactions = _dataset.Count;
    }

    /// <summary>
    /// Run the Apriori algorithm to find frequent itemsets.
    /// Returns a dictionary of itemset keys (e.g. "1,2") and their support counts.
    /// </summary>
    public Dictionary<string, int> FindFrequentItemsets()
    {
        if (_totalTransactions == 0) return new Dictionary<string, int>();

        int minSupportCount = (int)Math.Ceiling(_minSupport * _totalTransactions);
        
        // Find 1-itemsets
        var L1 = FindFrequent1Itemsets(minSupportCount);
        foreach (var kvp in L1)
        {
            _frequentItemsets[kvp.Key] = kvp.Value;
        }

        var currentL = L1;
        int k = 2;

        while (currentL.Count > 0 && k <= _maxItemsetSize)
        {
            var candidates = GenerateCandidates(currentL.Keys.ToList(), k);
            var Lk = CountSupportAndPrune(candidates, minSupportCount);
            
            foreach (var kvp in Lk)
            {
                _frequentItemsets[kvp.Key] = kvp.Value;
            }

            currentL = Lk;
            k++;
        }

        return _frequentItemsets;
    }

    /// <summary>
    /// Generate association rules from the frequent itemsets.
    /// Returns a list of generated rules.
    /// </summary>
    public List<RuleResult> GenerateRules()
    {
        var rules = new ConcurrentBag<RuleResult>();

        // We only generate rules from itemsets of size >= 2
        var itemsetsToProcess = _frequentItemsets.Where(kvp => kvp.Key.Contains(",")).ToList();

        Parallel.ForEach(itemsetsToProcess, kvp =>
        {
            var itemsetStr = kvp.Key;
            var itemsetSupportCount = kvp.Value;
            var items = itemsetStr.Split(',').Select(int.Parse).ToList();

            // Generate all possible non-empty proper subsets as antecedents
            int n = items.Count;
            int subsetCount = (1 << n) - 1; // 2^n - 1 (excluding full set, 0 is empty)

            for (int i = 1; i < subsetCount; i++)
            {
                var antecedent = new List<int>();
                var consequent = new List<int>();

                for (int j = 0; j < n; j++)
                {
                    if ((i & (1 << j)) > 0)
                        antecedent.Add(items[j]);
                    else
                        consequent.Add(items[j]);
                }

                if (antecedent.Count > 0 && consequent.Count > 0)
                {
                    string antKey = string.Join(",", antecedent);
                    string conKey = string.Join(",", consequent);

                    if (_frequentItemsets.TryGetValue(antKey, out int antSupportCount) &&
                        _frequentItemsets.TryGetValue(conKey, out int conSupportCount))
                    {
                        double support = (double)itemsetSupportCount / _totalTransactions;
                        double confidence = (double)itemsetSupportCount / antSupportCount;
                        double conSupport = (double)conSupportCount / _totalTransactions;
                        double antSupport = (double)antSupportCount / _totalTransactions;
                        double lift = confidence / conSupport;

                        if (confidence >= _minConfidence && lift >= _minLift)
                        {
                            var rule = CalculateMetrics(antKey, conKey, itemsetSupportCount, antSupportCount, conSupportCount, support, confidence, lift, antSupport, conSupport);
                            rules.Add(rule);
                        }
                    }
                }
            }
        });

        return rules.ToList();
    }

    private RuleResult CalculateMetrics(string antKey, string conKey, int unionCount, int antCount, int conCount, double support, double confidence, double lift, double antSupport, double conSupport)
    {
        double conviction = (confidence == 1.0) ? 0 : (1 - conSupport) / (1 - confidence);
        double leverage = support - (antSupport * conSupport);
        double jaccard = (double)unionCount / (antCount + conCount - unionCount);
        double cosine = (double)unionCount / Math.Sqrt((double)antCount * conCount);
        
        // Kulczynski: 0.5 * (P(Y|X) + P(X|Y))
        double confXY = confidence;
        double confYX = (double)unionCount / conCount;
        double kulczynski = 0.5 * (confXY + confYX);

        // AllConfidence: sup(X U Y) / max(sup(X), sup(Y))
        double maxSupportCount = Math.Max(antCount, conCount);
        double allConfidence = (double)unionCount / maxSupportCount;
        
        double maxConf = Math.Max(confXY, confYX);

        // Recommendation Score heuristic (can be tweaked)
        double score = (confidence * 0.4) + (lift * 0.4) + (jaccard * 0.2);

        return new RuleResult
        {
            Antecedent = antKey,
            Consequent = conKey,
            Support = support,
            Confidence = confidence,
            Lift = lift,
            Conviction = conviction,
            Leverage = leverage,
            JaccardSimilarity = jaccard,
            CosineSimilarity = cosine,
            Kulczynski = kulczynski,
            AllConfidence = allConfidence,
            MaxConfidence = maxConf,
            RecommendationScore = score
        };
    }

    private Dictionary<string, int> FindFrequent1Itemsets(int minSupportCount)
    {
        var counts = new ConcurrentDictionary<int, int>();

        Parallel.ForEach(_dataset, transaction =>
        {
            foreach (var item in transaction)
            {
                counts.AddOrUpdate(item, 1, (k, v) => v + 1);
            }
        });

        var L1 = new Dictionary<string, int>();
        foreach (var kvp in counts)
        {
            if (kvp.Value >= minSupportCount)
            {
                L1[kvp.Key.ToString()] = kvp.Value;
            }
        }

        return L1;
    }

    private List<string> GenerateCandidates(List<string> previousFrequentItemsets, int k)
    {
        var candidates = new HashSet<string>();
        
        // Optimized candidate generation
        // Join L_k-1 with itself
        for (int i = 0; i < previousFrequentItemsets.Count; i++)
        {
            var itemset1 = previousFrequentItemsets[i].Split(',').Select(int.Parse).ToList();
            for (int j = i + 1; j < previousFrequentItemsets.Count; j++)
            {
                var itemset2 = previousFrequentItemsets[j].Split(',').Select(int.Parse).ToList();

                // Check if the first k-2 elements are identical
                bool canJoin = true;
                for (int m = 0; m < k - 2; m++)
                {
                    if (itemset1[m] != itemset2[m])
                    {
                        canJoin = false;
                        break;
                    }
                }

                if (canJoin)
                {
                    var newItemset = new List<int>(itemset1);
                    newItemset.Add(itemset2[k - 2]);
                    newItemset.Sort();

                    // Pruning: check if all (k-1) subsets of newItemset are frequent
                    if (HasFrequentSubsets(newItemset, previousFrequentItemsets, k))
                    {
                        candidates.Add(string.Join(",", newItemset));
                    }
                }
            }
        }

        return candidates.ToList();
    }

    private bool HasFrequentSubsets(List<int> candidate, List<string> previousFrequentItemsets, int k)
    {
        var prevSet = new HashSet<string>(previousFrequentItemsets);
        
        // Generate all k-1 subsets
        for (int i = 0; i < k; i++)
        {
            var subset = new List<int>(candidate);
            subset.RemoveAt(i);
            if (!prevSet.Contains(string.Join(",", subset)))
            {
                return false;
            }
        }
        return true;
    }

    private Dictionary<string, int> CountSupportAndPrune(List<string> candidates, int minSupportCount)
    {
        var candidateCounts = new ConcurrentDictionary<string, int>();

        Parallel.ForEach(_dataset, transaction =>
        {
            foreach (var candidate in candidates)
            {
                var items = candidate.Split(',').Select(int.Parse).ToList();
                bool isSubset = true;
                foreach (var item in items)
                {
                    if (!transaction.Contains(item))
                    {
                        isSubset = false;
                        break;
                    }
                }

                if (isSubset)
                {
                    candidateCounts.AddOrUpdate(candidate, 1, (k, v) => v + 1);
                }
            }
        });

        return candidateCounts.Where(kvp => kvp.Value >= minSupportCount)
                              .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}

public class RuleResult
{
    public string Antecedent { get; set; } = string.Empty;
    public string Consequent { get; set; } = string.Empty;
    public double Support { get; set; }
    public double Confidence { get; set; }
    public double Lift { get; set; }
    public double Conviction { get; set; }
    public double Leverage { get; set; }
    public double JaccardSimilarity { get; set; }
    public double CosineSimilarity { get; set; }
    public double Kulczynski { get; set; }
    public double AllConfidence { get; set; }
    public double MaxConfidence { get; set; }
    public double RecommendationScore { get; set; }
}
