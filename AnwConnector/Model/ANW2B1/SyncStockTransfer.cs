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
    internal class SyncStockTransfer : ServiceJob
    {
        public override void TaskJob()
        {
            while (!CancelTokenSource.IsCancellationRequested)
            {
                try
                {
                    LogHelper.WriteLog("正在准备添加 InventoryTransfer......");
                    var parameters = new Dictionary<string, string>
                    {
                        {"$inlinecount", "allpages"},
                        {
                            "$select",
                            "docNum,creationTime,+toWarehouse,updateTime,+transferOutDate,transferInDate,toWarehouse,transferOutDate,updatorDisplayName,fromWarehouse,id,remarks,creatorDisplayName,status"
                        },
                        {
                            "$filter",
                            "((status+eq+'Close')+and+((ext_default_UDF22+eq+null)+or+(ext_default_UDF22+eq+'')))"
                        },
                        {"$orderby", "id+asc"},
                        {"$top", "50"},
                        {"$skip", "0"}
                    };
                    var httpResult = AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/InventoryTransfer",
                        parameters);
                    var jObject = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                    var stockTransferInfos = jObject["value"];

                    foreach (var stockTransferInfo in stockTransferInfos)
                    {
                        var docNum = stockTransferInfo["docNum"].ToString();
                        var id = stockTransferInfo["id"].ToString();
                        httpResult =
                            AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/InventoryTransfer(" + id + ")",
                                null);
                        var stockTransfer = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                        var fromWarehouse = stockTransferInfo["fromWarehouse"]["whsName"].ToString();
                        var toWarehouse = stockTransferInfo["toWarehouse"]["whsName"].ToString();
                        var sql = "select WhsCode,U_CardCode from OWHS where WhsName=@WhsName";
                        SqlParameter[] sqLparameters =
                        {
                            new SqlParameter("@WhsName", toWarehouse)
                        };
                        var table =
                            SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql,
                                sqLparameters)
                                .Tables[0];
                        var toWhsId = table.Rows[0]["WhsCode"].ToString();
                        var cardCode = table.Rows[0]["U_CardCode"].ToString();
                        SqlParameter[] sqLparameterss = { new SqlParameter("@WhsName", fromWarehouse) };
                        table =
                            SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql,
                                sqLparameterss)
                                .Tables[0];
                        var fromWhsId = table.Rows[0]["WhsCode"].ToString();
                        var oStockTransfer =
                            Globle.DiCompany.GetBusinessObject(BoObjectTypes.oStockTransfer) as StockTransfer;
                        if (oStockTransfer != null)
                        {
                            oStockTransfer.CardCode = cardCode;
                            oStockTransfer.FromWarehouse = fromWhsId;
                            oStockTransfer.ToWarehouse = toWhsId;
                            oStockTransfer.TaxDate = DateTime.Now;
                            oStockTransfer.DueDate = DateTime.Now;
                            oStockTransfer.UserFields.Fields.Item("U_occnr").Value = docNum;
                            oStockTransfer.UserFields.Fields.Item("U_TransReason").Value = "门店调拨"; //门店调拨
                            var oLines = oStockTransfer.Lines;
                            var lines = stockTransfer["lines"];
                            var current = 0;
                            foreach (var line in lines)
                            {
                                var inventoryUoM = line["inventoryUoM"].ToString();
                                var transferIn = line["transferIn"].ToString();
                                var code = line["sku"]["code"].ToString();
                                oLines.SetCurrentLine(current);
                                oLines.ItemCode = code;
                                oLines.Quantity = double.Parse(transferIn);
                                oLines.WarehouseCode = toWhsId;
                                oLines.Add();
                                current++;
                            }
                            var result = oStockTransfer.Add();
                            if (result != 0)
                            {
                                var errCode = 0;
                                var errMsg = "";
                                Globle.DiCompany.GetLastError(out errCode, out errMsg);
                                LogHelper.WriteLog("添加 InventoryTransfer 失败",
                                    new Exception("InventoryTransfer:" + docNum + "，错误信息:" + errMsg + "-" + errCode));
                            }
                            else
                            {
                                LogHelper.WriteLog("添加 InventoryTransfer 成功：" + docNum);
                                stockTransfer["ext_default_UDF22"] = string.Format("{0:u}", DateTime.Now);
                                var url = "https://app1.sapanywhere.cn/sbo/InventoryTransfer(" + id + ")";
                                httpResult = AnwHelper.UpdateObjectHttpResult(url, stockTransfer);
                            }
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(oStockTransfer);
                            GC.Collect();
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