using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.CommandLineUtils;

namespace Classes {

  public class dbconn {

    private string connectionString;
    public string errorMessage;

    public dbconn(string cs) {
      this.errorMessage = null;
      this.connectionString = cs;
    }

    private List<Dictionary<string, object>> _resultSetFromCMD(SqlCommand sqlcmd) {
      List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
      try {
        var reader = sqlcmd.ExecuteReader();
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
        this.errorMessage = null;
      } catch (Exception e) {
        this.errorMessage = e.Message;
      }
      return ret;
    }

    public List<Dictionary<string, object>> rows(string sql) {
      SqlConnection conn = new SqlConnection(this.connectionString);
      conn.Open();
      SqlCommand cmd = new SqlCommand(sql, conn);
      cmd.CommandTimeout = 300;
      List<Dictionary<string, object>> ret = this._resultSetFromCMD(cmd);
      conn.Close();
      return ret;
    }

    public bool exec(string sql) {
      bool ret = true;
      SqlConnection conn = new SqlConnection(this.connectionString);
      conn.Open();
      SqlCommand cmd = new SqlCommand(sql, conn);
      cmd.CommandTimeout = 300;
      try {
        cmd.ExecuteNonQuery();
        this.errorMessage = null;
      } catch (Exception e) {
        ret = false;
        Console.WriteLine(string.Format("Error: {0}", e.Message));
        this.errorMessage = e.Message;
      }
      conn.Close();
      return ret;
    }

  }

}