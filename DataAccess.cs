using HisabAccount.Models.ErrorLog;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HisabAccount.DataAccessLayer
{
    public static class DataAccess
    {
        private static readonly string constring = "Data Source=(local);Initial Catalog=dbplayer;User ID=sa;Password=vij@uma11";

        public static object ExecuteScalar(ref SQLModal sqlmodal)
        {
            object returnvalue = null;

            Preparecommand(sqlmodal.sqlQuery, sqlmodal.sqlParameters, out SqlCommand cmd, sqlmodal.commandType);
            returnvalue = ExecuteScalarCommon(sqlmodal, returnvalue, cmd);
            return returnvalue;
        }
        public static object ExecuteScalar<T>(T Modal, ref SQLModal sqlmodal) where T : class
        {
            object returnvalue = null;
            sqlmodal.sqlParameters = CreateParameterList(Modal, sqlmodal.outputParameters);
            Preparecommand(sqlmodal.sqlQuery, sqlmodal.sqlParameters, out SqlCommand cmd, sqlmodal.commandType);
            returnvalue = ExecuteScalarCommon(sqlmodal, returnvalue, cmd);
            return returnvalue;
        }

        public static void GetDataSet(ref SQLModal sqlmodal)
        {
            DataSet ds = new DataSet();
            Preparecommand(sqlmodal.sqlQuery, sqlmodal.sqlParameters, out SqlCommand cmd, out SqlDataAdapter da, sqlmodal.commandType);
            GetDataSetCommon(sqlmodal, ds, cmd, da);
        }
        public static void GetDataSet<T>(T Modal, ref SQLModal sqlmodal) where T : class
        {
            DataSet ds = new DataSet();
            sqlmodal.sqlParameters = CreateParameterList(Modal, sqlmodal.outputParameters);
            Preparecommand(sqlmodal.sqlQuery, sqlmodal.sqlParameters, out SqlCommand cmd, out SqlDataAdapter da, sqlmodal.commandType);
            GetDataSetCommon(sqlmodal, ds, cmd, da);
        }

        public static void GetDataTable(ref SQLModal sqlmodal)
        {
            DataTable DT = new DataTable();
            Preparecommand(sqlmodal.sqlQuery, sqlmodal.sqlParameters, out SqlCommand cmd, sqlmodal.commandType);
            GetDataTableCommon(sqlmodal, DT, cmd);
        }
        public static void GetDataTable<T>(T Modal, ref SQLModal sqlmodal) where T : class
        {
            DataTable DT = new DataTable();
            sqlmodal.sqlParameters = CreateParameterList(Modal, sqlmodal.outputParameters);
            Preparecommand(sqlmodal.sqlQuery, sqlmodal.sqlParameters, out SqlCommand cmd, sqlmodal.commandType);
            GetDataTableCommon(sqlmodal, DT, cmd);
        }

        public static void InsertUpdateDelete(ref SQLModal sqlmodal)
        {
            Preparecommand(sqlmodal.sqlQuery, sqlmodal.sqlParameters, out SqlCommand cmd, sqlmodal.commandType);
            InserUpdateDeleteCommon(sqlmodal, cmd);
        }
        public static void InsertUpdateDelete<T>(T Modal, ref SQLModal sqlmodal) where T : class
        {
            sqlmodal.sqlParameters = CreateParameterList(Modal, sqlmodal.outputParameters);
            Preparecommand(sqlmodal.sqlQuery, sqlmodal.sqlParameters, out SqlCommand cmd, sqlmodal.commandType);
            InserUpdateDeleteCommon(sqlmodal, cmd);
        }

        public static void BulkInsert(DataTable sourceTable, string destinationTable)
        {
            string connectionstring = constring;
            using (SqlConnection con = new SqlConnection(connectionstring))
            {
                con.Open();
                using (var bulkCopy = new SqlBulkCopy(con))
                {
                    bulkCopy.DestinationTableName = destinationTable;
                    //If Source and Destination Column Same(include identity) there is no need to column mapping.
                    foreach (DataColumn dc in sourceTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(dc.ColumnName, dc.ColumnName);
                    }
                    bulkCopy.WriteToServer(sourceTable);
                }
            }
        }

        #region CommonMethods

        private static void ErrorLogToTextFile(Errorlog error, SQLModal sqlmodal)
        {
            try
            {
                SqlParameter compnayidparam = sqlmodal.sqlParameters.FirstOrDefault(p => p.ParameterName == "@companyid");
                error.companyid = (int)(compnayidparam.Value ?? 0);
                error.functionname = sqlmodal.functionName;

                StringBuilder sb = new StringBuilder("Parameters : " + Environment.NewLine);
                foreach (SqlParameter parameter in sqlmodal.sqlParameters)
                {
                    sb.AppendLine($"Parameter Name: {parameter.ParameterName}, Parameter Value: {parameter.Value}");
                }
                error.inputparameters = sb.ToString();

                StringBuilder sb1 = new StringBuilder("Error Log : " + Environment.NewLine);
                sb1.AppendLine("-----------------------------------------------------------------------------------------");
                sb1.AppendLine($"Log Date Time: {DateTime.Now}");
                sb1.AppendLine($"Function Name: {error.functionname ?? "Not Provided"}");
                sb1.AppendLine($"Function Parameters: {error.inputparameters}");
                sb1.AppendLine($"Error Text: {error.errotext}");
                sb1.AppendLine($"Appication ID: {error.appid}");
                sb1.AppendLine($"Company ID: {error.companyid}");
                sb1.AppendLine($"User ID: {sqlmodal.userid}");
                sb1.AppendLine("-----------------------------------------------------------------------------------------");
                error.userid = sqlmodal.userid;

                System.IO.File.WriteAllText(@"D:ErrorLog.txt", sb1.ToString() + Environment.NewLine);
            }
            catch (Exception)
            {
            }
        }
        private static void ErrorLogToDatabase(Errorlog error, SQLModal sqlmodal)
        {
            SqlParameter compnayidparam = sqlmodal.sqlParameters.FirstOrDefault(p => p.ParameterName == "@companyid");
            error.companyid = (int)(compnayidparam.Value ?? 0);
            error.functionname = sqlmodal.functionName;
            StringBuilder sb = new StringBuilder("Parameters Used: ");
            foreach (SqlParameter parameter in sqlmodal.sqlParameters)
            {
                sb.AppendLine($"Parameter Name: {parameter.ParameterName}, Parameter Value: {parameter.Value}");
            }
            error.inputparameters = sb.ToString();
            error.userid = sqlmodal.userid;
            string insertquery = $"insert into tblerrorlog(logdatetime, functionname, inputparameters, errotext, stacktrace, appid, companyid, userid) " +
                $"values(GETUTCDATE(), '{error.functionname}', '{error.inputparameters}','{error.errotext.Replace("'", "''")}','{error.stacktrace.Replace("'", "''")}','{error.appid}','{error.companyid}','{error.userid}')";
          
            Preparecommand(insertquery, null, out SqlCommand ErrorCommand, CommandType.Text);
            try
            {
                using (ErrorCommand.Connection)
                {
                    ErrorCommand.Connection.Open();
                    ErrorCommand.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
                ErrorLogToTextFile(error, sqlmodal);
            }
            finally
            {
                ErrorCommand.Connection.Close();
            }
        }
        private static void Preparecommand(string sqlquery, List<SqlParameter> param, out SqlCommand cmd, out SqlDataAdapter da, CommandType ct)
        {
            cmd = new SqlCommand
            {
                Connection = new SqlConnection(constring),
                CommandText = sqlquery,
                CommandType = ct
            };
            if (param != null)
            {
                cmd.Parameters.AddRange(param.ToArray());
            }
            da = new SqlDataAdapter(cmd);
        }
        private static void Preparecommand(string sqlquery, List<SqlParameter> param, out SqlCommand cmd, CommandType ct)
        {
            cmd = new SqlCommand
            {
                Connection = new SqlConnection(constring),
                CommandText = sqlquery,
                CommandType = ct
            };
            if (param != null)
            {
                cmd.Parameters.AddRange(param.ToArray());
            }
        }
        private static object ExecuteScalarCommon(SQLModal sqlmodal, object returnvalue, SqlCommand cmd)
        {
            try
            {
                using (cmd.Connection)
                {
                    cmd.Connection.Open();
                    returnvalue = cmd.ExecuteScalar();
                }
                sqlmodal.flag = true;
                sqlmodal.message = "Success";
                sqlmodal.result = returnvalue.ToString();
            }
            catch (Exception ex)
            {
                sqlmodal.flag = false;
                sqlmodal.message = "Error !! ExecuteScalar ";
                sqlmodal.result = null;
                sqlmodal.exception = ex;
            }

            return returnvalue;
        }
        private static void GetDataSetCommon(SQLModal sqlmodal, DataSet ds, SqlCommand cmd, SqlDataAdapter da)
        {
            try
            {
                cmd.Connection.Open();
                using (SqlTransaction ST = cmd.Connection.BeginTransaction())
                {
                    da.SelectCommand.Transaction = ST;
                    da.Fill(ds);

                    List<object> outlist = new List<object>();

                    for (int i = 0; i < cmd.Parameters.Count; i++)
                    {
                        if (cmd.Parameters[i].Direction == ParameterDirection.Output)
                        {
                            outlist.Add(cmd.Parameters[i].Value);
                        }
                    }
                    sqlmodal.outputCollection = outlist;
                    sqlmodal.result = JsonConvert.SerializeObject(ds);
                    sqlmodal.flag = true;
                    sqlmodal.message = "Success";
                }
            }
            catch (Exception ex)
            {
                sqlmodal.result = null;
                sqlmodal.flag = false;
                sqlmodal.message = "Error!! GetDataset ";
                sqlmodal.exception = ex;
                throw;
            }
            finally
            {
                cmd.Connection.Close();
            }
        }
        private static void InserUpdateDeleteCommon(SQLModal sqlmodal, SqlCommand cmd)
        {
            try
            {
                using (cmd.Connection)
                {
                    SqlParameter ReturnParameter = cmd.Parameters.Add("RetVal", SqlDbType.Int);
                    ReturnParameter.Direction = ParameterDirection.ReturnValue;
                    cmd.Connection.Open();
                    cmd.ExecuteNonQuery();
                    List<object> outlist = new List<object>();

                    for (int i = 0; i < cmd.Parameters.Count; i++)
                    {
                        if (cmd.Parameters[i].Direction == ParameterDirection.Output)
                        {
                            outlist.Add(cmd.Parameters[i].Value);
                        }
                    }
                    sqlmodal.outputCollection = outlist;

                    sqlmodal.result = ReturnParameter.Value.ToString();
                    sqlmodal.message = "Success";
                    sqlmodal.flag = true;
                }
            }
            catch (Exception ex)
            {
                sqlmodal.flag = false;
                sqlmodal.message = "Error InsertUpdate ";
                sqlmodal.result = "-1";
                sqlmodal.exception = ex;
                Errorlog el = new Errorlog
                {
                    functionname = sqlmodal.functionName,
                    errotext = ex.Message,
                    stacktrace = ex.StackTrace,
                    userid = sqlmodal.userid,
                    appid = sqlmodal.appid
                };
                ErrorLogToDatabase(el, sqlmodal);
            }
            finally
            {
                cmd.Connection.Close();
            }
        }
        private static void GetDataTableCommon(SQLModal sqlmodal, DataTable DT, SqlCommand cmd)
        {
            try
            {
                SqlDataReader rdr;
                using (cmd.Connection)
                {
                    cmd.Connection.Open();
                    rdr = cmd.ExecuteReader();
                    DT.Load(rdr);

                    List<object> outlist = new List<object>();

                    for (int i = 0; i < cmd.Parameters.Count; i++)
                    {
                        if (cmd.Parameters[i].Direction == ParameterDirection.Output)
                        {
                            outlist.Add(cmd.Parameters[i].Value);
                        }
                    }

                    if (DT.Rows.Count > 0)
                    {
                        sqlmodal.outputCollection = outlist;
                        sqlmodal.flag = true;
                        sqlmodal.message = $"{DT.Rows.Count} Record Found.";
                        sqlmodal.result = JsonConvert.SerializeObject(DT);
                    }
                    else
                    {
                        sqlmodal.outputCollection = outlist;
                        sqlmodal.flag = false;
                        sqlmodal.message = "No Data Found";
                        sqlmodal.result = "No Data Found";
                    }
                }
            }
            catch (Exception ex)
            {
                sqlmodal.flag = false;
                sqlmodal.message = "Error !! Get Data Table ";
                sqlmodal.result = null;
                sqlmodal.exception = ex;
                Errorlog el = new Errorlog
                {
                    functionname = sqlmodal.functionName,
                    errotext = ex.Message,
                    stacktrace = ex.StackTrace,
                    userid = sqlmodal.userid,
                    appid = sqlmodal.appid
                };
                ErrorLogToDatabase(el, sqlmodal);
            }
        }


        private static List<SqlParameter> CreateParameterList<T>(T ModelName, string OutputParameters = "") where T : class
        {
            List<SqlParameter> ParamList = new List<SqlParameter>();
            if (ModelName != null)
            {
                PropertyInfo[] property = ModelName.GetType().GetProperties();
                foreach (PropertyInfo info in property)
                {
                    if (info.GetValue(ModelName, null) != null && info.GetValue(ModelName, null).ToString() != "0" && info.GetValue(ModelName, null).ToString() != "")
                        AddParameterToList("@" + info.Name, info.GetValue(ModelName, null), ref ParamList, false);
                }
            }

            if (!string.IsNullOrEmpty(OutputParameters))
            {
                var pname = OutputParameters.Split('|');

                for (int i = 0; i < pname.Length; i++)
                {
                    AddParameterToList("@" + pname[i], 0, ref ParamList, true);
                }
            }
            return ParamList;
        }
        public static void AddParameterToList(string ParameterName, object ParameterValue, ref List<SqlParameter> ParamList, bool IsOutput = false)
        {
            SqlParameter P = new SqlParameter();

            if (IsOutput)
            {
                P.ParameterName = ParameterName;
                P.Direction = ParameterDirection.Output;
                P.Size = 500;
            }
            else
            {
                P.ParameterName = ParameterName;
                P.Value = ParameterValue;
            }

            if (!string.IsNullOrEmpty(ParameterName))
            {
                ParamList.Add(P);
            }
        }
        #endregion
    }
}
