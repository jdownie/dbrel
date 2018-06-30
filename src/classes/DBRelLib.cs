using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Classes {

  public class DBRelLib {

    public static void Error(string err) {
      Console.WriteLine(err);
      Environment.Exit(1);
    }

    public static void Init(string dir) {
      if (System.IO.Directory.Exists(dir)) {
        List<string> subdirs = new List<string>();
        subdirs.Add("schema");
        subdirs.Add("view");
        subdirs.Add("procedure");
        subdirs.Add("function");
        subdirs.Add("trigger");
        subdirs.Add("index");
        string path = null;
        path = Path.Combine(dir, ".dbrel");
        if (!File.Exists(path)) {
          File.Create(path);
          Console.WriteLine(string.Format("Creating {0}", path));
        } else {
          Console.WriteLine(string.Format("{0} already exists", path));
        }
        foreach (string subdir in subdirs) {
          path = Path.Combine(dir, subdir);
          if (!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
            Console.WriteLine(string.Format("Creating {0}", path));
          } else {
            Console.WriteLine(string.Format("{0} already exists", path));
          }
        }
      } else {
        string err = string.Format("Nominated path ({0}) is not a directory.", dir);
        Console.WriteLine(err);
      }
    }

    public static Dictionary<string, Dictionary<string, string>> Cfg(string path) {
      Dictionary<string, Dictionary<string, string>> ret = new Dictionary<string, Dictionary<string, string>>();
      path = Path.Combine(path, ".dbrel");
      if (File.Exists(path)) {
        using (StreamReader file = File.OpenText(path)) {
          JsonSerializer s = new JsonSerializer();
          ret = (Dictionary<string, Dictionary<string, string>>)s.Deserialize(file, typeof(Dictionary<string, Dictionary<string, string>>));
        }
      }
      return ret;
    }

    public static string FindRoot(string path) {
      string ret = null;
      List<string> parts = new List<string>(Path.GetFullPath(path).Split(Path.DirectorySeparatorChar));
      while (parts.Count > 0) {
        string test = string.Join("/", parts);
        if (Directory.Exists(test) && File.Exists(string.Format("{0}{1}.dbrel", test, Path.DirectorySeparatorChar))) {
          ret = test;
        }
        parts.RemoveAt(parts.Count -1);
      }
      return ret;
    }

    public static string DropStatement(string script) {
      List<string> parts = new List<string>(script.Split(Path.DirectorySeparatorChar));
      string filename = parts[parts.Count - 1];
      string type = parts[parts.Count - 2];
      parts = new List<string>(filename.Split("."));
      parts.RemoveAt(parts.Count - 1);
      string objectname = string.Join(".", parts);
      string type_clause = null;
      if (type == "procedure") {
        type_clause = string.Format("( N'P', N'PC' )");
      }
      string ret = string.Format(@"
if exists ( select 1
            from sys.objects
            where object_id = OBJECT_ID(N'{0}')
              and type in {1}
          ) 
  drop {2} {0};
", objectname, type_clause, type);
      return ret;
    }

  }

}