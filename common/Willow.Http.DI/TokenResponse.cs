using System;
using Newtonsoft.Json;

namespace Willow.Http.DI
{
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; init; } = "";

        [JsonProperty("id_token")]
        public string IdToken { get; init; } = "";

        [JsonProperty("scope")]
        public string Scope { get; init; } = "";

        [JsonProperty("expires_in")]
        public int? ExpiresIn { get; init; }

        [JsonProperty("token_type")]
        public string TokenType { get; init; } = "";
    }
}
