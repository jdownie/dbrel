using System;
using Microsoft.Extensions.CommandLineUtils;
using Classes;

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
        } else {
          //Dictionary<string, DBTarget> cfg = DBRelLib.Cfg();
          Console.WriteLine(file.Value);
        }
        return 0;
      });
      cla.Execute(args);
    }

  }

}
