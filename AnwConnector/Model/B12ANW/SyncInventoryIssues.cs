using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AnwConnector.Common;
using AnwConnector.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAPbobsCOM;

namespace AnwConnector.Model.B12ANW
{
    internal class SyncInventoryIssues : ServiceJob
    {
        public override void TaskJob()
        {
            while (!CancelTokenSource.IsCancellationRequested)
            {
                try
                {
                    LogHelper.WriteLog("正在准备添加 (门店退回大仓)InventoryIssues......");
                    var oStockTransfer =
                         Globle.DiCompany.GetBusinessObject(BoObjectTypes.oStockTransfer) as StockTransfer;

                    var sql =
                        "SELECT DocEntry,Filler,U_TransReason,(select top 1 T4.WhsName from OWHS T4 where T4.WhsCode=T0.Filler ) as 'FoWhsName',ToWhsCode,(select top 1 T3.WhsName from OWHS T3 where T3.WhsCode=T0.ToWhsCode ) as 'ToWhsName' FROM OWTR T0 where ISNULL(T0.U_occnr,'')='' and T0.DocStatus='O' and T0.CANCELED='N'and exists (SELECT 'A' FROM WTR1 T1 inner join OWHS T2 on T1.WhsCode=T2.WhsCode where T0.DocEntry=T1.DocEntry and T2.WhsName=N'杭州总仓')";

                    var table =
                        SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql, null).Tables[0];

                    foreach (DataRow row in table.Rows)
                    {
                        var docEntry = int.Parse(row["DocEntry"].ToString());
                        if (oStockTransfer.GetByKey(docEntry))
                        {
                            var foWhsCode = row["Filler"].ToString();
                            var foWhsName = row["FoWhsName"].ToString();
                            var toWhsCode = row["ToWhsCode"].ToString();
                            var toWhsName = row["ToWhsName"].ToString();
                            var toTransReason = row["U_TransReason"].ToString();
                            var oLines = oStockTransfer.Lines;
                            if (toTransReason != "门店退回大仓")
                            {
                                continue; //退回大仓
                            }
                            var httpResult = AnwHelper.GetHttpResult(
                                "https://app1.sapanywhere.cn/sbo/InventoryIssues/createBO", null);
                            var jObject = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                            var xBusinessToken = httpResult.Header.Get("X-Business-Token");
                            var inventoryIssues = jObject;
                            var warehouse = (JObject)inventoryIssues["warehouse"];
                            var parameters = new Dictionary<string, string>
                            {
                                {"inlinecount", "allpages"},
                                {"$select", "+whsName,address,isDefaultWhs,address1,whsName,id,whsCode,status"},
                                {"$filter", "(status+eq+'Active')+and+(whsName+eq+'" + foWhsName + "')"},
                                {"$orderby", "whsCode+asc"},
                                {"$top", "50"},
                                {"$skip", "0"}
                            };

                            httpResult = AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/Warehouse",
                                parameters);
                            jObject = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                            var warehs = ((JArray)jObject["value"])[0];
                            warehouse["id"] = warehs["id"];
                            warehouse["whsName"] = warehs["whsName"];
                            var lines = (JArray)inventoryIssues["lines"];
                            var rowNum = 1;
                            var receiptTotal = 0.0;
                            for (var i = 0; i < oLines.Count; i++)
                            {
                                oLines.SetCurrentLine(i);
                                if (oLines.FromWarehouseCode == foWhsCode && oLines.WarehouseCode == toWhsCode)
                                {
                                    var itemCode = oLines.ItemCode;
                                    httpResult =
                                        AnwHelper.GetHttpResult(
                                            "https://app1.sapanywhere.cn/sbo/SKU?$inlinecount=allpages&$filter=(code+eq+'" +
                                            itemCode + "')&$orderby=id+asc&$top=10&$skip=0", null);
                                    jObject = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                                    var sku = ((JArray)jObject["value"])[0];
                                    var line = new JObject
                                    {
                                        new JProperty("barCode",
                                            string.IsNullOrEmpty(sku["barCode"].ToString())
                                                ? null
                                                : sku["barCode"].ToString()),
                                        new JProperty("id", null),
                                        new JProperty("inStock", "0.0"),
                                        new JProperty("inventoryUoM", sku["product"]["unitOfMeasure"].ToString()),
                                        new JProperty("lineTotal", (oLines.Quantity*oLines.Price).ToString()),
                                        new JProperty("issueQuantity", oLines.Quantity.ToString()),
                                        new JProperty("rowNum", rowNum),
                                        new JProperty("unitPrice", oLines.Price.ToString())
                                    };
                                    receiptTotal = receiptTotal + oLines.Quantity;
                                    line.Add(new JProperty("batches", new JArray()));
                                    line.Add(new JProperty("invUom", null));
                                    var linesku = new JObject
                                    {
                                        new JProperty("batchSerial", sku["batchSerial"].ToString()),
                                        new JProperty("code", sku["code"].ToString()),
                                        new JProperty("id", int.Parse(sku["id"].ToString())),
                                        new JProperty("name", sku["name"].ToString()),
                                        new JProperty("bundle", null),
                                        new JProperty("product", null)
                                    };
                                    line.Add(new JProperty("sku", linesku));
                                    lines.Add(line);
                                    rowNum++;
                                }
                            }
                            inventoryIssues["issueTotal"] = receiptTotal.ToString();
                            inventoryIssues["issueType"] = "退回杭州总仓";
                            httpResult = AnwHelper.AddObjectHttpResult(
                                "https://app1.sapanywhere.cn/sbo/InventoryIssues",
                                inventoryIssues, xBusinessToken);
                            if (httpResult.StatusCode == HttpStatusCode.Created)
                            {
                                jObject = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                                var docNum = jObject["docNum"].ToString();
                                oStockTransfer.UserFields.Fields.Item("U_occnr").Value = docNum;
                                var result = oStockTransfer.Update();
                                if (result != 0)
                                {
                                    sql = "UPDATE OWTR SET U_occnr='" + docNum + "' WHERE DocEntry=" + docEntry;
                                    SqlHelper.ExecuteNonQuery(SqlHelper.GetConnSting(), CommandType.Text, sql);
                                    var errCode = 0;
                                    var errMsg = "";
                                    Globle.DiCompany.GetLastError(out errCode, out errMsg);
                                    LogHelper.WriteLog("添加 InventoryTransfer 失败",
                                        new Exception("InventoryTransfer:" + docNum + "，错误信息:" + errMsg + "-" + errCode));
                                }
                                else
                                {
                                    LogHelper.WriteLog("添加 InventoryTransfer 成功:" + docNum);
                                }
                            }
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