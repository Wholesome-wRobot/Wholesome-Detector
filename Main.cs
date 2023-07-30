using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using wManager.Events;
using wManager.Plugin;
using wManager.Wow;
using wManager.Wow.Class;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using wManager.Wow.Patchables;

public class Main : IPlugin
{
    private static readonly Random Random = new Random();

    private readonly List<DetectedObject> _detectedObjects = new List<DetectedObject>();
    private readonly List<ulong> _alreadyDetectedObjects = new List<ulong>();
    private readonly List<int> _alreadyDetectedSpells = new List<int>();
    private readonly List<uint> _alreadyDetectedBuffs = new List<uint>();

    public void Initialize()
    {
        WholesomeDetectorSettings.Load();
        Logging.Write("Wholesome Detector initialized");
        Radar3D.OnDrawEvent += DrawRadar;
        ObjectManagerEvents.OnObjectManagerPulsed += OnObjectManagerPulse;
        EventsLuaWithArgs.OnEventsLuaStringWithArgs += EventsWithArgsHandler;
        Radar3D.Pulse();
    }

    public void Dispose()
    {
        Logging.Write("Wholesome Detector disposed");
        Radar3D.OnDrawEvent -= DrawRadar;
        ObjectManagerEvents.OnObjectManagerPulsed -= OnObjectManagerPulse;
        EventsLuaWithArgs.OnEventsLuaStringWithArgs -= EventsWithArgsHandler;
    }

    // LUA events
    private void EventsWithArgsHandler(string id, List<string> args)
    {
        switch (id)
        {
            case "COMBAT_LOG_EVENT_UNFILTERED":
                string logType = args[1];

                if (logType == "SPELL_CAST_SUCCESS"
                    || logType == "SPELL_CAST_START")
                {
                    //string timeStamp = args[0];
                    //string sourceGUID = args[2];
                    int spellId = int.Parse(args[8]);

                    if (!_alreadyDetectedSpells.Contains(spellId))
                    {
                        _alreadyDetectedSpells.Add(spellId);
                        string caster = args[3];
                        string spellName = args[9];
                        LogSpell($"{caster} cast {spellName} ({spellId})");
                        LogSpell($"https://www.wowhead.com/wotlk/spell={spellId}");
                    }
                }
                break;
        }
    }

    // Object Manager
    private void OnObjectManagerPulse()
    {
        // Objects detection
        List<WoWObject> objectList = ObjectManager.ObjectList.ToList();
        _detectedObjects.RemoveAll(doj => doj.Position.DistanceTo(ObjectManager.Me.Position) > WholesomeDetectorSettings.CurrentSettings.DetectionRadius);
        foreach (WoWObject wowObject in objectList.Where(doj => doj.Position.DistanceTo(ObjectManager.Me.Position) < WholesomeDetectorSettings.CurrentSettings.DetectionRadius))
        {
            if (wowObject is WoWUnit || wowObject is WoWPlayer)
            {
                uint unitBaseAddress = wowObject.GetBaseAddress;
                foreach (Aura aura in BuffManager.GetAuras(unitBaseAddress))
                {
                    if (!_alreadyDetectedBuffs.Contains(aura.SpellId))
                    {
                        _alreadyDetectedBuffs.Add(aura.SpellId);
                        LogBuff($"{wowObject.Name} has buff {aura.GetSpell.Name} ({aura.SpellId})");
                    }
                }
            }

            if (wowObject.Type == WoWObjectType.Object && !WholesomeDetectorSettings.CurrentSettings.DetectObjects) continue;
            if (wowObject.Type == WoWObjectType.Item && !WholesomeDetectorSettings.CurrentSettings.DetectItems) continue;
            if (wowObject.Type == WoWObjectType.GameObject && !WholesomeDetectorSettings.CurrentSettings.DetectGameObjects) continue;
            if (wowObject.Type == WoWObjectType.AiGroup && !WholesomeDetectorSettings.CurrentSettings.DetectAiGroups) continue;
            if (wowObject.Type == WoWObjectType.AreaTrigger && !WholesomeDetectorSettings.CurrentSettings.DetectAreaTriggers) continue;
            if (wowObject.Type == WoWObjectType.Container && !WholesomeDetectorSettings.CurrentSettings.DetectContainers) continue;
            if (wowObject.Type == WoWObjectType.Corpse && !WholesomeDetectorSettings.CurrentSettings.DetectCorpses) continue;
            if (wowObject.Type == WoWObjectType.None && !WholesomeDetectorSettings.CurrentSettings.DetectUntyped) continue;
            if (wowObject.Type == WoWObjectType.Player && !WholesomeDetectorSettings.CurrentSettings.DetectPlayers) continue;
            if (wowObject.Type == WoWObjectType.Unit && !WholesomeDetectorSettings.CurrentSettings.DetectUnits) continue;
            if (wowObject.Type == WoWObjectType.DynamicObject && !WholesomeDetectorSettings.CurrentSettings.DetectDynamicObjects) continue;

            DynamicObject dObject = wowObject.Type == WoWObjectType.DynamicObject ? new DynamicObject(wowObject.GetBaseAddress) : null;
            ulong guid = dObject != null ? dObject.Guid : wowObject.Guid;
            string name = dObject != null ? dObject.Name : wowObject.Name;
            int entry = dObject != null ? dObject.Entry : wowObject.Entry;
            Vector3 position = dObject != null ? dObject.Position : wowObject.Position;

            if (position.DistanceTo(ObjectManager.Me.Position) > WholesomeDetectorSettings.CurrentSettings.DetectionRadius) continue;

            if (!_detectedObjects.Exists(dobj => dobj.Id == entry))
            {
                Color co = Color.FromArgb((byte)Random.Next(100, 255), (byte)Random.Next(100, 255), (byte)Random.Next(100, 255));
                if (!_alreadyDetectedObjects.Contains(guid))
                {
                    LogObject($"// {name} ({wowObject.Type})");
                    LogObject($"new KnownAOE({entry}, 10f, _everyone),");
                    _alreadyDetectedObjects.Add(guid);
                }
                _detectedObjects.Add(new DetectedObject(position, wowObject.Type, co, name, guid, entry));
            }
        }
    }

