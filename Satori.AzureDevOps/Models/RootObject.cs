namespace Satori.AzureDevOps.Models;

public class RootObject<T>
{
    public int count { get; set; }
    public T[] value { get; set; }
}