namespace ProductsImport.Services;

/// <summary>Hands out the smallest free positive article number (ass1001), filling gaps left by deletions.</summary>
public class ArticleAllocator
{
    private readonly HashSet<long> _used;
    private long _cursor = 1;

    public ArticleAllocator(IEnumerable<long> existingArticleIds)
    {
        _used = new HashSet<long>(existingArticleIds);
    }

    public long Next()
    {
        while (_used.Contains(_cursor))
        {
            _cursor++;
        }

        _used.Add(_cursor);
        return _cursor;
    }

    /// <summary>Reserves an article number chosen explicitly by a row so it can never be handed out again.</summary>
    public void Reserve(long articleId) => _used.Add(articleId);

    public bool IsTaken(long articleId) => _used.Contains(articleId);
}
