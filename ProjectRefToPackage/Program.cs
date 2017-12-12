using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectRefToPackage
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i].ToLower())
                {
                    case "-project":
                        {
                            string prjFile = args[++i];

                            ProjectMigrator mgrt = new ProjectMigrator(prjFile);
                            mgrt.MigrateProjectReferences();
                        }
                        break;

                    case "-folder":
                        {
                            string folder = args[++i];
                            string[] allProjects = Directory.GetFiles(folder, "*.*proj", SearchOption.AllDirectories);
                            foreach (string prjFile in allProjects)
                            {
                                ProjectMigrator mgrt = new ProjectMigrator(prjFile);
                                mgrt.MigrateProjectReferences();
                            }
                        }
                        break;
                }
            }
        }
    }
}
