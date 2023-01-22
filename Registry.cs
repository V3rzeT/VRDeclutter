using System;
using System.Reflection;
using Microsoft.Win32;

namespace VRDeclutter
{
    public class Registry
    {
        // Set main executable to run at startup
        public static bool SetStartup()
        {
            try
            {
                Assembly curAssembly = Assembly.GetExecutingAssembly();
                RegistryKey? rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (rk != null)
                {
                    if (rk.GetValue(curAssembly.GetName().Name, null) == null)
                        rk.SetValue(curAssembly.GetName().Name, System.Windows.Forms.Application.ExecutablePath);
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch
            {
            }

            return false;
        }

        // Remove main executable from run at startup
        public static bool RemoveStartup()
        {
            try
            {
                Assembly curAssembly = Assembly.GetExecutingAssembly();
                RegistryKey? rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (rk != null)
                    rk.DeleteValue(curAssembly.GetName().Name, false);
                else
                    return false;

                return true;
            }
            catch
            {
            }

            return false;
        }
    }
}