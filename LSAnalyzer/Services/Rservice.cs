using Microsoft.Win32;
using RDotNet;

namespace LSAnalyzer.Services
{
    public class Rservice
    {
        private string? rPath;
        private REngine? engine;

        public Rservice() 
        {
            var rPathObject = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\R-core\\R64", "InstallPath", null);
            if (rPathObject != null)
            {
                rPath = rPathObject.ToString()!.Replace("\\", "/");
            }
        }

        public bool Connect()
        {
            if (rPath == null)
            {
                return false;
            }

            try
            {
                engine = REngine.GetInstance();
                engine.Evaluate("Sys.setenv(PATH = paste(\"" + rPath + "/bin/x64\", Sys.getenv(\"PATH\"), sep=\";\"))"); //ugly workaround for now!
                string[] a = engine.Evaluate("paste0('Result: ', stats::sd(c(1,2,3)))").AsCharacter().ToArray();
                if (a.Length == 0 || a[0] != "Result: 1")
                {
                    return false;
                }
            } catch
            {
                return false;
            }

            return true;
        }
    }
}
