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
    internal class SyncSalesReturn : ServiceJob
    {
        public override void TaskJob()
        {
            while (!CancelTokenSource.IsCancellationRequested)
            {
                try
                {
                    LogHelper.WriteLog("正在准备添加SalesReturn......");
                    var parameters = new Dictionary<string, string>
                    {
                        {"$inlinecount", "allpages"},
                        {"$select", "docNum,id,customer,returnedDate,status"},
                        {"$filter", "status+eq+'Close'+and+((ext_default_UDF18+eq+null)+or+(ext_default_UDF18+eq+''))"},
                        {"$orderby", "id+asc"}
                    };
                    var httpResult = AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/SalesReturn", parameters);
                    var jObject = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                    var salesReturnInfos = jObject["value"].Children();
                    foreach (var salesReturnInfo in salesReturnInfos)
                    {
                        var returnedDate = salesReturnInfo["returnedDate"].ToString();
                        var id = salesReturnInfo["id"].ToString();
                        httpResult = AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/SalesReturn(" + id + ")",
                            null);
                        var salesReturn = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                        var lines = salesReturn["lines"];
                        var salesOrderId = lines[0]["salesOrderId"].ToString();
                        var docNumber = salesReturn["docNum"].ToString();
                        var whsName = salesReturn["warehouse"]["whsName"].ToString();
                        var customerName = salesReturn["customerName"].ToString();
                        var sql = "select WhsCode from OWHS where WhsName=@WhsName";
                        SqlParameter[] sqLparameters =
                        {
                            new SqlParameter("@WhsName", whsName)
                        };
                        var table =
                            SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql,
                                sqLparameters)
                                .Tables[0];
                        var whsId = table.Rows[0]["WhsCode"].ToString();
                        var docCurrency = "RMB";
                        httpResult =
                            AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/SalesOrder(" + salesOrderId + ")",
                                null);
                        var salesOrder = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                        var customerInfo = salesOrder["channel"]["name"].ToString();
                        sql = "select CardCode from OCRD where CardName='" + customerInfo + "'";
                        table = SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql).Tables[0];
                        var cardCode = table.Rows[0]["CardCode"].ToString();
                        var vCreditNotes = (Documents)Globle.DiCompany.GetBusinessObject(BoObjectTypes.oCreditNotes);
                        vCreditNotes.CardCode = cardCode;
                        vCreditNotes.DocDate = DateTime.Today;
                        //vCreditNotes.DocTotal = double.Parse(salesOrder["grossDocTotal"].ToString());
                        vCreditNotes.UserFields.Fields.Item("U_occnr").Value = docNumber;
                        vCreditNotes.UserFields.Fields.Item("U_EndCust").Value = customerName; //custmerName
                        var salesLines = salesReturn["lines"];
                        var documentLines = vCreditNotes.Lines;
                        var i = 0;
                      
                        foreach (var salesLine in salesLines)
                        {
                           
                            var salesOrderLineId = salesLine["salesOrderLineId"].ToString();
                            foreach (var returnLine in salesOrder["returnLines"])
                            {
                                if (returnLine["baseDocLineNumber"].ToString().Equals(salesOrderLineId))
                                {
                                    var code = returnLine["sku"]["code"].ToString();
                                    var grossUnitPrice = returnLine["grossUnitPrice"].ToString();
                                    var unitPrice = returnLine["unitPrice"].ToString();
                                    var quantity = returnLine["quantity"].ToString();
                                    documentLines.SetCurrentLine(i);
                                    documentLines.CostingCode = cardCode;
                                    //  documentLines.COGSCostingCode = cardCode;
                                    documentLines.ItemCode = code;
                                    documentLines.VatGroup = "X1";
                                    documentLines.PriceAfterVAT = double.Parse(grossUnitPrice);
                                    //documentLines.Price = double.Parse(unitPrice)-1;
                                    documentLines.Quantity = double.Parse(quantity);
                                    documentLines.Currency = docCurrency;
                                    documentLines.WarehouseCode = whsId;
                                    documentLines.Add();
                                    i++;
                                    break;
                                }
                            }
                        }
                        var add = vCreditNotes.Add();
                        if (add != 0)
                        {
                            var errCode = 0;
                            var errMsg = "";
                            Globle.DiCompany.GetLastError(out errCode, out errMsg);
                            LogHelper.WriteLog("添加 SalesReturn 失败",
                                new Exception("SalesReturn:" + docNumber + "，错误信息:" + errMsg + "-" + errCode));
                        }
                        else
                        {
                            LogHelper.WriteLog("添加 SalesReturn 成功:" + docNumber);
                            salesReturn["ext_default_UDF18"] = string.Format("{0:u}", DateTime.Now); //B1更新时间
                            var url = "https://app1.sapanywhere.cn/sbo/SalesReturn(" + id + ")";
                            httpResult = AnwHelper.UpdateObjectHttpResult(url, salesReturn);
                        }
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(vCreditNotes);
                        GC.Collect();
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