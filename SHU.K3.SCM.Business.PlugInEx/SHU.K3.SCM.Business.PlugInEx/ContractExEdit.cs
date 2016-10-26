using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.CRM.Core;
using Kingdee.K3.CRM.OPP.Business.PlugIn;
using Kingdee.K3.SCM.Business;
using Kingdee.K3.SCM.Common.BusinessEntity.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;

namespace SHU.K3.SCM.Business.PlugInEx.Bill
{
    [Description("销售合同支持变更单据插件")]
    public class ContractExEdit : ContractEdit
    {
        private bool isContractChange = false;
        private Kingdee.BOS.Orm.DataEntity.DynamicObject OldData;
        /// <summary>
        /// 菜单操作查看版本号变更记录
        /// </summary>
        /// <param name="e"></param>
        public override void AfterDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterDoOperationEventArgs e)
        {
            string a;
            if ((a = e.Operation.Operation.ToUpperInvariant()) != null)
            {
                if ((a == "CONTRACTCHANGE"))//变更操作
                {
                    if (!e.OperationResult.IsSuccess)//失败
                    {
                        return;
                    }
                    //参数初始化
                    this.isContractChange = true;
                    this.OldData = (OrmUtils.Clone(base.View.Model.DataObject, false, false) as Kingdee.BOS.Orm.DataEntity.DynamicObject);
                }
                else if (a == "CONTRACTCHANGEVIEW")//查历史
                {
                    if (!e.OperationResult.IsSuccess)
                    {
                        return;
                    }
                    string fid = Convert.ToString(base.View.Model.GetPKValue());
                    if (fid.IsNullOrEmptyOrWhiteSpace())
                    {
                        return;
                    }
                    Kingdee.BOS.Core.DynamicForm.DynamicFormShowParameter para = new Kingdee.BOS.Core.DynamicForm.DynamicFormShowParameter();
                    para.CustomParams.Add("FId", fid);
                    para.CustomComplexParams.Add("OrderBusinessInfo", base.View.Model.BusinessInfo);
                    para.CustomParams.Add("FormId", this.Model.BusinessInfo.GetForm().Id);  // "SHU_WDS_CONTRACT"
                    para.OpenStyle.ShowType = Kingdee.BOS.Core.DynamicForm.ShowType.Default;
                    para.FormId = "SCM_OrderChangeView";
                    para.PageId = Kingdee.BOS.Util.SequentialGuid.NewGuid().ToString();
                    para.OpenStyle.ShowType = Kingdee.BOS.Core.DynamicForm.ShowType.NewTabPage;
                    para.OpenStyle.TagetKey = "FMainTab";
                    Kingdee.BOS.Core.DynamicForm.IDynamicFormView mainView = base.View.GetView(base.Context.ConsolePageId);
                    if (mainView != null)
                    {
                        mainView.ShowForm(para);
                        base.View.SendDynamicFormAction(mainView);
                        return;
                    }
                }
            }
            base.AfterDoOperation(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ContractData"></param>
        /// <returns></returns>
        private OrderVersion SetContractVersionObjectForSave(Kingdee.BOS.Orm.DataEntity.DynamicObject ContractData)
        {
            return new OrderVersion
            {
                FID = Convert.ToInt64(Convert.ToString(ContractData["Id"]) + Convert.ToString(ContractData["FVersionNo"])),
                OldFID = Convert.ToInt64(ContractData["Id"]),
                FormId = this.Model.BusinessInfo.GetForm().Id,   //"SHU_WDS_CONTRACT"
                BillNo = Convert.ToString(ContractData["FBILLNO"]),
                Version = Convert.ToString(ContractData["FVersionNo"]),
                ChangerId = Convert.ToInt64(ContractData["FCHANGERID_Id"]),
                ChangeReason = Convert.ToString(ContractData["FChangeReason"]),
                ChangeDate = (ContractData["FCHANGEDATE"] as DateTime?),
                JsonData = SCMCommon.SerializeDynamicObjectToJsonString(this.Model.BusinessInfo, ContractData, null)
            };
        }

        /// <summary>执行变更历史版本的保存
		/// 变更保存
		/// </summary>
		private void DoContractChangeVersionDataSave()
        {
            List<OrderVersion> listOrderVersion = new List<OrderVersion>();
            if (Convert.ToString(OldData["FVersionNo"]).Equals("000"))
            {
                OrderVersion OldVer = this.SetContractVersionObjectForSave(OldData);
                listOrderVersion.Add(OldVer);
            }
            OrderVersion CurrVer = this.SetContractVersionObjectForSave(base.View.Model.DataObject);
            listOrderVersion.Add(CurrVer);
            Kingdee.K3.SCM.ServiceHelper.CommonServiceHelper.SaveOrderChangeVersionData(base.Context, listOrderVersion);
        }


        /// <summary>
        /// 保存之后回写变更历史
        /// </summary>
        /// <param name="e"></param>
        public override void AfterSave(Kingdee.BOS.Core.Bill.PlugIn.Args.AfterSaveEventArgs e)
        {
            this.InvokePluginMethod("AfterSave", e);
            if (base.View.OpenParameter.Status == OperationStatus.ADDNEW)
            {
                if (base.View.BusinessInfo.GetEntryEntity("FCRMAllocation") == null || base.View.BusinessInfo.GetEntryEntity("FCRMAllocation").Fields == null)
                {
                    return;
                }
                this.CreateAllocations();
            }
            GetAllocationData();

            if (!Convert.ToBoolean(base.View.Model.GetValue("FHAVEENTRY")))
            {
                base.View.GetControl<Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel.TabControl>("FTab1").SelectedTabItemKey = "FTAB1_P1";
            }
            if (!e.OperationResult.IsSuccess || !this.isContractChange)
            {
                return;
            }
            this.DoContractChangeVersionDataSave();
            this.isContractChange = false;
            base.View.GetControl<Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel.FieldEditor>("FIsFree").Enabled = false;
        }
        /// <summary>
        /// 获取协作信息
        /// </summary>
        private void GetAllocationData()
        {
            base.View.Model.BeginIniti();
            base.View.Model.DeleteEntryData("FCRMAllocation");
            DynamicObject dyAllocation = CRMAllocationHelper.GetAllocations(base.Context, base.View.BusinessInfo.GetForm().Id, this.Model.DataObject["Id"].ToString());
            if (!dyAllocation.IsNullOrEmpty())
            {
                foreach (DynamicObject dyn in ((DynamicObjectCollection)dyAllocation["FCRMAllocation"]))
                {
                    base.View.Model.CreateNewEntryRow("FCRMAllocation");
                    int index = base.View.Model.GetEntryRowCount("FCRMAllocation") - 1;
                    foreach (Field field in base.View.BusinessInfo.GetEntryEntity("FCRMAllocation").Fields)
                    {
                        base.View.Model.SetValue(field.Key, dyn[field.Key], index);
                    }
                }
            }
            base.View.UpdateView("FCRMAllocation");
            base.View.Model.EndIniti();
            base.View.GetControl<EntryGrid>("FCRMAllocation").Enabled = false;
        }


        /// <summary>
        /// 锁定是否信贷
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            //当存在下游单据
            if (base.View.OpenParameter.Status != OperationStatus.ADDNEW)
            {
                StringBuilder stringBuilder = new StringBuilder();
                string fid = Convert.ToString(base.View.Model.GetPKValue());
                if (fid.IsNullOrEmptyOrWhiteSpace())
                {
                    return;
                }
                stringBuilder.AppendLine(string.Format("SELECT FFIRSTFORMID FROM t_BF_Instance where FFIRSTFORMID='WDS_CONTRACT' and FFIRSTBILLID={0}", fid));
                //DataSet dataSet = DBServiceHelper.ExecuteDataSet(Context, stringBuilder.ToString());
                //当存在时
                using (IDataReader reader = DBServiceHelper.ExecuteReader(Context, stringBuilder.ToString()))
                {
                    if (reader.Read())
                    {
                        base.View.LockField("F_WDS_Iscredit", false);//存在下游单据不允许修改
                        //base.View.GetControl("F_WDS_Iscredit").Enabled = false;
                        //list.Add(Convert.ToString(dataReader["FTABLENUMBER"]));
                    }
                    reader.Close();
                }
            }
        }

    }
}
