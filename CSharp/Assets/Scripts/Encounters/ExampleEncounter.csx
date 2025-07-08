using System;
using WanderSpire.Scripting.Content;

var enc = EncounterManager.Definitions.FirstOrDefault(e => e.Id == "tower_ambush");
if (enc is not null)
{
    Console.WriteLine($"Encounter loaded: {enc.Id}");
    // spawn logic...
}