    public class DetectedObject
    {
        public Vector3 Position { get; private set; }
        public WoWObjectType Type { get; private set; }
        public Color Color { get; private set; }
        public string Name { get; private set; }
        public ulong Guid { get; private set; }
        public int Id { get; private set; }

        public DetectedObject(Vector3 position, WoWObjectType type, Color color, string name, ulong guid, int id)
        {
            Position = position;
            Type = type;
            Color = color;
            Name = string.IsNullOrEmpty(name) ? "Unknown object" : name;
            Guid = guid;
            Id = id;
        }
    }

    private void DrawRadar()
    {
        Radar3D.DrawCircle(ObjectManager.Me.Position, WholesomeDetectorSettings.CurrentSettings.DetectionRadius, Color.AliceBlue, false, 20);
        int YOffset = 20;
        int nbObjects = 0;
        foreach (DetectedObject detectedObject in _detectedObjects)
        {
            nbObjects++;
            Radar3D.DrawString($"[{detectedObject.Type}] {detectedObject.Name} - {detectedObject.Id}", new Vector3(450, 50 + (nbObjects * YOffset), 0), 13, detectedObject.Color, 255, FontFamily.GenericSerif);
            Radar3D.DrawCircle(detectedObject.Position, 0.5f, detectedObject.Color, true, 200);
        }
    }

    public void Settings()
    {
        WholesomeDetectorSettings.Load();
        WholesomeDetectorSettings.CurrentSettings.ToForm();
        WholesomeDetectorSettings.CurrentSettings.Save();
    }

    private class DynamicObject : WoWObject
    {
        public DynamicObject(uint address) : base(address) { }

        public override Vector3 Position =>
            new Vector3(Memory.WowMemory.Memory.ReadFloat(BaseAddress + 0xE8),
                Memory.WowMemory.Memory.ReadFloat(BaseAddress + 0xEC),
                Memory.WowMemory.Memory.ReadFloat(BaseAddress + 0xF0));
        public override string Name => new Spell(SpellID).Name;
        public override float GetDistance => Position.DistanceTo(ObjectManager.Me.PositionWithoutType);
        public ulong Caster =>
            Memory.WowMemory.Memory.ReadUInt64(GetDescriptorAddress((uint)Descriptors.DynamicObjectFields.Caster));
        public int SpellID =>
            Memory.WowMemory.Memory.ReadInt32(GetDescriptorAddress((uint)Descriptors.DynamicObjectFields.SpellID));
        public float Radius =>
            Memory.WowMemory.Memory.ReadFloat(GetDescriptorAddress((uint)Descriptors.DynamicObjectFields.Radius));
        public int CastTime =>
            Memory.WowMemory.Memory.ReadInt32(GetDescriptorAddress((uint)Descriptors.DynamicObjectFields.CastTime));
    }

    private void LogObject(string message)
    {
        Logging.Write(message, Logging.LogType.Normal, Color.Green);
    }

    private void LogSpell(string message)
    {
        Logging.Write(message, Logging.LogType.Normal, Color.CornflowerBlue);
    }

    private void LogBuff(string message)
    {
        Logging.Write(message, Logging.LogType.Normal, Color.Orange);
    }
}
