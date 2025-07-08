using System;
using WanderSpire.Scripting;

namespace ScriptHost
{
    public static class World
    {
        private const int Max = 4096;
        private static readonly uint[] _buf = new uint[Max];

        public static void ForEachEntity(Action<Entity> action)
        {
            var eng = Engine.Instance;
            if (eng is null) return;

            int n = EngineInterop.Engine_GetAllEntities(eng.Context, _buf, _buf.Length);
            for (int i = 0; i < n; i++)
                action(Entity.FromRaw(eng.Context, (int)_buf[i]));
        }
    }
}
