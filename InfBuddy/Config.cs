using System;
using System.Collections.Generic;
using System.IO;
using AOSharp.Core;
using Newtonsoft.Json;
using AOSharp.Core.UI;

namespace InfBuddy
{
    public class Config
    {
        public GlobalSettings GlobalSettings { get; set; }
        public Dictionary<int, CharacterSettings> CharSettings { get; set; }

        protected string _path;

        [JsonIgnore]
        public bool IsLeech => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].IsLeech : false;

        public static Config Load(string path)
        {
            Config config;
            try
            {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
            }
            catch
            {
                Chat.WriteLine($"No config file found.");
                Chat.WriteLine($"Using default settings");
                config = new Config
                {
                    GlobalSettings = new GlobalSettings() {
                        MissionDifficulty = MissionDifficulty.Easy,
                        MissionFaction = MissionFaction.Neut
                    },
                    CharSettings = new Dictionary<int, CharacterSettings>()
                };
            }

            config._path = path;

            return config;
        }

        public void Save()
        {
            if(!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp");

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\InfBuddy"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\Infbuddy");

            File.WriteAllText(_path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }

    public class GlobalSettings
    {
        public MissionDifficulty MissionDifficulty { get; set; }
        public MissionFaction MissionFaction { get; set; }

        public GlobalSettings Clone()
        {
            return (GlobalSettings)this.MemberwiseClone();
        }

        public string GetMissionName()
        {
            switch (MissionDifficulty)
            {
                case MissionDifficulty.Easy:
                    if(MissionFaction == MissionFaction.Neut)
                        return "The Purification Ritual - Easy";
                    else
                        return "The Purification Ritual - Ea...";
                case MissionDifficulty.Medium:
                    return "The Purification Ritual - Me...";
                case MissionDifficulty.Hard:
                    return "The Purification Ritual - Ha...";
                default:
                    return "Unknown";
            }
        }
    }

    public class CharacterSettings
    {
        public bool IsLeech { get; set; }
    }

    public enum MissionDifficulty
    {
        Easy = 1,
        Medium = 2,
        Hard = 3
    }

    public enum MissionFaction
    {
        Neut = 1,
        Omni = 2,
        Clan = 3
    }
}
