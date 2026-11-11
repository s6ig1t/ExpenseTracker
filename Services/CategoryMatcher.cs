using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

/// <summary>
/// Maps a free-text expense description to the closest ExpenseCategory
/// using keyword matching. Falls back to "Miscellaneous".
/// </summary>
public class CategoryMatcher
{
    private static readonly Dictionary<string, string[]> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Housing"]         = ["rent", "mortgage", "hoa", "landlord", "lease", "property"],
        ["Food & Dining"]   = ["grocery", "groceries", "food", "restaurant", "dining", "lunch",
                                "dinner", "breakfast", "coffee", "cafe", "pizza", "takeout",
                                "doordash", "ubereats", "grubhub", "walmart food", "costco"],
        ["Transportation"]  = ["gas", "fuel", "uber", "lyft", "bus", "metro", "toll", "parking",
                                "car payment", "auto", "vehicle", "train", "subway"],
        ["Utilities"]       = ["electric", "electricity", "water", "sewer", "gas bill",
                                "internet", "cable", "phone", "mobile", "utility", "utilities"],
        ["Healthcare"]      = ["doctor", "dentist", "pharmacy", "prescription", "medical",
                                "health", "hospital", "clinic", "copay", "co-pay", "vision",
                                "glasses", "therapy"],
        ["Entertainment"]   = ["movie", "concert", "netflix", "hulu", "spotify", "disney",
                                "game", "gaming", "steam", "entertainment", "ticket", "show"],
        ["Shopping"]        = ["amazon", "target", "clothing", "clothes", "shoes", "apparel",
                                "electronics", "shopping", "store", "mall", "purchase"],
        ["Insurance"]       = ["insurance", "insur", "premium", "policy", "geico", "allstate",
                                "progressive", "coverage"],
        ["Savings"]         = ["saving", "savings", "401k", "ira", "investment", "roth",
                                "brokerage", "transfer to savings"],
        ["Subscriptions"]   = ["subscription", "membership", "annual fee", "monthly fee",
                                "patreon", "substack"],
        ["Education"]       = ["tuition", "school", "college", "course", "udemy", "coursera",
                                "book", "textbook", "student loan", "education"],
        ["Travel"]          = ["flight", "airbnb", "hotel", "airfare", "airline", "vacation",
                                "travel", "trip", "baggage"],
        ["Personal Care"]   = ["haircut", "hair", "salon", "spa", "gym", "fitness", "barber",
                                "skincare", "cosmetics", "beauty"],
    };

    public ExpenseCategory Match(string description, IEnumerable<ExpenseCategory> categories)
    {
        var catList = categories.ToList();

        foreach (var (categoryName, keywords) in Keywords)
        {
            if (keywords.Any(kw =>
                    description.Contains(kw, StringComparison.OrdinalIgnoreCase)))
            {
                var match = catList.FirstOrDefault(c =>
                    c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
        }

        // Fallback: Miscellaneous
        return catList.FirstOrDefault(c =>
            c.Name.Equals("Miscellaneous", StringComparison.OrdinalIgnoreCase))
            ?? catList.First();
    }
}
