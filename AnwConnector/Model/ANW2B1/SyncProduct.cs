using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnwConnector.Common;
using AnwConnector.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAPbobsCOM;

namespace AnwConnector.Model.ANW2B1
{
    internal class SyncProduct : ServiceJob
    {
        public override void TaskJob()
        {
            while (!CancelTokenSource.IsCancellationRequested)
            {
                try
                {
                    LogHelper.WriteLog("正在准备添加 Product......");
                    var parameters = new Dictionary<string, string>();
                    parameters.Add("$inlinecount", "allpages");
                    parameters.Add("$filter",
                        "(status+eq+'Active')+and+((ext_default_UDF15+eq+null)+or+(ext_default_UDF15+eq+''))");
                    //parameters.Add("$filter", "(status+eq+'Active')");
                    parameters.Add("$orderby", "code+asc");
                    parameters.Add("$top", "50");
                    parameters.Add("$skip", "0");
                    var httpResult = AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/Product", parameters);
                    var jObject = (JObject) JsonConvert.DeserializeObject(httpResult.Html);
                    var productInfos = jObject["value"].Children().ToList();
                    foreach (var productInfo in productInfos)
                    {
                        var itemCode = productInfo["code"].ToString();
                        var itemName = productInfo["name"].ToString();
                        var id = productInfo["id"].ToString();

                        httpResult = AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/Product(" + id + ")",
                            parameters);
                        var product = (JObject) JsonConvert.DeserializeObject(httpResult.Html);
                        var vendorName = product["vendor"]["name"].ToString();
                        var sql1 = "SELECT CardCode FROM OCRD WHERE CardType='S'and CardName=@Mainsupplier";
                        SqlParameter[] sqlParameters =
                        {
                            new SqlParameter("@Mainsupplier", vendorName)
                        };
                        var mainsupplier =
                            SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql1, sqlParameters)
                                .Tables[0].Rows[0]["CardCode"].ToString();
                        var productTaxClass = product["productTaxClass"];
                        var taxName = productTaxClass["description"].ToString().Replace("%", "");
                        if (!Regex.IsMatch(taxName, @"^[1-9]\d*$"))
                        {
                            continue;
                        }
                        parameters.Clear();
                        parameters.Add("$inlinecount", "allpages");
                        parameters.Add("$filter", "(product/id+eq+" + id + ")");
                        parameters.Add("$orderby", "id+asc");
                        parameters.Add("$top", "50");
                        parameters.Add("$skip", "0");
                        httpResult = AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/SKU", parameters);
                        jObject = (JObject) JsonConvert.DeserializeObject(httpResult.Html);
                        var skus = jObject["value"].Children().ToList();
                        var skuCount = skus.Count;
                        httpResult =
                            AnwHelper.PostHttpResult(
                                "https://app1.sapanywhere.cn/sbo/Product(" + id + ")/getProductPriceListRows",
                                "{\"pagingInfo\":{\"start\":0,\"size\":" + skuCount + "}}");
                        jObject = (JObject) JsonConvert.DeserializeObject(httpResult.Html);
                        var skuPriceInfos = jObject["rows"].ToList();
                        foreach (var sku in skus)
                        {
                            var skuCode = sku["code"].ToString();
                            var skuId = sku["id"].ToString();
                            if (skuCode.Length > 20)
                            {
                                continue;
                            }
                            var sql = "select 'A' from oitm where ItemCode=@ItemCode";
                            SqlParameter[] sqLparameters =
                            {
                                new SqlParameter("@ItemCode", skuCode)
                            };
                            var dataTable =
                                SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql, sqLparameters)
                                    .Tables[0];
                            if (dataTable.Rows.Count == 0)
                            {
                                var barCode = sku["barCode"].ToString();
                                var batchSerial = sku["batchSerial"].ToString();
                                var skucodeAndBarCode = sku["codeAndBarCode"].ToString();
                                var skuName = sku["name"].ToString();
                                Items oItem = Globle.DiCompany.GetBusinessObject(BoObjectTypes.oItems);
                                oItem.ItemCode = skuCode;
                                oItem.ItemName = skuName;
                                oItem.BarCode = barCode;
                                foreach (var skuPriceInfo in skuPriceInfos)
                                {
                                    if (skuPriceInfo["skuId"].ToString().Equals(skuId))
                                    {
                                        var rows = skuPriceInfo["row"].ToList();
                                        var priceAfterVat = string.IsNullOrEmpty(rows[2].ToString())
                                            ? 0
                                            : double.Parse(rows[2].ToString());
                                        var priceList = oItem.PriceList;
                                        priceList.SetCurrentLine(0);
                                        priceList.Currency = "RMB";
                                        priceList.BasePriceList = 1;
                                        priceList.Factor = 1;
                                        priceList.Price = priceAfterVat;
                                        break;
                                    }
                                }
                                oItem.InventoryUOM = product["unitOfMeasure"].ToString();
                                oItem.PurchaseUnit = product["defaultPurchaseUom"]["name"].ToString();
                                oItem.SalesUnit = product["defaultSalesUom"]["name"].ToString();
                                oItem.Mainsupplier = mainsupplier;
                                if (product["brand"].HasValues)
                                {
                                    var brand = product["brand"]["name"].ToString();
                                    oItem.UserFields.Fields.Item("U_Brand").Value = brand;
                                }

                                //var secuType = product["ext_default_UDF13"].ToString();
                                //oItem.UserFields.Fields.Item("U_SecuType").Value = secuType;

                                //var exeStand = product["ext_default_UDF12"].ToString();
                                //oItem.UserFields.Fields.Item("U_ExeStand").Value = exeStand;

                                //var fabMat = product["ext_default_UDF11"].ToString();
                                //oItem.UserFields.Fields.Item("U_FabMat").Value = fabMat;

                                //var uAge = product["ext_default_UDF14"].ToString();
                                //oItem.UserFields.Fields.Item("U_Age").Value = uAge;

                                //var season = product["ext_default_UDF7"].ToString();
                                //oItem.UserFields.Fields.Item("U_Season").Value = season;


                                //var style = product["ext_default_UDF1"].ToString();
                                //oItem.UserFields.Fields.Item("U_Style").Value = style;

                                //var colSystem = product["ext_default_UDF2"].ToString();
                                //oItem.UserFields.Fields.Item("U_ColSystem").Value = colSystem;

                                //var uFabTex = product["ext_default_UDF3"].ToString();
                                //oItem.UserFields.Fields.Item("U_FabTex").Value = uFabTex;

                                //var uLinging = product["ext_default_UDF4"].ToString();
                                //oItem.UserFields.Fields.Item("U_Linging").Value = uLinging;


                                //var uHeel = product["ext_default_UDF5"].ToString();
                                //oItem.UserFields.Fields.Item("U_Heel").Value = uHeel;


                                //var uLargBtm = product["ext_default_UDF6"].ToString();
                                //oItem.UserFields.Fields.Item("U_LargBtm").Value = uLargBtm;


                                //var uLargMat = product["ext_default_UDF7"].ToString();
                                //oItem.UserFields.Fields.Item("U_LargMat").Value = uLargMat;

                                //var uPipeHght = product["ext_default_UDF8"].ToString();
                                //oItem.UserFields.Fields.Item("U_PipeHght").Value = uPipeHght;

                                //var origItemCode = product["ext_default_UDF22"].ToString();
                                //oItem.UserFields.Fields.Item("U_OrigItemCode").Value = origItemCode;

                                //var exeStand = product["ext_default_UDF21"].ToString();
                                //oItem.UserFields.Fields.Item("U_ExeStand").Value = exeStand;

                                //var model = sku["model"].ToString();
                                //oItem.UserFields.Fields.Item("U_Size").Value = model;

                                var variantValues = sku["variantValues"].ToString().Split('/');

                                if (variantValues.Length > 1)
                                {
                                    var orgszie = variantValues[1];
                                    oItem.UserFields.Fields.Item("U_Size").Value = orgszie;
                                }

                                //var uColor = variantValues[0];
                                //oItem.UserFields.Fields.Item("U_Color").Value = uColor;


                                //var uOrigin = sku["ext_default_UDF18"].ToString();
                                //oItem.UserFields.Fields.Item("U_Origin").Value = uOrigin;

                                //var uLevel = sku["ext_default_UDF19"].ToString();
                                //oItem.UserFields.Fields.Item("U_Level").Value = uLevel;

                                //var uInterBarCode = sku["ext_default_UDF20"].ToString();
                                //oItem.UserFields.Fields.Item("U_InterBarCode").Value = uInterBarCode;


                                var itemGroupName = product["ext_default_UDF16"].ToString();

                                sql = "SELECT ItmsGrpCod FROM OITB WHERE ItmsGrpNam=@ItmsGrpNam";

                                SqlParameter[] sLparameters =
                                {
                                    new SqlParameter("@ItmsGrpNam", itemGroupName)
                                };
                                var tb =
                                    SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql,
                                        sLparameters)
                                        .Tables[0];
                                if (tb.Rows.Count > 0)
                                {
                                    var itmsGrpCod = tb.Rows[0][0].ToString();
                                    oItem.ItemsGroupCode = int.Parse(itmsGrpCod);
                                }

                                //if (batchSerial.Equals("BatchProduct", StringComparison.CurrentCultureIgnoreCase))
                                //{
                                //    oItem.ManageBatchNumbers = BoYesNoEnum.tNO;
                                //    oItem.SRIAndBatchManageMethod = BoManageMethod.bomm_OnEveryTransaction;
                                //}
                                sql = "select * from OVTG where Category='O' and Rate=" + taxName;

                                dataTable = SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql)
                                    .Tables[0];

                                if (dataTable.Rows.Count > 0)
                                {
                                    var taxCode = dataTable.Rows[0]["Code"].ToString();
                                    oItem.SalesVATGroup = taxCode;
                                }
                                oItem.UserFields.Fields.Item("U_ParentCode").Value = itemCode;

                                var add = oItem.Add();
                                jObject = (JObject) JsonConvert.DeserializeObject(product.ToString());
                                var versionNum = jObject["versionNum"].ToString();
                                jObject["versionNum"] = int.Parse(versionNum) + 1;
                                jObject["ext_default_UDF15"] = string.Format("{0:u}", DateTime.Now);
                                var url = "https://app1.sapanywhere.cn/sbo/Product(" + id + ")";
                                httpResult = AnwHelper.UpdateObjectHttpResult(url, jObject);
                                if (add != 0)
                                {
                                    var errMsg = "";
                                    var errCode = 0;
                                    Globle.DiCompany.GetLastError(out errCode, out errMsg);
                                    LogHelper.WriteLog("添加 SKU 失败",
                                        new Exception("SKU:" + skuCode + "，错误信息:" + errMsg + "-" + errCode));
                                }
                                else
                                {
                                    LogHelper.WriteLog("添加 SKU 成功:" + skuCode);
                                }
                            }
                            else
                            {
                                Items oItem = Globle.DiCompany.GetBusinessObject(BoObjectTypes.oItems);
                                if (oItem.GetByKey(skuCode))
                                {
                                    foreach (var skuPriceInfo in skuPriceInfos)
                                    {
                                        if (skuPriceInfo["skuId"].ToString().Equals(skuId))
                                        {
                                            var rows = skuPriceInfo["row"].ToList();
                                            var priceAfterVat = string.IsNullOrEmpty(rows[3].ToString())
                                                ? 0
                                                : double.Parse(rows[3].ToString());
                                            var priceList = oItem.PriceList;
                                            priceList.SetCurrentLine(0);
                                            priceList.Currency = "RMB";
                                            priceList.BasePriceList = 1;
                                            priceList.Factor = 1;
                                            priceList.Price = priceAfterVat;
                                            break;
                                        }
                                    }
                                    oItem.InventoryUOM = product["unitOfMeasure"].ToString();
                                    oItem.PurchaseUnit = product["defaultPurchaseUom"]["name"].ToString();
                                    oItem.SalesUnit = product["defaultSalesUom"]["name"].ToString();
                                    oItem.Mainsupplier = mainsupplier;

                                    if (product["brand"].HasValues)
                                    {
                                        var brand = product["brand"]["name"].ToString();
                                        oItem.UserFields.Fields.Item("U_Brand").Value = brand;
                                    }
                                    //var multiYears = product["multiYears"].ToString();
                                    //oItem.UserFields.Fields.Item("U_Production").Value = multiYears;

                                    //var secuType = product["ext_default_UDF13"].ToString();
                                    //oItem.UserFields.Fields.Item("U_SecuType").Value = secuType;

                                    //var exeStand = product["ext_default_UDF12"].ToString();
                                    //oItem.UserFields.Fields.Item("U_ExeStand").Value = exeStand;

                                    //var fabMat = product["ext_default_UDF11"].ToString();
                                    //oItem.UserFields.Fields.Item("U_FabMat").Value = fabMat;

                                    //var uAge = product["ext_default_UDF14"].ToString();
                                    //oItem.UserFields.Fields.Item("U_Age").Value = uAge;

                                    //var season = product["ext_default_UDF7"].ToString();
                                    //oItem.UserFields.Fields.Item("U_Season").Value = season;

                                    var variantValues = sku["variantValues"].ToString().Split('/');

                                    if (variantValues.Length > 1)
                                    {
                                        var orgszie = variantValues[1];
                                        oItem.UserFields.Fields.Item("U_Size").Value = orgszie;
                                    }
                                    //var uColor = variantValues[0];
                                    //oItem.UserFields.Fields.Item("U_Color").Value = uColor;

                                    var itemGroupName = product["ext_default_UDF16"].ToString();

                                    sql = "SELECT ItmsGrpCod FROM OITB WHERE ItmsGrpNam=@ItmsGrpNam";

                                    SqlParameter[] sLparameters =
                                    {
                                        new SqlParameter("@ItmsGrpNam", itemGroupName)
                                    };
                                    var tb =
                                        SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql,
                                            sLparameters)
                                            .Tables[0];
                                    if (tb.Rows.Count > 0)
                                    {
                                        var itmsGrpCod = tb.Rows[0][0].ToString();
                                        oItem.ItemsGroupCode = int.Parse(itmsGrpCod);
                                    }

                                    //var style = product["ext_default_UDF1"].ToString();
                                    //oItem.UserFields.Fields.Item("U_Style").Value = style;

                                    //var colSystem = product["ext_default_UDF2"].ToString();
                                    //oItem.UserFields.Fields.Item("U_ColSystem").Value = colSystem;

                                    //var uFabTex = product["ext_default_UDF3"].ToString();
                                    //oItem.UserFields.Fields.Item("U_FabTex").Value = uFabTex;

                                    //var uLinging = product["ext_default_UDF4"].ToString();
                                    //oItem.UserFields.Fields.Item("U_Linging").Value = uLinging;


                                    //var uHeel = product["ext_default_UDF5"].ToString();
                                    //oItem.UserFields.Fields.Item("U_Heel").Value = uHeel;


                                    //var uLargBtm = product["ext_default_UDF6"].ToString();
                                    //oItem.UserFields.Fields.Item("U_LargBtm").Value = uLargBtm;


                                    //var uLargMat = product["ext_default_UDF7"].ToString();
                                    //oItem.UserFields.Fields.Item("U_LargMat").Value = uLargMat;

                                    //var uPipeHght = product["ext_default_UDF8"].ToString();
                                    //oItem.UserFields.Fields.Item("U_PipeHght").Value = uPipeHght;

                                    //var uFabMat = product["ext_default_UDF9"].ToString();
                                    //oItem.UserFields.Fields.Item("U_FabMat").Value = uFabMat;

                                    //var origItemCode = product["ext_default_UDF22"].ToString();
                                    //oItem.UserFields.Fields.Item("U_OrigItemCode").Value = origItemCode;

                                    //var exeStand = product["ext_default_UDF21"].ToString();
                                    //oItem.UserFields.Fields.Item("U_ExeStand").Value = exeStand;

                                    //var model = sku["model"].ToString();
                                    //oItem.UserFields.Fields.Item("U_Size").Value = model;

                                    //var variantValues = sku["variantValues"].ToString().Split('/');

                                    //if (variantValues.Length > 1)
                                    //{
                                    //    var orgSize = variantValues[1];
                                    //    oItem.UserFields.Fields.Item("U_OrgSize").Value = orgSize;
                                    //}
                                    //var uColor = variantValues[0];
                                    //oItem.UserFields.Fields.Item("U_Color").Value = uColor;


                                    //var uOrigin = sku["ext_default_UDF18"].ToString();
                                    //oItem.UserFields.Fields.Item("U_Origin").Value = uOrigin;

                                    //var uLevel = sku["ext_default_UDF19"].ToString();
                                    //oItem.UserFields.Fields.Item("U_Level").Value = uLevel;

                                    //var uInterBarCode = sku["ext_default_UDF20"].ToString();
                                    //oItem.UserFields.Fields.Item("U_InterBarCode").Value = uInterBarCode;
                                    oItem.Update();
                                    jObject = (JObject) JsonConvert.DeserializeObject(product.ToString());
                                    var versionNum = jObject["versionNum"].ToString();
                                    jObject["versionNum"] = int.Parse(versionNum) + 1;
                                    jObject["ext_default_UDF15"] = string.Format("{0:u}", DateTime.Now);

                                    var url = "https://app1.sapanywhere.cn/sbo/Product(" + id + ")";
                                    httpResult = AnwHelper.UpdateObjectHttpResult(url, jObject);
                                    LogHelper.WriteLog("更新 SKU 成功:" + skuCode);
                                }
                                Marshal.ReleaseComObject(oItem);
                                GC.Collect();
                            }
                        }
                    }
                }
                catch
                    (Exception exception)
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
            if (CancelTokenSource.IsCancellationRequested)
            {
                CancelTokenSource.Cancel();
            }
        }
    }
}