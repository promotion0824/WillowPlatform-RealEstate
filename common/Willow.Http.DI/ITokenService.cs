using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Willow.Http.DI
{
    public interface ITokenService
    {
        Task<TokenResponse> GetToken(ApiConfiguration config);
    }

    public class ApiConfiguration
    {
        public string  ClientId      { get; init; } = "";
        public string  ClientSecret  { get; init; } = "";
        public string? Audience      { get; init; }
        public string? UserName      { get; init; }
        public string? Password      { get; init; }
   }
}
