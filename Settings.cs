using System.IO;
using Newtonsoft.Json;

namespace VRDeclutter
{
    public class Settings
    {
        public enum ActionSetting
        {
            Nothing,
            AutoHide,
            MinimizeHide
        }

        public bool RunOnStartup { get; set; } = false;
        public bool DockToTray { get; set; } = false;
        public bool CloseAllWithSteamVR { get; set; } = false;
        public ActionSetting SteamVrActionSetting { get; set; } = ActionSetting.Nothing;
        public ActionSetting WmrPortalActionSetting { get; set; } = ActionSetting.Nothing;
        public ActionSetting OculusActionSetting { get; set; } = ActionSetting.Nothing;
        public ActionSetting SlimeVrActionSetting { get; set; } = ActionSetting.Nothing;
        public ActionSetting K2VrActionSetting { get; set; } = ActionSetting.Nothing;
        public ActionSetting OvrToolkitActionSetting { get; set; } = ActionSetting.Nothing;

        public static bool ReadSettings(string filePath, out Settings settings)
        {
            settings = null;

            if (File.Exists(filePath))
                try
                {
                    settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(filePath));
                    return true;
                }
                catch
                {
                }

            return false;
        }

        public static bool WriteSettings(string filePath, Settings settings)
        {
            try
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(settings, Formatting.Indented));
                return true;
            }
            catch
            {
            }

            return false;
        }
    }
}