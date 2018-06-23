using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Classes {

  public class DBTarget {
    public string conncetionString { get; set; }
    public string cfg { get; set; }
  }

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

    public Dictionary<string, DBTarget> Cfg(string path) {
      Dictionary<string, DBTarget> ret = new Dictionary<string, DBTarget>();
      path = Path.Combine(path, ".dbrel");
      if (!File.Exists(path)) {
        using (StreamReader file = File.OpenText(path)) {
          JsonSerializer s = new JsonSerializer();
          ret = (Dictionary<string, DBTarget>)s.Deserialize(file, typeof(Dictionary<string, DBTarget>));
        }
      }
      return ret;
    }

  }

}