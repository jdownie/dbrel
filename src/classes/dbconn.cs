using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.CommandLineUtils;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace Classes {

  public class dbconn {

    private string connectionString;
    private string driver;
    public string errorMessage;

    public dbconn(string cs, string driver = "mssql") {
      this.errorMessage = null;
      this.driver = driver;
      this.connectionString = cs;
    }

    private List<Dictionary<string, object>> _resultSetFromReader(MySqlDataReader rdr) {
      List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
      while (rdr.Read()) {
        Dictionary<string, object> row = new Dictionary<string, object>();
        for (int i = 0; i < rdr.FieldCount; i++) {
          string fieldname = rdr.GetName(i);
          row[fieldname] = rdr[i];
        }
        ret.Add(row);
      }
      return ret;
    }

    private List<Dictionary<string, object>> _resultSetFromCommand(SqlCommand sqlcmd) {
      List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
      try {
        if (this.driver == "mssql") {
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
        }
        this.errorMessage = null;
      } catch (Exception e) {
        this.errorMessage = e.Message;
      }
      return ret;
    }

    public List<Dictionary<string, object>> rows(string sql) {
      List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
      if (this.driver == "mssql") {
        SqlConnection conn = new SqlConnection(this.connectionString);
        conn.Open();
        SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.CommandTimeout = 300;
        ret = this._resultSetFromCommand(cmd);
        conn.Close();
      }
      if (this.driver == "mysql") {
        MySqlConnection conn = new MySqlConnection(this.connectionString);
        conn.Open();
        MySqlCommand cmd = new MySqlCommand(sql, conn);
        MySqlDataReader rdr = cmd.ExecuteReader();
        ret = this._resultSetFromReader(rdr);
        conn.Close();
      }
      return ret;
    }

    public bool exec(string sql) {
      bool ret = true;
      if (this.driver == "mssql") {
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
      }
      if (this.driver == "mysql") {
        MySqlConnection conn = new MySqlConnection(this.connectionString);
        conn.Open();
        MySqlCommand cmd = new MySqlCommand(sql, conn);
        try {
          cmd.ExecuteNonQuery();
          this.errorMessage = null;
        } catch (Exception e) {
          ret = false;
          Console.WriteLine(string.Format("Error: {0}", e.Message));
          this.errorMessage = e.Message;
        }
        conn.Close();
      }
      return ret;
    }

  }

}
