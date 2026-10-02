// <copyright file="ServerStatusServiceTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Tests;

using MUnique.OpenMU.Web.Portal.Services;

/// <summary>
/// Tests for <see cref="ServerStatusService"/>.
/// </summary>
[TestFixture]
public class ServerStatusServiceTest
{
    /// <summary>
    /// Tests that the status object is parsed.
    /// </summary>
    [Test]
    public void ParsesObject()
    {
        var status = ServerStatusService.Parse("""{"state":"Online","players":42,"playersList":[]}""");
        Assert.That(status, Is.EqualTo(new ServerStatus(true, 42)));
    }

    /// <summary>
    /// Tests that the status object is parsed when it's serialized as JSON string, like the admin panel does.
    /// </summary>
    [Test]
    public void ParsesObjectSerializedAsString()
    {
        var status = ServerStatusService.Parse("\"{\\\"state\\\":\\\"Online\\\",\\\"players\\\":7,\\\"playersList\\\":[]}\"");
        Assert.That(status, Is.EqualTo(new ServerStatus(true, 7)));
    }
}
