// <copyright file="Plugins.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Pages;

using Microsoft.AspNetCore.Components;
using MUnique.OpenMU.Web.Shared.Services;

/// <summary>
/// Code-behind for the <see cref="Plugins"/> page.
/// </summary>
public partial class Plugins
{
    /// <summary>
    /// Gets or sets the plug-in identifier.
    /// </summary>
    [SupplyParameterFromQuery(Name = "id")]
    public string? PlugInId { get; set; }

    /// <summary>
    /// Gets or sets the name filter, kept in the address (?name=).
    /// </summary>
    [SupplyParameterFromQuery(Name = "name")]
    public string? NameQuery { get; set; }

    /// <summary>
    /// Gets or sets the type filter, kept in the address (?type=).
    /// </summary>
    [SupplyParameterFromQuery(Name = "type")]
    public string? TypeQuery { get; set; }

    /// <summary>
    /// Gets or sets the extension point filter, kept in the address (?point=).
    /// </summary>
    [SupplyParameterFromQuery(Name = "point")]
    public string? PointQuery { get; set; }

    /// <summary>
    /// Gets or sets the status filter ("active", "inactive"), kept in the address (?status=).
    /// </summary>
    [SupplyParameterFromQuery(Name = "status")]
    public string? StatusQuery { get; set; }

    [Inject]
    private PlugInController PlugInController { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    private HashSet<Guid> Expanded { get; } = new();

    private string NameFilter
    {
        get => this.PlugInController.NameFilter;
        set
        {
            this.PlugInController.NameFilter = value ?? string.Empty;
            this.UpdateAddress();
        }
    }

    private string TypeFilter
    {
        get => this.PlugInController.TypeFilter;
        set
        {
            this.PlugInController.TypeFilter = value ?? string.Empty;
            this.UpdateAddress();
        }
    }

    private string StatusFilter
    {
        get => this.PlugInController.ActiveFilter switch { true => "active", false => "inactive", _ => string.Empty };
        set
        {
            this.PlugInController.ActiveFilter = value switch { "active" => true, "inactive" => false, _ => null };
            this.UpdateAddress();
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync().ConfigureAwait(true);

        // the filters come from the address: going back to this page restores them
        if (this.PlugInId is null)
        {
            SetIfChanged(this.PlugInController.NameFilter, this.NameQuery ?? string.Empty, v => this.PlugInController.NameFilter = v);
            SetIfChanged(this.PlugInController.TypeFilter, this.TypeQuery ?? string.Empty, v => this.PlugInController.TypeFilter = v);
            var point = Guid.TryParse(this.PointQuery, out var parsedPoint) ? parsedPoint : Guid.Empty;
            if (this.PlugInController.PointFilter != point)
            {
                this.PlugInController.PointFilter = point;
            }

            bool? active = this.StatusQuery switch { "active" => true, "inactive" => false, _ => null };
            if (this.PlugInController.ActiveFilter != active)
            {
                this.PlugInController.ActiveFilter = active;
            }
        }

        if (Guid.TryParse(this.PlugInId, out var id))
        {
            var plugin = await this.PlugInController.GetByIdAsync(id).ConfigureAwait(true);

            if (plugin is { })
            {
                this.PlugInController.NameFilter = plugin.PlugInName ?? string.Empty;
                this.PlugInController.TypeFilter = string.Empty;
                this.PlugInController.PointFilter = Guid.Empty;

                if (plugin.ConfigurationType is { })
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(100).ConfigureAwait(false);
                        await this.InvokeAsync(() => this.PlugInController.ShowPlugInConfigAsync(plugin)).ConfigureAwait(false);
                    });
                }
            }

            this.PlugInId = null;
        }
    }

    private static void SetIfChanged(string current, string value, Action<string> set)
    {
        if (!string.Equals(current, value, StringComparison.Ordinal))
        {
            set(value);
        }
    }

    private void OnPlugInPointSelected(ChangeEventArgs args)
    {
        if (args.Value is string guidString && Guid.TryParse(guidString, out var result))
        {
            this.PlugInController.PointFilter = result;
            this.UpdateAddress();
        }
    }

    private bool IsExpanded(MUnique.OpenMU.Web.Shared.Models.PlugInConfigurationViewItem item) => this.Expanded.Contains(item.Id);

    private void ToggleInfo(MUnique.OpenMU.Web.Shared.Models.PlugInConfigurationViewItem item)
    {
        if (!this.Expanded.Remove(item.Id))
        {
            this.Expanded.Add(item.Id);
        }
    }

    /// <summary>
    /// Writes the filters into the address without a new history entry.
    /// </summary>
    private void UpdateAddress()
    {
        var point = this.PlugInController.PointFilter;
        var uri = this.NavigationManager.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            ["name"] = string.IsNullOrWhiteSpace(this.PlugInController.NameFilter) ? null : this.PlugInController.NameFilter,
            ["type"] = string.IsNullOrWhiteSpace(this.PlugInController.TypeFilter) ? null : this.PlugInController.TypeFilter,
            ["point"] = point == Guid.Empty ? null : point.ToString(),
            ["status"] = string.IsNullOrEmpty(this.StatusFilter) ? null : this.StatusFilter,
            ["id"] = null,
        });
        this.NavigationManager.NavigateTo(uri, replace: true);
    }
}
