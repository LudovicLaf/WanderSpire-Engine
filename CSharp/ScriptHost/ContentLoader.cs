// ScriptHost/ContentLoader.cs
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using WanderSpire.Scripting.Content;
using static ScriptEngine;

namespace WanderSpire.Scripting
{
    public static class ContentLoader
    {
        private static readonly ScriptOptions _opts = ScriptOptions.Default
            .AddReferences(typeof(IQuestDefinition).Assembly)
            .AddImports("System", "WanderSpire.Scripting", "WanderSpire.Scripting.Content");

        public static List<IQuestDefinition> LoadQuests() => LoadScripts<IQuestDefinition>("Quests");
        public static List<IEncounterDefinition> LoadEncounters() => LoadScripts<IEncounterDefinition>("Encounters");

        private static List<T> LoadScripts<T>(string subDir)
        {
            var folder = Path.Combine(ContentPaths.Root, subDir);
            if (!Directory.Exists(folder)) return new();

            var list = new List<T>();
            foreach (var file in Directory.EnumerateFiles(folder, "*.csx", SearchOption.AllDirectories))
            {
                try
                {
                    var script = CSharpScript.Create<object>(
                        File.ReadAllText(file),
                        _opts,
                        typeof(Globals)
                    );
                    var runner = script.CreateDelegate();
                    var ret = runner(new Globals { Engine = Engine.Instance!, Dt = 0f, Ticks = 0 }).Result;
                    if (ret is T t) list.Add(t);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ContentLoader] Failed to load {file}: {ex.Message}");
                }
            }
            return list;
        }
    }
}
