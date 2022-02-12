namespace NowListening.Data;

public class MyCookie
{
    public bool Secure { get; set; }
    public bool HttpOnly { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
    public string Domain { get; set; }
    public string Path { get; set; }
    public long Expiry { get; set; }
}