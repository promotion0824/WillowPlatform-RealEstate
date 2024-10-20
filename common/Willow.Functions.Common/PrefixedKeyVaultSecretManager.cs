using System.Linq;
using Microsoft.Extensions.Configuration;
using Azure.Security.KeyVault.Secrets;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using System;
using Google.Protobuf;

namespace Willow.Functions.Common;


/// <summary>
/// Manages Key Vault secrets based on specified prefixes, allowing selective loading of secrets that match the specified prefixes. 
/// This class is used when adding Azure Key Vault as a configuration source by providing it as a parameter in the configuration 
/// extension method. The double hyphen ("--") is used as the delimiter to split the prefix and secret in the secret names.
/// 
/// For example, if the prefixes are {"Common", "CommSvc"}:
/// - A secret named "Common--MySecret" will become a configuration setting called "MySecret".
/// - A secret named "CommSvc--MyGroup--MyOtherSecret" will become a configuration setting called "MyGroup:MyOtherSecret".
/// </summary>
public class PrefixedKeyVaultSecretManager : KeyVaultSecretManager
{
    string[] _prefixes;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefixedKeyVaultSecretManager"/> class with the specified prefixes.
    /// </summary>
    /// <param name="prefixes">The prefixes to filter the Key Vault secrets.</param>
    public PrefixedKeyVaultSecretManager(string[] prefixes)
    {
        _prefixes = prefixes;
    }

    /// <summary>
    /// Determines if a secret should be loaded based on the specified prefixes.
    /// </summary>
    /// <param name="secret">The secret properties.</param>
    /// <returns><c>true</c> if the secret should be loaded; otherwise, <c>false</c>.</returns>
    public override bool Load(SecretProperties secret)
    {
        return _prefixes.Any(p => secret.Name.StartsWith(p + "--", StringComparison.InvariantCulture));
    }

    /// <summary>
    /// Extracts the key from the secret's name while replacing delimiters.
    /// </summary>
    /// <param name="secret">The Key Vault secret.</param>
    /// <returns>The extracted key.</returns>
    public override string GetKey(KeyVaultSecret secret)
    {
        var prefix = _prefixes.FirstOrDefault(p => secret.Name.StartsWith(p + "--", StringComparison.InvariantCulture));
        return secret.Name.Substring((prefix + "--").Length)
            .Replace("--", ConfigurationPath.KeyDelimiter, StringComparison.InvariantCulture);
    }
}

/// <summary>
/// Provides an extension method to add a prefixed Key Vault as a configuration source.
/// </summary>
public static class Extension
{
    /// <summary>
    /// Adds a prefixed Key Vault as a configuration source.
    /// </summary>
    /// <param name="config">The configuration builder.</param>
    /// <param name="keyVaultName">The name of the Key Vault.</param>
    /// <param name="prefixes">The prefixes to filter the Key Vault secrets.</param>
    /// <returns>The updated configuration builder.</returns>
    public static IConfigurationBuilder AddPrefixedKeyVault(
        this IConfigurationBuilder config,
        string keyVaultName,
        string[] prefixes)
    {
        return config.AddAzureKeyVault(
            new Uri($"https://{keyVaultName}.vault.azure.net"),
            new DefaultAzureCredential(),
            new PrefixedKeyVaultSecretManager(prefixes));
    }
}
