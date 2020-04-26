using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace HisabAccount.DataAccessLayer
{
    public class SQLModal
    {
        public string sqlQuery { get; set; }
        public List<SqlParameter> sqlParameters { get; set; }
        public CommandType commandType { get; }
        public string inputParameters { get; }
        public string outputParameters { get; }
        public string inputParameterValues { get; }
        public object outputCollection { get; set; }
        public string result { get; set; }
        public bool flag { get; set; }
        public string message { get; set; }
        public string functionName { get; }
        public int? userid { get; }
        public int? appid { get; }
        public Exception exception { get; set; }
        
       
        public SQLModal(string SqlQuery, bool StoredProcedure)
        {
            sqlQuery = SqlQuery;
            commandType = StoredProcedure ? CommandType.StoredProcedure : CommandType.Text;
        }
        public SQLModal(string SqlQuery, bool StoredProcedure = true, string FunctionName = null, int? UserId = null, int? Appid = null)
        {
            sqlQuery = SqlQuery;
            functionName = FunctionName;
            userid = UserId;
            appid = Appid;
            commandType = StoredProcedure ? CommandType.StoredProcedure : CommandType.Text;
        }
        public SQLModal(string SqlQuery, string InputParameters, string InputParameterValues, bool StoredProcedure = true, string FunctionName = null, int? UserId = null, int? Appid = null)
        {
            sqlQuery = SqlQuery;
            inputParameters = InputParameters;
            inputParameterValues = InputParameterValues;
            functionName = FunctionName;
            userid = UserId;
            appid = Appid;
            commandType = StoredProcedure ? CommandType.StoredProcedure : CommandType.Text;
            sqlParameters = CreateParameterList(InputParameters, InputParameterValues, "");
        }
        public SQLModal(string SqlQuery, string InputParameters, string InputParameterValues, string OutputParameters, bool StoredProcedure = true, string FunctionName = null, int? UserId = null, int? Appid = null)
        {
            sqlQuery = SqlQuery;
            inputParameters = InputParameters;
            inputParameterValues = InputParameterValues;
            functionName = FunctionName;
            userid = UserId;
            appid = Appid;
            commandType = StoredProcedure ? CommandType.StoredProcedure : CommandType.Text;
            sqlParameters = CreateParameterList(InputParameters, InputParameterValues, OutputParameters);
        }
        public SQLModal(string SqlQuery, List<SqlParameter> sqlParametersList, bool StoredProcedure = true, string FunctionName = null, int? UserId = null, int? Appid = null)
        {
            sqlQuery = SqlQuery;
            sqlParameters = sqlParametersList;
            functionName = FunctionName;
            userid = UserId;
            appid = Appid;
            commandType = StoredProcedure ? CommandType.StoredProcedure : CommandType.Text;
        }
        private List<SqlParameter> CreateParameterList(string InputParameters, string InputParameterValues, string OutputParameters = "")
        {
            List<SqlParameter> ParamList = new List<SqlParameter>();
            if (!string.IsNullOrEmpty(InputParameters))
            {
                var pname = InputParameters.Split('|');
                var pval = InputParameterValues.Split('|');

                for (int i = 0; i < pname.Length; i++)
                {
                    DataAccess.AddParameterToList("@" + pname[i], pval[i], ref ParamList, false);
                }
            }

            if (!string.IsNullOrEmpty(OutputParameters))
            {
                var pname = OutputParameters.Split('|');

                for (int i = 0; i < pname.Length; i++)
                {
                    DataAccess.AddParameterToList("@" + pname[i], 0, ref ParamList, true);
                }
            }

            return ParamList;
        }
        
    }
}
