using robotManager.Helpful;
using System;
using System.ComponentModel;
using System.IO;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class WholesomeDetectorSettings : Settings
{
    private static readonly string _productName = "Wholesome_Detector";
    private static string GetSettingsPath => AdviserFilePathAndName(_productName, ObjectManager.Me.Name + "." + Usefuls.RealmName);
    public static WholesomeDetectorSettings CurrentSettings { get; set; }

    // Important
    [DefaultValue(15)]
    [Category("Detector")]
    [DisplayName("Detection radius")]
    [Description("Detection radius")]
    public int DetectionRadius { get; set; }

    [DefaultValue(true)]
    [Category("Detector settings")]
    [DisplayName("DynamicObjects")]
    [Description("Detect dynamic objects")]
    public bool DetectDynamicObjects { get; set; }

    [DefaultValue(true)]
    [Category("Detector settings")]
    [DisplayName("Objects")]
    [Description("Detect objects")]
    public bool DetectObjects { get; set; }

    [DefaultValue(true)]
    [Category("Detector settings")]
    [DisplayName("GameObjects")]
    [Description("Detect GameObjects")]
    public bool DetectGameObjects { get; set; }

    [DefaultValue(true)]
    [Category("Detector settings")]
    [DisplayName("Untyped")]
    [Description("Detect Untyped")]
    public bool DetectUntyped { get; set; }

    [DefaultValue(true)]
    [Category("Detector settings")]
    [DisplayName("Units")]
    [Description("Detect Units")]
    public bool DetectUnits { get; set; }

    // Misc
    [DefaultValue(false)]
    [Category("Misc")]
    [DisplayName("AiGroups")]
    [Description("Detect AiGroups")]
    public bool DetectAiGroups { get; set; }

    [DefaultValue(false)]
    [Category("Misc")]
    [DisplayName("AreaTriggers")]
    [Description("Detect AreaTriggers")]
    public bool DetectAreaTriggers { get; set; }

    [DefaultValue(false)]
    [Category("Misc")]
    [DisplayName("Containers")]
    [Description("Detect Containers")]
    public bool DetectContainers { get; set; }

    [DefaultValue(false)]
    [Category("Misc")]
    [DisplayName("Corpses")]
    [Description("Detect Corpses")]
    public bool DetectCorpses { get; set; }

    [DefaultValue(false)]
    [Category("Misc")]
    [DisplayName("Items")]
    [Description("Detect Items")]
    public bool DetectItems { get; set; }

    [DefaultValue(false)]
    [Category("Misc")]
    [DisplayName("Players")]
    [Description("Detect Players")]
    public bool DetectPlayers { get; set; }

    public WholesomeDetectorSettings()
    {
        DetectionRadius = 15;

        // Important
        DetectDynamicObjects = true;
        DetectObjects = true;
        DetectGameObjects = true;
        DetectUntyped = true;
        DetectUnits = true;

        // Misc
        DetectAiGroups = false;
        DetectAreaTriggers = false;
        DetectContainers = false;
        DetectCorpses = false;
        DetectItems = false;
        DetectPlayers = false;
    }

    public bool Save()
    {
        try
        {
            return Save(GetSettingsPath);
        }
        catch (Exception ex)
        {
            Logging.WriteError($"{_productName} > Save(): " + ex);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(GetSettingsPath))
            {
                CurrentSettings = Load<WholesomeDetectorSettings>(GetSettingsPath);
                return true;
            }
            CurrentSettings = new WholesomeDetectorSettings();
        }
        catch (Exception ex)
        {
            Logging.WriteError($"{_productName} > Load(): " + ex);
        }
        return false;
    }
}
