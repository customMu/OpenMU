// <copyright file="RateLimitPolicies.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal;

/// <summary>
/// The names of the rate limiting policies.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// The policy for pages which accept credentials, like login and registration.
    /// </summary>
    public const string Credentials = "credentials";
}
