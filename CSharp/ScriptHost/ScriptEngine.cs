using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using ScriptHost;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WanderSpire.Scripting;
using WanderSpire.Scripting.Content;

public sealed class ScriptEngine : IDisposable
{
    public static ScriptEngine? Current { get; private set; }

    public delegate WanderSpire.ScriptHost.Behaviour? BehaviourFactoryDelegate(string behaviourName);
    public BehaviourFactoryDelegate? BehaviourFactory { get; set; }

    private readonly IntPtr _ctx;
    private readonly List<ScriptRunner<object>> _runners = new();
    private readonly List<ITickReceiver> _systems = new();
    private readonly object _sysLock = new();

    private FileSystemWatcher? _scriptsWatcher, _contentWatcher;
    private Globals _globals;

    public List<IQuestDefinition> Quests { get; private set; } = new();
    public List<IEncounterDefinition> Encounters { get; private set; } = new();

    public ScriptEngine(string scriptsDir, IntPtr ctx)
    {
        if (Current != null)
            throw new InvalidOperationException("Only one ScriptEngine may be active.");
        Current = this;
        _ctx = ctx;

        TickManager.Instance.Initialize(ctx);
        TickManager.Instance.Tick += OnTick;

        DiscoverStaticSystems();
        SetupCsxWatcher(scriptsDir);

        _contentWatcher = new FileSystemWatcher(ContentPaths.Root, "*.csx")
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        _contentWatcher.Changed += (_, __) => ReloadContent();

        _globals = new Globals { Engine = Engine.Instance!, Dt = 0f, Ticks = 0 };

        CompileAllScripts();
        ReloadContent();

        BindEntityScripts();

        Console.WriteLine($"[ScriptEngine] systems={_systems.Count}, scripts={_runners.Count}, quests={Quests.Count}, encounters={Encounters.Count}");
    }

    private void OnTick(float dt, ulong tickIndex)
    {
        _globals.Dt = dt;
        _globals.Ticks = (int)tickIndex;

        ITickReceiver[] snapshot;
        lock (_sysLock)
            snapshot = _systems.ToArray();

        foreach (var sys in snapshot)
            try { sys.OnTick(dt); }
            catch (Exception e) { Console.Error.WriteLine($"[TickReceiver] {e}"); }

        foreach (var run in _runners)
            try { run(_globals).Wait(); }
            catch (Exception e) { Console.Error.WriteLine($"[ScriptRunner] {e}"); }
    }

    public void RegisterSystem(ITickReceiver sys)
    {
        lock (_sysLock)
        {
            bool isBehaviour = sys is WanderSpire.ScriptHost.Behaviour;
            if (!isBehaviour &&
                _systems.Any(s => s.GetType() == sys.GetType() &&
                                  !(s is WanderSpire.ScriptHost.Behaviour)))
                return;

            _systems.Add(sys);
        }
    }

    public void PurgeBehaviours()
    {
        lock (_sysLock)
        {
            for (int i = _systems.Count - 1; i >= 0; i--)
                if (_systems[i] is WanderSpire.ScriptHost.Behaviour beh)
                {
                    if (beh is IDisposable d)
                        d.Dispose();
                    _systems.RemoveAt(i);
                }
        }
    }

    public void BindEntityScripts()
    {
        if (BehaviourFactory is null) return;

        ScriptHost.World.ForEachEntity(ent =>
        {
            var sc = ent.GetScriptData<ScriptsComponent>("ScriptsComponent");
            string[] scripts = sc?.Scripts
                              ?? ent.GetScriptData<string[]>("scripts")
                              ?? Array.Empty<string>();

            if (scripts.Length == 0) return;

            foreach (var name in scripts)
            {
                lock (_sysLock)
                {
                    if (_systems.Any(s =>
                          s is WanderSpire.ScriptHost.Behaviour b &&
                          b.Entity == ent &&
                          (b.GetType().FullName == name || b.GetType().Name == name)))
                        continue;
                }

                var beh = BehaviourFactory.Invoke(name);
                if (beh == null)
                {
                    Console.Error.WriteLine($"[ScriptEngine] Behaviour '{name}' unresolved for entity {ent.Uuid:X16}");
                    continue;
                }

                beh._Attach(ent);
                RegisterSystem(beh);
                Console.WriteLine($"[ScriptEngine] +{beh.GetType().Name} → entity {ent.Uuid:X16}");
            }
        });
    }

    /* ====================================================================
       Internals – static systems discovery, CS-script pipeline …
    ==================================================================== */
    private void DiscoverStaticSystems()
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var t in asm.GetTypes())
                if (!t.IsAbstract &&
                    typeof(ITickReceiver).IsAssignableFrom(t) &&
                    !typeof(WanderSpire.ScriptHost.Behaviour).IsAssignableFrom(t))
                {
                    if (Activator.CreateInstance(t) is ITickReceiver inst)
                        RegisterSystem(inst);
                }
    }

    private void SetupCsxWatcher(string dir)
    {
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        _scriptsWatcher = new FileSystemWatcher(dir, "*.csx")
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        _scriptsWatcher.Changed += (_, __) => CompileAllScripts();
    }

    private void ReloadContent()
    {
        Quests = ContentLoader.LoadQuests();
        Encounters = ContentLoader.LoadEncounters();
    }

    private void CompileAllScripts()
    {
        _runners.Clear();
        foreach (var csx in Directory.EnumerateFiles(
                     AppContext.BaseDirectory, "*.csx", SearchOption.AllDirectories))
        {
            try
            {
                var script = CSharpScript.Create(
                    File.ReadAllText(csx),
                    ScriptOptions.Default
                        .AddReferences(typeof(Globals).Assembly)
                        .AddImports("System", "WanderSpire.Scripting"),
                    typeof(Globals)
                );
                script.Compile();
                _runners.Add(script.CreateDelegate());
            }
            catch
            {
                /* ignore individual compile errors – keep going */
            }
        }
    }

    public void Dispose()
    {
        _scriptsWatcher?.Dispose();
        _contentWatcher?.Dispose();
        TickManager.Instance.Tick -= OnTick;

        // Tear down *all* systems that require cleanup (e.g. HealthBarRenderSystem)
        lock (_sysLock)
        {
            foreach (var sys in _systems)
                if (sys is IDisposable d)
                    d.Dispose();
            _systems.Clear();
        }

        Current = null;
    }

    public sealed class Globals
    {
        public required WanderSpire.Scripting.Engine Engine;
        public float Dt;
        public int Ticks;
    }
}
