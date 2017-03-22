using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using AnwConnector.Common;
using AnwConnector.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAPbobsCOM;


namespace AnwConnector.Model.ANW2B1
{
    internal class SyncReturnBack : ServiceJob
    {

        public override void TaskJob()
        {
            while (!CancelTokenSource.IsCancellationRequested)
            {
                try
                {
                    LogHelper.WriteLog("正在准备添加 ReturnBack......");
                    var parameters = new Dictionary<string, string>();
                    parameters.Add("$inlinecount", "allpages");
                    parameters.Add("$select",
                        "issueType,docNum,creationTime,updatorDisplayName,updateTime,id,issueDate,warehouse,+warehouse,remarks,creatorDisplayName,status");
                    parameters.Add("$filter",
                        "((status+eq+'Close')+and+(issueType+eq+'门店退回大仓')+and+((ext_default_UDF21+eq+null)+or+(ext_default_UDF21+eq+'')))");
                    parameters.Add("$orderby", "id+asc");
                    parameters.Add("$top", "50");
                    parameters.Add("$skip", "0");
                    var httpResult = AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/InventoryIssues",
                        parameters);
                    var jObject = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                    var returnBackInfos = jObject["value"];
                    foreach (var returnBackInfo in returnBackInfos)
                    {
                        var docNum = returnBackInfo["docNum"].ToString();

                        var id = returnBackInfo["id"].ToString();
                        var issueType = returnBackInfo["issueType"].ToString(); //门店领用
                        httpResult =
                            AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/InventoryIssues(" + id + ")",
                                null);
                        var inventoryGenExit = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                        var whsName = inventoryGenExit["warehouse"]["whsName"].ToString();
                        var sql = "select WhsCode from OWHS where WhsName=@WhsName";
                        SqlParameter[] sqLparameters =
                        {
                            new SqlParameter("@WhsName", whsName)
                        };
                        var table =
                            SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql,
                                sqLparameters)
                                .Tables[0];
                        var toWhsId = table.Rows[0]["WhsCode"].ToString();
                        var oInventoryGenExit =
                            Globle.DiCompany.GetBusinessObject(BoObjectTypes.oInventoryGenExit) as Documents;
                        oInventoryGenExit.DocDueDate = DateTime.Now;
                        oInventoryGenExit.TaxDate = DateTime.Now;
                        oInventoryGenExit.UserFields.Fields.Item("U_occnr").Value = docNum;
                        oInventoryGenExit.UserFields.Fields.Item("U_PUROUT").Value = issueType; //门店领用
                        var oLines = oInventoryGenExit.Lines;
                        var current = 0;
                        foreach (var line in inventoryGenExit["lines"])
                        {
                            sql = "select U_CardCode from OWHS where WhsCode=@WhsCode"; //CardCode
                            SqlParameter[] sqLparameter =
                            {
                                new SqlParameter("@WhsCode", toWhsId)
                            };
                            table =
                                SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql, sqLparameter)
                                    .Tables[0];
                            var uCarCode = table.Rows[0]["U_CardCode"].ToString();
                            oLines.CostingCode = uCarCode;
                            oLines.SetCurrentLine(current);
                            oLines.ItemCode = line["sku"]["code"].ToString();
                            oLines.Quantity = double.Parse(line["issueQuantity"].ToString());
                            oLines.WarehouseCode = toWhsId;
                            sql = "select U_Account from [@PUROUT] where Code=@Code ";
                            SqlParameter[] Sql =
                            {
                                new SqlParameter("@Code", issueType)
                            };
                            table =
                                SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql, Sql).Tables[0];
                            var uAccount = table.Rows[0]["U_Account"].ToString(); //Account
                            oLines.AccountCode = uAccount;
                            oLines.Add();
                            current++;
                        }
                        var result = oInventoryGenExit.Add();
                        if (result != 0)
                        {
                            var errCode = 0;
                            var errMsg = "";
                            Globle.DiCompany.GetLastError(out errCode, out errMsg);
                            LogHelper.WriteLog("添加 InventoryIssues 失败",
                                      new Exception("InventoryIssues:" + docNum + "，错误信息:" + errMsg + "-" + errCode));
                        }
                        else
                        {
                            LogHelper.WriteLog("添加 InventoryIssues 成功" + docNum);
                            inventoryGenExit["ext_default_UDF21"] = string.Format("{0:u}", DateTime.Now);
                            var url = "https://app1.sapanywhere.cn/sbo/InventoryIssues(" + id + ")";
                            httpResult = AnwHelper.UpdateObjectHttpResult(url, inventoryGenExit);
                        }
                    }
                }
                catch (Exception exception)
                {
                    LogHelper.WriteLog(GetType().Name, exception);
                }
                Thread.Sleep(SleepTime);
            }
        }

        protected override void Start()
        {
            Task.Factory.StartNew(TaskJob, CancelTokenSource.Token);
        }

        protected override void Stop()
        {
            if (!CancelTokenSource.IsCancellationRequested)
            {
                CancelTokenSource.Cancel();
            }
        }
    }
}
