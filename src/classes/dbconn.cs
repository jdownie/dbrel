using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;

namespace Classes {

  public class dbconn {

    private string connectionString;

    public dbconn(string cs) {
      this.connectionString = cs;
    }

    private List<Dictionary<string, object>> _resultSetFromCMD(SqlCommand sqlcmd) {
      var reader = sqlcmd.ExecuteReader();
      List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
      while (reader.Read()) {
        Dictionary<string, object> record = new Dictionary<string, object>();
        for (int i = 0; i < reader.FieldCount; i++) {
          string colname = reader.GetName(i);
          if (reader.IsDBNull(i)) {
            record[colname] = null;
          } else {
            if (reader[i] is string) {
              record[colname] = ((string)reader[i]).TrimEnd();
            } else {
              record[colname] = reader[i];
            }
          }
        }
        ret.Add(record);
      }
      reader.Close();
      return ret;
    }

    public List<Dictionary<string, object>>fromString(string sql) {
      SqlConnection conn = new SqlConnection(this.connectionString);
      conn.Open();
      SqlCommand cmd = new SqlCommand(sql, conn);
      cmd.CommandTimeout = 300;
      List<Dictionary<string, object>> ret = this._resultSetFromCMD(cmd);
      return ret;
    }

  }

}