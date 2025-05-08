using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class UserParameterDao
{
    private readonly HttpClient _http = HttpClientFactory.GetClient();

    public class UserParameter
    {
        [JsonProperty("attack")]  public int Attack  { get; set; }
        [JsonProperty("defence")] public int Defence { get; set; }
    }

    /// <summary>
    /// 指定した userId の attack, defence を取得する。
    /// </summary>
    public async Task<UserParameter> GetUserParametersAsync(string userId)
    {
        // PostgREST エンドポイント: /rest/v1/user_parameter
        // クエリ: ?select=attack,defence&user_id=eq.<userId>
        var url = $"/rest/v1/user_parameter?select=attack,defence&user_id=eq.{Uri.EscapeDataString(userId)}";

        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        // Supabase は配列を返すので、先頭要素を取る
        var list = JsonConvert.DeserializeObject<UserParameter[]>(json);
        if (list.Length == 0)
            throw new Exception($"UserParameter not found for user_id={userId}");

        return list[0];
    }
}
