// <copyright file="PlugInDiscoveryTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.GameServer.MessageHandler;
using MUnique.OpenMU.GameServer.RemoteView.MiniGames;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The discovery of the plugins as on the start of the server (09.10.2026: a server without the dialog of the warden
/// and with an abstract packet handler couldn't take a connection).
/// </summary>
[TestFixture]
public class PlugInDiscoveryTest
{
    /// <summary>
    /// All plugins load, the view of the Illusion of Noria is known and no packet handler is abstract.
    /// </summary>
    [Test]
    public void AllPlugInsAreConcrete()
    {
        try
        {
            _ = typeof(IllusionOfNoriaViewPlugIn).Assembly.DefinedTypes.ToList();
        }
        catch (System.Reflection.ReflectionTypeLoadException ex)
        {
            Assert.Fail(string.Join(" | ", ex.LoaderExceptions.Select(e => e?.Message).Distinct()));
        }
        var manager = new PlugInManager(null, new NullLoggerFactory(), null, null);

        var known = (System.Collections.IDictionary)typeof(PlugInManager).GetField("_knownPlugIns", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(manager)!;
        var types = known.Values.Cast<Type>().ToList();
        Assert.That(types.Where(t => t.IsAbstract).Select(t => t.FullName), Is.Empty);
        Assert.That(manager.GetKnownPlugInsOf<IPacketHandlerPlugIn>().Where(t => t.IsAbstract), Is.Empty);
    }
}
