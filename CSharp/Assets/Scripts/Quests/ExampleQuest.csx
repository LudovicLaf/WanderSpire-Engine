using System;
using WanderSpire.Runtime.Content;

public class ExampleQuest : IQuestDefinition
{
    public string Id => "orc_hunt";
    public string Title => "Hunt the Orc Packs";

    public void OnStart() => Console.WriteLine("[Quest] Started: " + Id);
    public void OnTick(float dt)
    {
        // per-frame quest logic...
    }
}

return new ExampleQuest();