// <copyright file="VaultResult.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services.Vault;

/// <summary>
/// The result of an operation of the <see cref="AccountVaultService"/>.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <param name="Code">The outcome.</param>
/// <param name="Value">The value, when <paramref name="Code"/> is <see cref="VaultResultCode.Success"/>.</param>
public sealed record VaultResult<T>(VaultResultCode Code, T? Value = default)
{
    /// <summary>
    /// Gets a value indicating whether the operation was done.
    /// </summary>
    public bool IsSuccess => this.Code == VaultResultCode.Success;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result.</returns>
    public static VaultResult<T> Success(T value) => new(VaultResultCode.Success, value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="code">The reason.</param>
    /// <returns>The result.</returns>
    public static VaultResult<T> Fail(VaultResultCode code) => new(code);
}
