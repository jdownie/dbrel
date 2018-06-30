using System;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using Classes;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace dbrel {

  class Program {

    static void Main(string[] args) {
      CommandLineApplication cla = new CommandLineApplication(throwOnUnexpectedArg: false);
      cla.Name = "dbrel";
      cla.FullName = "dbrel: Database Release Utility";
      cla.HelpOption("-? | -h | --help");
      cla.VersionOption( "-v | --version", "0.1" );
      CommandOption init = cla.Option( "-i |--init <dir>"
                                     , "Initialise an empty folder structure."
                                     , CommandOptionType.SingleValue);
      CommandOption target = cla.Option( "-t |--target <target>"
                                       , "Target database code referenced in .dbrel"
                                       , CommandOptionType.SingleValue);
      CommandOption schema_action = cla.Option( "-s | --schema_action <action>"
                                              , "Database schema action"
                                              , CommandOptionType.SingleValue);
      CommandArgument file = cla.Argument( "file"
                                         , "The file to run in against the target database."
                                         , false);
      cla.OnExecute(() => {
        if (init.HasValue()) {
          DBRelLib.Init(init.Value());
        } else if (schema_action.HasValue()) {
          Console.WriteLine(string.Format("Schema Action: {0}", schema_action.Value()));
        } else if (file.Value != null) {
          string root = DBRelLib.FindRoot(file.Value);
          Dictionary<string, Dictionary<string, string>> cfg = DBRelLib.Cfg(root);
          List<string> keys = new List<string>(cfg.Keys);
          string tgt = keys[0];
          if (target.HasValue()) {
            tgt = target.Value();
          }
          if (File.Exists(file.Value)) {
            dbconn db = new dbconn(cfg[tgt]["connectionString"]);
            string sql1 = DBRelLib.DropStatement(file.Value);
            db.exec(sql1);
            using (StreamReader sr = new StreamReader(file.Value)) {
              string sql2 = sr.ReadToEnd();
              Console.WriteLine(sql2);
              db.exec(sql2);
            }
          } else {
            Console.WriteLine("Specified file does not exist...\n  {0}", file.Value);
          }
        } else {
          cla.ShowHelp();
        }
        return 0;
      });
      cla.Execute(args);
    }

  }

}
