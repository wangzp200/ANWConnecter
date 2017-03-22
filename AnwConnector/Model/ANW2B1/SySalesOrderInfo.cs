using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AnwConnector.Common;
using AnwConnector.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAPbobsCOM;

namespace AnwConnector.Model.ANW2B1
{
    internal class SySalesOrderInfo : ServiceJob
    {
        public override void TaskJob()
        {
            while (!CancelTokenSource.IsCancellationRequested)
            {
                try
                {
                    LogHelper.WriteLog("正在准备添加SalesOrderInfo......");
                    var parameters = new Dictionary<string, string>();
                    parameters.Add("$inlinecount", "allpages");
                    parameters.Add("$filter",
                        "((status+eq+'tOpen')+and+((ext_default_UDF32+eq+null)+or+(ext_default_UDF32+eq+''))");
                    parameters.Add("$select",
                        "docCurrency,quantity,headerAction,docNumber,returnStatus,salesUomName,docDate,customerName,grossDocTotal,skuName,paidTotal,id,lineNumber,invoiceStatus,logisticsStatus,allocationStatus,paymentStatus,customer,status");
                    parameters.Add("$orderby", "id+asc");
                    var httpResult = AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/SalesOrder", parameters);
                    var jObject = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                    var salesOrderInfos = jObject["value"];
                    foreach (var salesOrderInfo in salesOrderInfos)
                    {
                        var salesOrderId = salesOrderInfo["id"].ToString();
                        var docNumber = salesOrderInfo["docNumber"].ToString();
                        httpResult =
                            AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/SalesOrder(" + salesOrderId + ")",
                                null);
                        var salesOrder = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                        var bsproductLines = salesOrder["productLines"];

                        //var customerInfo = salesOrder["channel"]["name"].ToString();

                        //var sql = "select CardCode from OCRD where CardName='" + customerInfo + "'";
                        //var table = SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql).Tables[0];
                        //var cardCode = table.Rows[0]["CardCode"].ToString();
                        //var shippingCost = salesOrder["shippingCost"].ToString();
                        var vOrders = (Documents)Globle.DiCompany.GetBusinessObject(BoObjectTypes.oOrders);
                        var customerName = salesOrder["customer"]["displayName"].ToString();
                        var sql = "select CardCode from OCRD where CardName=@customerName";
                        SqlParameter[] sqlParameters =
                        {
                            new SqlParameter("@customerName", customerName)
                        };
                        var table =
                            SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql, sqlParameters)
                                .Tables[0];
                        var cardCode = table.Rows[0]["CardCode"].ToString();
                        var channelname = salesOrder["channel"]["name"].ToString();
                        vOrders.UserFields.Fields.Item("U_Channel").Value = channelname;
                        vOrders.UserFields.Fields.Item("U_SalesOrderId").Value = docNumber;
                        var discountPetage = salesOrder["discountPercentage"].ToString();
                        vOrders.DiscountPercent = double.Parse(discountPetage);
                        vOrders.CardCode = cardCode;
                        vOrders.DocDate = DateTime.Today;
                        vOrders.DocDueDate = DateTime.Today;
                        var documentLines = vOrders.Lines;
                        var i = 0;
                        foreach (var bsproductLine in bsproductLines)
                        {
                            var whsName = "";
                            var whsId = "";
                            var docCurrency = "RMB";
                            whsName = bsproductLine["warehouse"]["whsName"].ToString();
                            whsId = bsproductLine["warehouse"]["id"].ToString();
                            sql = "select WhsCode from OWHS where WhsName=@WhsName";
                            SqlParameter[] sqLparameters =
                            {
                                new SqlParameter("@WhsName", whsName)
                            };
                            table =
                                SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql,
                                    sqLparameters)
                                    .Tables[0];
                            whsId = table.Rows[0]["WhsCode"].ToString();
                            var sku = bsproductLine["sku"];
                            var code = sku["code"].ToString();
                            var quantity = bsproductLine["quantity"].ToString();
                            var skuName = bsproductLine["skuName"].ToString();
                            //var uom = bsproductLine["uom"].ToString();
                            var grossUnitPrice = bsproductLine["grossUnitPrice"].ToString();
                            var inventoryUomName = bsproductLine["inventoryUomName"].ToString();
                            var inventoryUomQuantity = bsproductLine["inventoryUomQuantity"].ToString();
                            var unitPrice = bsproductLine["unitPrice"].ToString();
                            var paymentTerm = bsproductLine["paymentTerm"];
                            var netLineTotal = bsproductLine["netLineTotal"];
                            var taxAmount = bsproductLine["taxAmount"];
                            documentLines.Add();
                            documentLines.SetCurrentLine(i);
                            //documentLines.CostingCode = "ANW9";//cardCode
                            //documentLines.COGSCostingCode = cardCode; //cardCode
                            documentLines.ItemCode = code;
                            documentLines.PriceAfterVAT = double.Parse(grossUnitPrice);
                            documentLines.Price = double.Parse(unitPrice);
                            documentLines.Quantity = double.Parse(quantity);
                            documentLines.Currency = docCurrency;
                            documentLines.WarehouseCode = whsId;
                            i++;
                        }
                        var add = vOrders.Add();
                        if (add != 0)
                        {
                            var errCode = 0;
                            var errMsg = "";
                            Globle.DiCompany.GetLastError(out errCode, out errMsg);
                            LogHelper.WriteLog("添加 SalesOrder 失败",
                                new Exception("SalesOrder:" + docNumber + "，错误信息:" + errMsg + "-" + errCode));
                        }
                        else
                        {
                            LogHelper.WriteLog("添加 Invoice 成功:" + docNumber);
                            salesOrder["ext_default_UDF32"] = string.Format("{0:u}", DateTime.Now);
                            var url = "https://app1.sapanywhere.cn/sbo/SalesOrder(" + salesOrderId + ")";
                            httpResult = AnwHelper.UpdateObjectHttpResult(url, salesOrder);
                        }
                        Marshal.ReleaseComObject(vOrders);
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