using System;
using System.Net.Http;
using System.Threading.Tasks;

using Willow.Api.Client;

namespace Willow.Http.DI
{
    public class Auth0TokenService : ITokenService
    {
        private readonly IRestApi _auth0Api;

        public Auth0TokenService(IRestApi auth0Api)
        {
            _auth0Api = auth0Api ?? throw new ArgumentNullException(nameof(auth0Api));
        }

        #region ITokenService

        public async Task<TokenResponse> GetToken(ApiConfiguration config)
        {
            try
            {
                if(string.IsNullOrWhiteSpace(config.Password))
                { 
                    return await _auth0Api.Post<TokenRequest, TokenResponse>("oauth/token", new TokenRequest
                    {
                        client_id     = config.ClientId,
                        client_secret = config.ClientSecret,
                        audience      = config.Audience!
                    });
                }
                else
                {
                    return await _auth0Api.Post<TokenRequestForPassword, TokenResponse>("oauth/token", new TokenRequestForPassword
                    {
                        client_id     = config.ClientId,
                        client_secret = config.ClientSecret,
                        username      = config.UserName!,
                        password      = config.Password!
                    });
                }
            }
            catch (RestException rex)
            { 
                throw new HttpRequestException($"Failed to get access token. Http status code: {rex.StatusCode} ResponseBody: {rex.Response}", rex);
            }
            catch (NullReferenceException nex)
            { 
                throw new HttpRequestException("Missing Auth0 audience or username and password", nex);
            }
            catch (Exception ex)
            { 
                throw new HttpRequestException("Failed to get access token", ex);
            }
        }

        #endregion

        #region Private

        internal sealed class TokenRequest
        {
            public string client_id     { get; init; } = "";
            public string client_secret { get; init; } = "";
            public string audience      { get; init; } = "";
            public string grant_type    { get; init; } = "client_credentials";
        }

        internal sealed class TokenRequestForPassword
        {
            public string grant_type        { get; init; } = "password";
            public string client_id         { get; init; } = "";
            public string client_secret     { get; init; } = "";
            public string username          { get; init; } = "";
            public string password          { get; init; } = "";
            public string scope             { get; init; } = "openid";
        }

        #endregion
    }
}
