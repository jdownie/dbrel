using System;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using Classes;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace dbrel {

  class Program {

    static void Main(string[] args) {
      CommandLineApplication cla = new CommandLineApplication(throwOnUnexpectedArg: false);
      cla.Name = "dbrel";
      cla.FullName = "dbrel: Database Release Utility";
      cla.HelpOption("-? | -h | --help");
      cla.VersionOption( "-v | --version", "0.0.7" );
      CommandOption init = cla.Option( "-i | --init <dir>"
                                     , "Initialise an empty folder structure."
                                     , CommandOptionType.SingleValue);
      CommandOption target = cla.Option( "-t | --target <target>"
                                       , "Target database code referenced in .dbrel"
                                       , CommandOptionType.SingleValue);
      CommandOption config = cla.Option( "-c | --config"
                                       , "Apply configuration script."
                                       , CommandOptionType.NoValue);
      CommandOption schema_action = cla.Option( "-s | --schema_action <action>"
                                              , "Database schema action"
                                              , CommandOptionType.SingleValue);
      CommandArgument file = cla.Argument( "file"
                                         , "The file to run in against the target database."
                                         , false);
      cla.OnExecute(() => {
        string pathGiven = Path.GetFullPath(".");
        if (File.Exists(file.Value)) {
          pathGiven = Path.GetFullPath(file.Value);
        }
        string root = DBRelLib.FindRoot(pathGiven);
        if (root == null) {
          Console.WriteLine(string.Format("Unable to locate .dbrel from {0}", pathGiven));
        } else {
          Dictionary<string, Dictionary<string, string>> cfg = DBRelLib.Cfg(root);
          List<string> keys = new List<string>(cfg.Keys);
          string tgt = keys[0];
          if (target.HasValue()) {
            tgt = target.Value();
          }
          if (keys.Contains(tgt)) {
            List<string> tgtKeys = new List<string>(cfg[tgt].Keys);
            // If there is a block_hosts key, interpret it...
            bool block_host = false;
            if (tgtKeys.Contains("block_hosts")) {
              List<string> block_hosts = new List<string>(cfg[tgt]["block_hosts"].Split(","));
              block_host = block_hosts.Contains(System.Net.Dns.GetHostName());
            }
            // If there is an ignore_patterns key, interpret it...
            bool ignore_file = false;
            if (tgtKeys.Contains("ignore_patterns")) {
              List<string> ignore_patterns = new List<string>(cfg[tgt]["ignore_patterns"].Split(","));
              foreach (string ignore_pattern in ignore_patterns) {
                Regex re = new Regex(ignore_pattern);
                if (file.Value != null && re.Match(file.Value).Success) {
                  ignore_file = true;
                }
              }
            }
            if (block_host) {
              Console.WriteLine(string.Format("{0} has been explicitly blocked for the target '{1}'", System.Net.Dns.GetHostName(), tgt));
            } else if (ignore_file) {
              Console.WriteLine(string.Format("Ignoring {0}. Check your .dbrel file is this is a mistake.", file.Value));
            } else {
              string cs = cfg[tgt]["connectionString"];
              if (Environment.GetEnvironmentVariable("DEBUG") != null) {
                Console.WriteLine(cs);
              }
              string driver = "mssql";
              if (cfg[tgt].ContainsKey("driver")) {
                driver = cfg[tgt]["driver"];
              }
              if (init.HasValue()) {
                DBRelLib.Init(init.Value());
              } else if (config.HasValue()) {
                DBRelLib.Config(root, tgt, cs, driver);
              } else if (schema_action.HasValue()) {
                DBRelLib.SchemaInit(cs, driver);
                Dictionary<int, Dictionary<string, string>> queue = DBRelLib.SchemaQueue(root);
                dbconn db = new dbconn(cs, driver);
                List<Dictionary<string, object>> rows = db.rows("select id from _dbrel");
                foreach (KeyValuePair<int, Dictionary<string, string>> entry in queue) {
                  string applied = "0";
                  foreach (Dictionary<string, object>row in rows) {
                    if ((int)row["id"] == entry.Key) {
                      applied = "1";
                    }
                  }
                  queue[entry.Key]["applied"] = applied;
                }
                if (schema_action.Value() == "queue") {
                  foreach (KeyValuePair<int, Dictionary<string, string>> entry in queue) {
                    if (entry.Value["applied"] == "0") {
                      Console.WriteLine(string.Format("{0}\t{1}", entry.Key, entry.Value["filepath"]));
                    }
                  }
                } else if (schema_action.Value() == "rollback") {
                  rows = db.rows("select isnull(max(id), 1) id from _dbrel");
                  int id = (int)rows[0]["id"];
                  Console.WriteLine(string.Format("Rolling back schema patch {0}.\n  I hope you know what you're doing!", id));
                  db.exec(string.Format("delete _dbrel where id = {0}", id));
                } else if (schema_action.Value() == "apply"
                        || schema_action.Value() == "manual" ) {
                  bool applied = false;
                  int i = 1;
                  while (!applied && i <= queue.Count) {
                    if (queue[i]["applied"] == "0") {
                      string filepath = queue[i]["filepath"];
                      using (StreamReader fin = File.OpenText(filepath)) {
                        string sql = fin.ReadToEnd();
                        bool result = true;
                        if (schema_action.Value() == "apply") {
                          Console.Write(string.Format("Applying schema patch {0}...", filepath));
                          result = db.exec(sql);
                          Console.WriteLine(string.Format(" {0}", ( result ? "Success" : "Fail" )));
                        } else {
                          Console.Write(string.Format("Manually marking schema patch {0} as applied.\n  I hope you know what you're doing!", i));
                        }
                        if (result) {
                          db.exec(string.Format("insert into _dbrel (id) values ({0});", i));
                        }
                      }
                      applied = true;
                    }
                    i++;
                  }
                  if (!applied) {
                    Console.WriteLine(string.Format("No schema patches to apply to {0}", tgt));
                  }
                } else {
                  Console.WriteLine(string.Format("I don't know how to perform schema action '{0}'", schema_action.Value()));
                }
              } else if (file.Value != null) {
                if (File.Exists(file.Value)) {
                  dbconn db = new dbconn(cs, driver);
                  string sql1 = DBRelLib.DropStatement(file.Value, driver);
                  db.exec(sql1);
                  using (StreamReader sr = new StreamReader(file.Value)) {
                    string sql2 = sr.ReadToEnd();
                    bool result = db.exec(sql2);
                    Console.WriteLine(string.Format("Applying {0}... {1}", pathGiven, ( result ? "Success" : "Fail" )));
                  }
                } else {
                  Console.WriteLine("Specified file does not exist...\n  {0}", file.Value);
                }
              } else {
                cla.ShowHelp();
              }
            }
          } else {
            Console.WriteLine(string.Format("Invalid target specified: {0}", tgt));
          }
        }
        return 0;
      });
      cla.Execute(args);
    }

  }

}
