// <copyright file="LearnedSkillsWithoutLevelRequirementPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Skills which a character has already learned can be used regardless of the level requirement of the skill,
/// e.g. after a reset to level 1. Other requirements (e.g. energy) still apply, and learning a skill still
/// requires the level of the scroll or orb. When this plugin is deactivated, the level requirement applies as usual.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.LearnedSkillsWithoutLevelRequirementPlugIn_Name), Description = nameof(PlugInResources.LearnedSkillsWithoutLevelRequirementPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("7C8217EF-9810-4E54-BCAC-FD0A677F9DD9")]
public class LearnedSkillsWithoutLevelRequirementPlugIn : IFeaturePlugIn
{
}
