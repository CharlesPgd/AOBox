using System;
using System.Collections.Generic;
using System.IO;
using AOSharp.Core;
using Newtonsoft.Json;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;

namespace APFBuddy
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
                    GlobalSettings = new GlobalSettings() { Sector = APFSector.S13 },
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
        public APFSector Sector { get; set; }

        public GlobalSettings Clone()
        {
            return (GlobalSettings)this.MemberwiseClone();
        }

        public int GetSectorId()
        {
            switch (Sector)
            {
                case APFSector.S13:
                    return Constants.S13Id;
                case APFSector.S28:
                    return Constants.S28Id;
                case APFSector.S35:
                    return Constants.S35Id;
                default:
                    throw new Exception("Not possible pls stop break");
            }
        }

        public Vector3 GetSectorEntrancePos()
        {
            switch (Sector)
            {
                case APFSector.S13:
                    return Constants.S13EntrancePos;
                case APFSector.S28:
                    return Constants.S28EntrancePos;
                case APFSector.S35:
                    return Constants.S35EntrancePos;
                default:
                    throw new Exception("Not possible pls stop break");
            }
        }

        public Vector3 GetSectorExitPos()
        {
            switch (Sector)
            {
                case APFSector.S13:
                    return Constants.S13ExitPos;
                case APFSector.S35:
                    return Constants.S35ExitPos;
                default:
                    throw new Exception("Not possible pls stop break");
            }
        }
    }

    public class CharacterSettings
    {
        public bool IsLeech { get; set; }
    }
    public enum APFSector
    {
        S13 = 13,
        S28 = 28,
        S35 = 35
    }
}
