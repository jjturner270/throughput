namespace Throughput.Sim;

using System.Security.Cryptography;
using System.Text;

public sealed class StateHasher
{
    private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

    public StateHasher Begin(string section)
    {
        Append("[" + section + "]\n");
        return this;
    }

    public StateHasher Field(string key, string value)
    {
        Append(key);
        Append("=");
        Append(value);
        Append("\n");
        return this;
    }

    public StateHasher Field(string key, long value) => Field(key, value.ToString("D"));
    public StateHasher Field(string key, int value) => Field(key, value.ToString("D"));

    public string Finish()
    {
        byte[] bytes = _hash.GetHashAndReset();
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private void Append(string text) => _hash.AppendData(Encoding.UTF8.GetBytes(text));
}
