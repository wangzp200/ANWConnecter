using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AnwConnector.Common;
using AnwConnector.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAPbobsCOM;

namespace AnwConnector.Model.ANW2B1
{
    internal class SyncInventoryCountings : ServiceJob
    {
        public override void TaskJob()
        {
            while (!CancelTokenSource.IsCancellationRequested)
            {
                try
                {
                    LogHelper.WriteLog("正在准备添加 InventoryCounting......");
                    var parameters = new Dictionary<string, string>();
                    parameters.Add("$inlinecount", "allpages");
                    parameters.Add("$select",
                        "docNum,countedDate,creationTime,updatorDisplayName,+status,updateTime,id,warehouse,creatorDisplayName,+countedDate,status");
                    parameters.Add("$filter",
                        "((status+eq+'Close')+and+((ext_default_UDF19+eq+null)+or+(ext_default_UDF19+eq+'')))");
                    parameters.Add("$orderby", "id+asc");
                    parameters.Add("$top", "50");
                    parameters.Add("$skip", "0");
                    var httpResult = AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/InventoryCounting",
                        parameters);
                    var jObject = (JObject)JsonConvert.DeserializeObject(httpResult.Html);

                    var inventoryCountingInfos = jObject["value"];
                    var oInventoryCountingsService =
                        (InventoryCountingsService)
                            Globle.DiCompany.GetCompanyService()
                                .GetBusinessService(ServiceTypes.InventoryCountingsService);
                    foreach (var inventoryCountingInfo in inventoryCountingInfos)
                    {
                        var id = inventoryCountingInfo["id"].ToString();
                        LogHelper.WriteLog("正在从ANW读取：" + id + " InventoryCounting");
                        var docNum = inventoryCountingInfo["docNum"].ToString();

                        var sql = "SELECT 'A' FROM OINC WHERE U_occnr=@occnr";

                        SqlParameter[] sqLparameters =
                        {
                            new SqlParameter("@occnr", docNum)
                        };
                        var table = SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql,
                            sqLparameters)
                            .Tables[0];
                        if (table.Rows.Count > 0)
                        {
                            continue;
                        }
                        httpResult =
                            AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/InventoryCounting(" + id + ")",
                                null);
                        var inventoryCounting = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                        var oInventoryCounting =
                            (InventoryCounting)
                                oInventoryCountingsService.GetDataInterface(
                                    InventoryCountingsServiceDataInterfaces.icsInventoryCounting);
                        var whsName = inventoryCountingInfo["warehouse"]["whsName"].ToString();

                        sql = "select WhsCode,U_CardCode from OWHS where WhsName=@WhsName";
                        SqlParameter[] sqLparameterss = { new SqlParameter("@WhsName", whsName) };
                        table =
                            SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql,
                                sqLparameterss)
                                .Tables[0];
                        var whsId = table.Rows[0]["WhsCode"].ToString();

                        var lines = inventoryCounting["lines"];
                        foreach (var line in lines)
                        {
                            var inventoryUoM = line["inventoryUoM"].ToString();
                            var countedQuantity = line["countedQuantity"].ToString();
                            var code = line["sku"]["code"].ToString();
                            var oInventoryCountingLine = oInventoryCounting.InventoryCountingLines.Add();
                            oInventoryCountingLine.ItemCode = code;
                            oInventoryCountingLine.CountedQuantity = double.Parse(countedQuantity);
                            oInventoryCountingLine.CounterType = CounterTypeEnum.ctUser;
                            oInventoryCountingLine.Counted = BoYesNoEnum.tYES;
                            oInventoryCountingLine.WarehouseCode = whsId;
                        }
                        oInventoryCounting.UserFields.Item("U_occnr").Value = docNum;
                        var result = oInventoryCountingsService.Add(oInventoryCounting);

                        LogHelper.WriteLog("成功从ANW到B1同步:" + docNum + " InventoryCounting");

                        inventoryCounting["ext_default_UDF19"] = string.Format("{0:u}", DateTime.Now);
                        var url = "https://app1.sapanywhere.cn/sbo/InventoryCounting(" + id + ")";
                        httpResult = AnwHelper.UpdateObjectHttpResult(url, inventoryCounting);
                        if (httpResult.StatusCode == HttpStatusCode.Created)
                        {
                            LogHelper.WriteLog("完成ANW更新标记:" + docNum + " InventoryCounting");
                        }
                        else
                        {
                            LogHelper.WriteLog("ANW更新标记失败：" + docNum + " InventoryCounting",
                                new Exception(httpResult.Html));
                        }
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(oInventoryCounting);
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(oInventoryCountingsService);
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