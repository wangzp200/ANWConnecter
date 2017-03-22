using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnwConnector.Common;
using AnwConnector.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAPbobsCOM;

namespace AnwConnector.Model.ANW2B1
{
    internal class SyncCustomer : ServiceJob
    {
        public override void TaskJob()
        {
            while (!CancelTokenSource.IsCancellationRequested)
            {
                var parameters = new Dictionary<string, string>();
                parameters.Clear();
                parameters.Add("$inlinecount", "allpages");
                parameters.Add("$filter", "status+eq+'ACTIVE'");
                parameters.Add("$select",
                    "lastName,creationTime,displayName,customerGroup,customerCode,+creatorDisplayName,updateTime,firstName,customerType,stage,updatorDisplayName,ownerDisplayName,id,email,status");

                parameters.Add("$orderby", "id+desc");
                parameters.Add("$top", "50");
                parameters.Add("$skip", "0");
                var httpResult = AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/Customer", parameters);
                var jObject = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                var customerInfos = jObject["value"].Children().ToList();
                foreach (var customerInfo in customerInfos)
                {
                    var id = customerInfo["id"].ToString();
                    httpResult = AnwHelper.GetHttpResult("https://app1.sapanywhere.cn/sbo/Customer(" + id + ")",
                        null);
                    var customer = (JObject)JsonConvert.DeserializeObject(httpResult.Html);
                    var customerCode = customer["customerCode"].ToString();
                    var displayName = customer["displayName"].ToString();
                    var customerGroup = customer["customerGroup"].ToString();
                    var discount = customer["ext_default_UDF6"].ToString();
                    var email = customer["email"].ToString();
                    var fax = customer["fax"].ToString();
                    var phone = customer["phone"].ToString();
                    var mobile = customer["mobile"].ToString();
                    var remarks = customer["remarks"].ToString();
                    var webSite = customer["webSite"].ToString();
                    var newcustomerCode = GetNewCustomerCode(customerCode);
                    var sql = "select 'A' from OCRD where CardCode=@CardCode";
                    SqlParameter[] sqLparameters =
                {
                    new SqlParameter("@CardCode", newcustomerCode)
                };
                    var dataTable =
                        SqlHelper.ExecuteDataset(SqlHelper.GetConnSting(), CommandType.Text, sql, sqLparameters)
                            .Tables[0];
                    if (dataTable.Rows.Count == 0)
                    {
                        BusinessPartners businessPartners =
                            Globle.DiCompany.GetBusinessObject(BoObjectTypes.oBusinessPartners);
                        businessPartners.CardType = BoCardTypes.cCustomer;
                        businessPartners.CardCode = newcustomerCode;
                        businessPartners.CardName = displayName;
                        businessPartners.EmailAddress = email;
                        businessPartners.Fax = fax;
                        businessPartners.Phone1 = phone;
                        businessPartners.Phone2 = mobile;
                        businessPartners.FreeText = remarks;
                        businessPartners.Website = webSite;
                        businessPartners.UserFields.Fields.Item("U_Discount").Value = discount;
                        //businessPartners.UserFields.Fields.Item("U_anwcustcode").Value = customerCode;
                        var add = businessPartners.Add();
                        if (add != 0)
                        {
                            var errCode = 0;
                            var errMsg = "";
                            Globle.DiCompany.GetLastError(out errCode, out errMsg);
                        }
                        else
                        {
                            jObject = (JObject)JsonConvert.DeserializeObject(customer.ToString());
                            jObject["ext_default_UDF5"] = string.Format("{0:u}", DateTime.Now);
                            var url = "https://app1.sapanywhere.cn/sbo/Customer(" + id + ")";
                            httpResult = AnwHelper.UpdateObjectHttpResult(url, jObject);
                            LogHelper.WriteLog("更新 Customer 成功:" + customerCode);
                        }
                    }
                }
                Thread.Sleep(SleepTime);
            }
        }

        private static string GetNewCustomerCode(string customerCode)
        {
            var len = customerCode.Length;
            for (var i = 0; i < 8 - len; i++)
            {
                customerCode = "0" + customerCode;
            }
            return "C" + customerCode;
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