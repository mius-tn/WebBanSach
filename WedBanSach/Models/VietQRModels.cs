namespace WedBanSach.Models;

public class BankResponse
{
    public string code { get; set; } = string.Empty;
    public string desc { get; set; } = string.Empty;
    public List<BankDto>? data { get; set; }
}

public class BankDto
{
    public string name { get; set; } = string.Empty;
    public string code { get; set; } = string.Empty;
    public string bin { get; set; } = string.Empty;
    public string shortName { get; set; } = string.Empty;
    public string logo { get; set; } = string.Empty;
}
