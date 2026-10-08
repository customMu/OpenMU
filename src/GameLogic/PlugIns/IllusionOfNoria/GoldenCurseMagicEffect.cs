// <copyright file="GoldenCurseMagicEffect.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.IllusionOfNoria;

using System.Timers;

/// <summary>
/// The curse of the Gilded Colossus (Illusion of Noria): takes a fixed amount of health every second while it lasts.
/// The boss renews it every second for the players near it, so it keeps burning until they leave and the time runs out.
/// </summary>
public sealed class GoldenCurseMagicEffect : MagicEffect
{
    private readonly Timer _damageTimer;
    private readonly uint _damage;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoldenCurseMagicEffect"/> class.
    /// </summary>
    /// <param name="definition">The definition of the effect.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="attacker">The boss.</param>
    /// <param name="owner">The cursed player.</param>
    /// <param name="damagePerSecond">The damage per second.</param>
    public GoldenCurseMagicEffect(MagicEffectDefinition definition, TimeSpan duration, IAttacker attacker, IAttackable owner, int damagePerSecond)
        : base(duration, definition)
    {
        this.Attacker = attacker;
        this.Owner = owner;
        this._damage = (uint)Math.Max(0, damagePerSecond);
        this._damageTimer = new Timer(1000);
        this._damageTimer.Elapsed += this.OnDamageTimerElapsed;
        this._damageTimer.Start();
    }

    /// <summary>
    /// Gets the cursed player.
    /// </summary>
    public IAttackable Owner { get; }

    /// <summary>
    /// Gets the boss.
    /// </summary>
    public IAttacker Attacker { get; }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        this._damageTimer.Stop();
        this._damageTimer.Dispose();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Catching all Exceptions.")]
    private async void OnDamageTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            if (!this.Owner.IsAlive || this.IsDisposed || this.IsDisposing || this._damage == 0)
            {
                return;
            }

            // like poison: the health directly, not the shield
            await this.Owner.ApplyPoisonDamageAsync(this.Attacker, this._damage).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            (this.Owner as ILoggerOwner)?.Logger.LogError(ex, "Error when applying the damage of the golden curse");
        }
    }
}
