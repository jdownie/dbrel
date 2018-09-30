using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Classes;

namespace Classes {

  public class DBRelLib {

    public static void Error(string err) {
      Console.WriteLine(err);
      Environment.Exit(1);
    }

    public static void Config(string dir, string tgt, string cs, string driver) {
      string configScript = string.Format("{0}/config/{1}.sql", dir, tgt);
      if (!File.Exists(configScript)) {
        Console.WriteLine(string.Format("{0} does not exist.", configScript));
      } else {
        dbconn db = new dbconn(cs, driver);
        using (StreamReader sr = new StreamReader(configScript)) {
          string sql = sr.ReadToEnd();
          bool result = db.exec(sql);
          Console.WriteLine(string.Format("Applying {0}... {1}", configScript, ( result ? "Success" : "Fail" )));
        }
      }
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
        subdirs.Add("config");
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
      string ret = null;
      if (type == "index") {
        parts = new List<string>(objectname.Split("."));
        string tablename = parts[0];
        string indexname = parts[1];
        ret = string.Format(@"
if exists ( select 1
            from sys.indexes 
            where object_id = OBJECT_ID('{0}')
              and name='{1}')
  drop index {0}.{1}
        ", tablename, indexname);
      } else {
        if (type == "procedure") { type_clause = string.Format("( N'P', N'PC' )"); }
        if (type == "function") { type_clause = string.Format("( N'FN', N'TF' )"); }
        if (type == "trigger") { type_clause = string.Format("( N'TR' )"); }
        if (type == "view") { type_clause = string.Format("( N'V' )"); }
        ret = string.Format(@"
if exists ( select 1
            from sys.objects
            where object_id = OBJECT_ID(N'{0}')
              and type in {1}
          ) 
  drop {2} {0};
", objectname, type_clause, type);
      }
      return ret;
    }

    public static void SchemaInit(string cs, string driver) {
      string sql = "select count(*) as c from sysobjects where type = 'U' and name = '_dbrel';";
      dbconn db = new dbconn(cs, driver);
      List<Dictionary<string, object>> rows = db.rows(sql);
      int c = (int)rows[0]["c"];
      if (c == 0) {
        sql = "create table _dbrel (id int not null, primary key (id));";
        db.exec(sql);
      }
    }

    public static Dictionary<int, Dictionary<string, string>> SchemaQueue(string root) {
      Dictionary<int, Dictionary<string, string>> ret = new Dictionary<int, Dictionary<string, string>>();
      int i = 1;
      bool loop = true;
      while (loop) {
        bool file_match = false;
        for (int l = 0; l <= 4; l++) {
          string prefix = new string('0', l);
          string filepath = string.Format("{0}/schema/{1}{2}.sql", root, prefix, i);
          if (File.Exists(filepath)) {
            ret[i] = new Dictionary<string, string>();
            ret[i]["filepath"] = filepath;
            file_match = true;
          }
        }
        if (!file_match) {
          loop = false;
        } else {
          i++;
        }
      }
      return ret;
    }

  }

}
