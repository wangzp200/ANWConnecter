using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using AnwConnector.Common;
using AnwConnector.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAPbobsCOM;

namespace AnwConnector
{
    internal static class Program
    {
        /// <summary>
        ///     应用程序的主入口点。
        /// </summary>
        private static void Main()
        {
            //var servicesToRun = new ServiceBase[]
            //{
            //    new AnwConnector()
            //};
            //ServiceBase.Run(servicesToRun);
           LogHelper.WriteLog("正在准备添加SalesReturn......");
                    var parameters = new Dictionary<string, string>();
                    parameters.Add("$inlinecount", "allpages");
                    parameters.Add("$filter",
                        "((status+eq+'tClosed')+and+((ext_default_UDF34+eq+null)+or+(ext_default_UDF34+eq+''))");
                    parameters.Add("$select",
                        "docNumber,+status,dueDate,+paymentStatus,+dueDate,postingDate,id,customerName,paymentStatus,customer,status");
                    parameters.Add("$orderby", "id+asc");
                    parameters.Add("$top", "50");
                    parameters.Add("$skip", "0");
                    var httpResult = AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/Invoice", parameters);
                    var jObject = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                    var invoiceInfos = jObject["value"].Children().ToList();
                    foreach (var invoiceInfo in invoiceInfos)
                    {
                        var invoceId = invoiceInfo["id"].ToString();
                        httpResult = AnwHelper.GetHttpResult(
                            "https://app1.sapanywhere.cn/sbo/Invoice(" + invoceId + ")", null);
                        var invoice = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                        var customerName = invoice["customerName"].ToString();
                        var sql = "select CardCode from OCRD where CardName='" + customerName + "'";
                        var table = SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql).Tables[0];
                        var cardCode = table.Rows[0]["CardCode"].ToString();
                        var invoiceLines = invoice["invoiceLines"]; 
                        
                        var docNumber = invoice["docNumber"].ToString();
                        var salesOrderId = invoiceLines[0]["baseDocId"].ToString();
                        var salsesOrderNum = invoiceLines[0]["baseDocNumber"].ToString();
                        httpResult =
                            AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/SalesOrder(" + salesOrderId + ")",
                                null);
                        var salesOrder = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                        var bsproductLines = salesOrder["productLines"];
                        var channel = salesOrder["channel"]["name"].ToString();
                        //var sql = "select CardCode from OCRD where CardName='" + customerInfo + "'";
                        //var table = SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql).Tables[0];
                        //var cardCode = table.Rows[0]["CardCode"].ToString();
                        var shippingCost = salesOrder["shippingCost"].ToString();
                        var vInvoice = (Documents)Globle.DiCompany.GetBusinessObject(BoObjectTypes.oInvoices);
                        vInvoice.CardCode = cardCode; //cardCode
                        vInvoice.DocDate = DateTime.Today;
                        vInvoice.UserFields.Fields.Item("U_LineId").Value = salsesOrderNum;
                        //vInvoice.DocTotal = double.Parse(invoice["grossDocTotal"].ToString());
                        vInvoice.UserFields.Fields.Item("U_Channel").Value = channel;
                        //vInvoice.UserFields.Fields.Item("U_SalesOrderId").Value = salsesOrderNum; 
                        var expenses = vInvoice.Expenses;
                        expenses.SetCurrentLine(0);
                        expenses.ExpenseCode = 1;
                        //expenses.LineTotal = Math.Round((double.Parse(shippingCost) + 5) / 1.03, 2);
                        expenses.DistributionRule = cardCode; //cardCode

                        var documentLines = vInvoice.Lines;
                        var i = 0;
                      
                        foreach (var invoiceLine in invoiceLines)
                        {
                            var whsName = "";
                            var whsId = "";
                            var docCurrency = "RMB";
                            var baseDocLineNumber = invoiceLine["baseDocLineNumber"].ToString();
                            foreach (var bsproductLine in bsproductLines)
                            {
                                if (baseDocLineNumber == bsproductLine["lineNumber"].ToString())
                                {
                                    whsName = bsproductLine["warehouse"]["whsName"].ToString();
                                    whsId = bsproductLine["warehouse"]["id"].ToString();
                                    break;
                                }
                            }

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
                            var sku = invoiceLine["sku"];
                            var code = sku["code"].ToString();
                            var quantity = invoiceLine["quantity"].ToString();
                            var skuName = invoiceLine["skuName"].ToString();
                            var uom = invoiceLine["uom"].ToString();
                            var grossUnitPrice = invoiceLine["grossUnitPrice"].ToString();
                            var inventoryUomName = invoiceLine["inventoryUomName"].ToString();
                            var inventoryUomQuantity = invoiceLine["inventoryUomQuantity"].ToString();
                            var unitPrice = invoiceLine["unitPrice"].ToString();


                            var paymentTerm = invoiceLine["paymentTerm"];
                            var netLineTotal = invoiceLine["netLineTotal"];
                            var taxAmount = invoiceLine["taxAmount"];


                            documentLines.Add();
                            documentLines.SetCurrentLine(i);
                            //documentLines.CostingCode = "ANW9";//cardCode
                            documentLines.COGSCostingCode = cardCode; //cardCode
                            documentLines.ItemCode = code;
                            documentLines.PriceAfterVAT = double.Parse(grossUnitPrice);
                            documentLines.Price = double.Parse(unitPrice);
                            documentLines.Quantity = double.Parse(quantity);
                            documentLines.Currency = docCurrency;
                            documentLines.WarehouseCode = whsId;
                            i++;
                        }
                        var add = vInvoice.Add();
                        if (add != 0)
                        {
                            var errCode = 0;
                            var errMsg = "";
                            Globle.DiCompany.GetLastError(out errCode, out errMsg);
                            LogHelper.WriteLog("添加 Invoice 失败",
                                new Exception("Invoice:" + docNumber + "，错误信息:" + errMsg + "-" + errCode));
                        }
                        else
                        {
                            LogHelper.WriteLog("添加 Invoice 成功:" + docNumber);
                            invoice["ext_default_UDF34"] = string.Format("{0:u}", DateTime.Now);
                            var url = "https://app1.sapanywhere.cn/sbo/Invoice(" + invoceId + ")";
                            httpResult = AnwHelper.UpdateObjectHttpResult(url, invoice);
                        }
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(vInvoice);
                        GC.Collect();
                    }

                
               
        }
    }
}