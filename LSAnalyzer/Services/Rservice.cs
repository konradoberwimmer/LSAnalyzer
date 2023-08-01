using Microsoft.Win32;
using RDotNet;
using System.Linq;

namespace LSAnalyzer.Services
{
    public class Rservice
    {
        private string? _rPath;
        private REngine? _engine;
        private readonly string[] _rPackages = new string[] { "BIFIEsurvey", "foreign" };

        public Rservice() 
        {
            var rPathObject = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\R-core\\R64", "InstallPath", null);
            if (rPathObject != null)
            {
                _rPath = rPathObject.ToString()!.Replace("\\", "/");
            }
        }

        public bool Connect()
        {
            if (_rPath == null)
            {
                return false;
            }

            try
            {
                _engine = REngine.GetInstance();
                _engine.Evaluate("Sys.setenv(PATH = paste(\"" + _rPath + "/bin/x64\", Sys.getenv(\"PATH\"), sep=\";\"))"); //ugly workaround for now!
                string[] a = _engine.Evaluate("paste0('Result: ', stats::sd(c(1,2,3)))").AsCharacter().ToArray();
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

        public bool CheckNecessaryRPackages()
        {
            if (_engine == null) 
            { 
                return false;
            }

            foreach (string rPackage in _rPackages)
            {
                bool available = _engine.Evaluate("nzchar(system.file(package='" + rPackage + "'))").AsLogical().First();
                if (!available)
                {
                    return false;
                }
            }

            return true;
        }

        public bool InstallNecessaryRPackages()
        {
            if (_engine == null)
            {
                return false;
            }

            bool userLibraryFolderConfigured = _engine.Evaluate("nzchar(Sys.getenv('R_LIBS_USER'))").AsLogical().First();
            if (!userLibraryFolderConfigured) 
            {
                return false;
            }

            try
            {
                _engine.Evaluate("if (!dir.exists(Sys.getenv('R_LIBS_USER'))) { dir.create(Sys.getenv('R_LIBS_USER')) }");
            } catch
            {
                return false;
            }

            foreach (string rPackage in _rPackages)
            {
                bool available = _engine.Evaluate("nzchar(system.file(package='" + rPackage + "'))").AsLogical().First();
                if (!available)
                {
                    try
                    {
                        _engine.Evaluate("install.packages('" + rPackage + "', lib = Sys.getenv('R_LIBS_USER'), repos = 'https://cloud.r-project.org')");
                    } catch
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
