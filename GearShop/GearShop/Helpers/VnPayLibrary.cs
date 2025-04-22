using System.Net;
using System.Security.Cryptography;
using System.Text;

public class VnPayLibrary
{
    private SortedList<string, string> requestData = new(new VnPayCompare());
    public void AddRequestData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value)) requestData.Add(key, value);
    }

    public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
    {
        var data = new StringBuilder();
        foreach (var kv in requestData)
        {
            data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
        }

        var signData = data.ToString().TrimEnd('&');
        string sign = HmacSHA512(vnp_HashSecret, signData);
        return baseUrl + "?" + signData + "&vnp_SecureHash=" + sign;
    }

    public static string HmacSHA512(string key, string inputData)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        using var hmac = new HMACSHA512(keyBytes);
        var hashValue = hmac.ComputeHash(inputBytes);
        return BitConverter.ToString(hashValue).Replace("-", "").ToLower();
    }

    private class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y) => string.CompareOrdinal(x, y);
    }
}
