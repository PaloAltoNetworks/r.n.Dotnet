namespace rnDotnet.Services.Assembly
{
    public interface IFileHasher
    {
        string CalculateSHA256(string filePath);
    }
}