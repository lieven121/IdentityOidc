namespace Identity.App.Models;

public class ResultPage<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; } = 0;
}
