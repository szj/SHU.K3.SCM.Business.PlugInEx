using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SHU.K3.SCM.Business.PlugInEx.Bill
{
    [Description("网上订单 可用量插件")]
    class CPOrderQtyQuery : AbstractBillPlugIn
    {
        private string kczz = "";
        private string ckid = "";
        private string cksx = "";
        private string t1 = "";
        private string t2 = "";
        private string t3 = "";
        private string t4 = "";
        private string t5 = "";
        private string t6 = "";
        private string t7 = "";
        private string t_scbl = "";
        private string t_wwll = "";
        private string t_wwbl = "";
        private string slid = "qty";
        private string strCheck = "";
        private Dictionary<string, DynamicObjectCollection> detamilColl = null;
        private Dictionary<string, object> billHeadColl = null;
        private string fentryName = "";
        private string formName = "";
        private DynamicObjectType entryType = null;
        private DynamicObject checkResult = null;
        private object fid = 0;
        private string state = "0";
        private List<Dictionary<string, object>> listHeads = null;
        private List<Dictionary<string, DynamicObjectCollection>> listDetamils = null;
        /// <summary>
        /// 初始化，获得fromid
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            this.formName = base.View.Model.OpenParameter.FormId;
            this.fentryName = this.ConvertEntryName(this.formName);
            this.updateId();
        }
        private void updateId()
        {
            string value = this.ObjToVar(base.View.Model.GetValue("fbillno"));
            if (!string.IsNullOrWhiteSpace(value))
            {
                this.fid = base.View.OpenParameter.PkValue;
            }
            else
            {
                this.fid = 0;
            }
        }
        private int AddEntity(DynamicObject dy, long xsid, int seq, bool state = false, bool msg = true)
        {
            object obj = dy["MaterialId_id"];
            object obj2 = null;
            DynamicObject dynamicObject = dy[this.cksx] as DynamicObject;
            int result;
            if (obj == null)
            {
                result = 0;
            }
            else
            {
                if (dynamicObject != null)
                {
                    obj2 = dynamicObject["id"];
                }
                object dwid = dy["BaseUnitId_id"];
                DynamicObjectCollection insQty = this.GetInsQty(Context, obj, xsid, this.ObjToVar(obj2), dwid);
                if (insQty.Count == 0)
                {
                    if (msg)
                    {
                        base.View.ShowErrMessage("没有找到符合条件的库存！", "", 0);
                    }
                    result = 0;
                }
                else
                {
                    this.billHeadColl["FKCQty"] = insQty.Sum((DynamicObject u) => this.ObjToDec(u["FBASEQTY"]));
                    this.billHeadColl["FMaterialIdH"] = obj;
                    this.billHeadColl["FStockOrgId"] = xsid;
                    this.billHeadColl["Fseq"] = dy["seq"];
                    decimal num = 0m;
                    foreach (DynamicObject current in insQty)
                    {
                        decimal kcsl = this.ObjToDec(current["FBASEQTY"]);
                        DynamicObjectCollection awaitQty = this.GetAwaitQty(Context, obj, xsid, this.ObjToVar(current["FStockId"]), dwid, this.fid);
                        decimal num2 = 0m;
                        object khid = "0";
                        if (awaitQty.Count > 0)
                        {
                            num2 = awaitQty.Sum((DynamicObject u) => this.ObjToDec(u["fqty"]));
                            khid = awaitQty[0]["FCustomerID"];
                        }
                        if (state)
                        {
                            this.detamilColl[this.ObjToVar(current["FStockId"])] = awaitQty;
                            DynamicObject item = this.BuildErrEntryObject(this.entryType, dy, seq, kcsl, num2, this.fid, khid, current["FStockId"]);
                            ((Collection<DynamicObject>)this.checkResult["Entry"]).Add(item);
                        }
                        num += num2;
                    }
                    this.billHeadColl["Fdfsl"] = num;
                    this.billHeadColl["FBaseUnitId"] = dy["BaseUnitId_id"];
                    seq++;
                    result = seq;
                }
            }
            return result;
        }
        public override void EntryBarItemClick(BarItemClickEventArgs e)
        {
            base.EntryBarItemClick(e);
            string text = e.BarItemKey.ToUpper();
            if (text != null)
            {
                if (text == "TBBUTTON_ALLKYLCX")
                {
                    this.updateId();
                    if (!string.IsNullOrEmpty(this.fentryName))
                    {
                        DynamicObject dynamicObject = base.View.Model.GetValue(this.kczz) as DynamicObject;
                        if (dynamicObject != null)
                        {
                            this.state = "1";
                            this.checkResult = this.GetInvCheckResultObject(Context, out this.entryType);
                            this.detamilColl = new Dictionary<string, DynamicObjectCollection>();
                            this.listHeads = new List<Dictionary<string, object>>();
                            this.listDetamils = new List<Dictionary<string, DynamicObjectCollection>>();
                            this.billHeadColl = new Dictionary<string, object>();
                            long num = Convert.ToInt64(dynamicObject["id"]);
                            object paramter = SystemParameterServiceHelper.GetParamter(Context, num, 0L, "SAL_SystemParameter", "WDS_QtyCheck", 0L);
                            this.strCheck = this.ObjToVar(paramter);
                            if (this.formName.ToUpper() == "SAL_DELIVERYNOTICE")
                            {
                                this.SelsectAllOpen(this.fentryName, num);
                            }
                        }
                    }
                }
                else if (text == "TBBUTTON_KYLCX")
                {
                    this.updateId();
                    if (!string.IsNullOrEmpty(this.fentryName))
                    {
                        DynamicObject dynamicObject2 = base.View.Model.GetValue(this.kczz) as DynamicObject;
                        if (dynamicObject2 != null)
                        {
                            this.state = "1";
                            this.checkResult = this.GetInvCheckResultObject(Context, out this.entryType);
                            this.detamilColl = new Dictionary<string, DynamicObjectCollection>();
                            this.billHeadColl = new Dictionary<string, object>();
                            long num2 = Convert.ToInt64(dynamicObject2["id"]);
                            object paramter2 = SystemParameterServiceHelper.GetParamter(Context, num2, 0L, "SAL_SystemParameter", "WDS_QtyCheck", 0L);
                            this.strCheck = this.ObjToVar(paramter2);
                            if (this.formName.ToUpper() == "SAL_SALEORDER" || this.formName.ToUpper() == "SAL_DELIVERYNOTICE" || this.formName.ToUpper() == "CP_SaleOrder")
                            {
                                this.SelsectOneOpen(this.fentryName, num2);
                            }

                        }
                    }
                }
            }
        }
        /// <summary>
        /// SelsectOneOpen
        /// </summary>
        /// <param name="fentryName"></param>
        /// <param name="xsid"></param>
        private void SelsectAllOpen(string fentryName, long xsid)
        {
            int entryRowCount = base.View.Model.GetEntryRowCount(fentryName);
            for (int i = 0; i < entryRowCount; i++)
            {
                this.billHeadColl = new Dictionary<string, object>();
                this.detamilColl = new Dictionary<string, DynamicObjectCollection>();
                DynamicObject entityDataObject = base.View.Model.GetEntityDataObject(base.View.Model.BillBusinessInfo.GetEntity(fentryName), i);
                int seq = 0;
                object c = null;
                this.AddEntity(entityDataObject, xsid, seq, true, false);
                DynamicObject dynamicObject = entityDataObject[this.cksx] as DynamicObject;
                DynamicObjectCollection entityDataObject2 = View.Model.GetEntityDataObject(base.View.Model.BillBusinessInfo.GetEntity(fentryName));
                decimal num = this.ObjToDec(entityDataObject[this.slid]);
                object maid = entityDataObject["MaterialId_id"];
                if (dynamicObject != null)
                {
                    c = dynamicObject["id"];
                    num = (
                        from u in entityDataObject2
                        where this.ObjToVar(u["MaterialId_id"]) == this.ObjToVar(maid) && this.ObjToVar(u[this.cksx + "_id"]) == this.ObjToVar(c)
                        select u).Sum((DynamicObject u) => this.ObjToDec(u[this.slid]));
                }
                else
                {
                    num = (
                        from u in entityDataObject2
                        where this.ObjToVar(u["MaterialId_id"]) == this.ObjToVar(maid)
                        select u).Sum((DynamicObject u) => this.ObjToDec(u[this.slid]));
                }
                this.billHeadColl["FDjQty"] = num;
                this.listHeads.Add(this.billHeadColl);
                this.listDetamils.Add(this.detamilColl);
            }
            if (this.checkResult != null)
            {
                DynamicObjectCollection dynamicObjectCollection = this.checkResult["Entry"] as DynamicObjectCollection;
                if (dynamicObjectCollection.Count > 0)
                {
                    IDynamicFormView myform = base.View;
                    DynamicFormShowParameter dynamicFormShowParameter = new DynamicFormShowParameter();
                    dynamicFormShowParameter.FormId=("WDS_SAL_DELIVERYNOTICEALLQTY");
                    dynamicFormShowParameter.PageId=(SequentialGuid.NewGuid().ToString());
                    dynamicFormShowParameter.CustomComplexParams.Add("selectDataObject", this.checkResult);
                    dynamicFormShowParameter.CustomComplexParams.Add("detail", this.listDetamils);
                    dynamicFormShowParameter.CustomComplexParams.Add("billHeadColl", this.listHeads);
                    dynamicFormShowParameter.OpenStyle.ShowType = ShowType.Modal;
                    dynamicFormShowParameter.Resizable=(true);
                    myform.ShowForm(dynamicFormShowParameter);
                }
            }
        }
        private void SelsectOneOpen(string fentryName, long xsid)
        {
            int entryCurrentRowIndex = base.View.Model.GetEntryCurrentRowIndex(fentryName);
            DynamicObject entityDataObject = base.View.Model.GetEntityDataObject(View.Model.BillBusinessInfo.GetEntity(fentryName), entryCurrentRowIndex);
            int seq = 0;
            object c = null;
            this.AddEntity(entityDataObject, xsid, seq, true, true);
            DynamicObject dynamicObject = entityDataObject[this.cksx] as DynamicObject;
            DynamicObjectCollection entityDataObject2 = base.View.Model.GetEntityDataObject(base.View.Model.BillBusinessInfo.GetEntity(fentryName));
            decimal num = this.ObjToDec(entityDataObject[this.slid]);
            object maid = entityDataObject["MaterialId_id"];
            if (dynamicObject != null)
            {
                c = dynamicObject["id"];
                num = (
                    from u in entityDataObject2
                    where this.ObjToVar(u["MaterialId_id"]) == this.ObjToVar(maid) && this.ObjToVar(u[this.cksx + "_id"]) == this.ObjToVar(c)
                    select u).Sum((DynamicObject u) => this.ObjToDec(u[this.slid]));
            }
            else
            {
                num = (
                    from u in entityDataObject2
                    where this.ObjToVar(u["MaterialId_id"]) == this.ObjToVar(maid)
                    select u).Sum((DynamicObject u) => this.ObjToDec(u[this.slid]));
            }
            this.billHeadColl["FDjQty"] = num;
            if (this.checkResult != null)
            {
                DynamicObjectCollection dynamicObjectCollection = this.checkResult["Entry"] as DynamicObjectCollection;
                if (dynamicObjectCollection.Count > 0)
                {
                    IDynamicFormView formview = base.View;
                    DynamicFormShowParameter dynamicFormShowParameter = new DynamicFormShowParameter();
                    dynamicFormShowParameter.FormId=("WDS_SAL_QUERYQTY");
                    dynamicFormShowParameter.PageId=(SequentialGuid.NewGuid().ToString());
                    dynamicFormShowParameter.CustomComplexParams.Add("selectDataObject", this.checkResult);
                    dynamicFormShowParameter.CustomComplexParams.Add("detail", this.detamilColl);
                    dynamicFormShowParameter.CustomComplexParams.Add("billHeadColl", this.billHeadColl);
                    dynamicFormShowParameter.OpenStyle.ShowType = ShowType.Modal ;
                    dynamicFormShowParameter.Resizable=(true);
                    formview.ShowForm(dynamicFormShowParameter);
                }
            }
        }
        private DynamicObjectCollection GetAwaitQty(Context ctx, object fmaid, object FStockOrgId, string FStockId, object dwid, object fid)
        {
            StringBuilder stringBuilder = new StringBuilder("/*dialect*/");
            string text = " and t_1.FStockId=@FStockId";
            string text2 = " and FSrcStockId=@FStockId";
            string text3 = "  and F_WDS_StockidEntry=@FStockId";
            string text4 = " order by fdate";
            stringBuilder.AppendLine(" select * from ( ");
            DynamicObjectCollection result;
            if (this.strCheck == "A")
            {
                stringBuilder.AppendFormat("\n                \n                 --销售订单的待发货数量 没有做销售出库并且销售出库已审核，未关闭，未冻结,不是暂存\n                select FDOCUMENTSTATUS,FCreatorId,FModifierId,FApproverID,t_3.FCUSTID FCustomerID,fqty- isnull(FBaseStockOutQty,0) fqty,FBillNo,'销售订单' ftype,fseq,t_1.FMATERIALID,FDATE,t_1.FENTRYID from T_SAL_ORDERENTRY t_1\n                left join T_SAL_ORDERENTRY_R t_2 on t_1.FENTRYID=t_2.FENTRYID\n                left join T_SAL_ORDER t_3 on t_1.fid=t_3.fid\n                where  t_1.FMrpCloseStatus='A' and FMrpFreezeStatus='A' and FDOCUMENTSTATUS<>'Z' and isnull(fqty,0)<>isnull(FBaseStockOutQty,0) \n\t\t\t\t{3}\n                and    t_1. FMATERIALID in (select FMATERIALID from T_BD_MATERIAL where FMASTERID=(select top 1 FMASTERID from T_BD_MATERIAL where FMATERIALID=@fmaid) )   and FSALEORGID=@FStockOrgId \n                and FBaseUnitId=@dwid  {2} \n                ", new object[]
                {
                    text,
                    text2,
                    text3,
                    this.t1
                });
                stringBuilder.AppendLine();
            }
            else
            {
                if (!(this.strCheck == "B"))
                {
                    result = null;
                    return result;
                }
                stringBuilder.AppendFormat("\n                --发货通知单，未关闭，未作废发货通知单的组织哪个,不是暂存\t\t\n\t\t\t\t  select FDOCUMENTSTATUS,FCreatorId,FModifierId,FApproverID,t_2.FCustomerID,FBaseUnitQty - isnull(FBASESUMOUTQTY,0) fqty,FBillNo,'发货通知单' ftype,fseq,t_1.FMATERIALID,FDATE,t_1.FENTRYID  from T_SAL_DELIVERYNOTICEENTRY t_1\n\t\t\t\t  left join T_SAL_DELIVERYNOTICE t_2 on t_1.fid=t_2.fid\n                where  t_1.FCLOSESTATUS='A' and FDOCUMENTSTATUS<>'Z' and FCancelStatus='A' \n\t\t\t\t{3}\n                and    t_1. FMATERIALID in (select FMATERIALID from T_BD_MATERIAL where FMASTERID=(select top 1 FMASTERID from T_BD_MATERIAL where FMATERIALID=@fmaid) )   and FDeliveryOrgID=@FStockOrgId \n                and FBaseUnitId=@dwid  and FSHIPMENTSTOCKID=@FStockId\n                ", new object[]
                {
                    text,
                    text2,
                    text3,
                    this.t7
                });
                stringBuilder.AppendLine();
            }
            stringBuilder.AppendFormat("  \n                union all\n                --其他出库单 没有审核，未作废,不是暂存\t\n\t\t\t\tselect  FDOCUMENTSTATUS,FCreatorId,FModifierId,FApproverID,t_2.FCustId FCustomerID,FBaseQty,FBillNo,'其他出库单' ftype,fseq,t_1.FMATERIALID,FDATE,t_1.FENTRYID from T_STK_MISDELIVERY t_2\n\t\t\t\tleft join T_STK_MISDELIVERYENTRY t_1 on  t_1.fid=t_2.fid\n\t\t\t\t where  FDOCUMENTSTATUS<>'C' and FCancelStatus='A' and FDOCUMENTSTATUS<>'Z'   and FStockDirect<>'RETURN'  and     FMATERIALID in (select FMATERIALID from T_BD_MATERIAL where FMASTERID=(select top 1 FMASTERID from T_BD_MATERIAL where FMATERIALID=@fmaid) )   and FStockOrgId=@FStockOrgId \n                    {4}\n                and FBaseUnitId=@dwid  {0}\n                union all\n\t            --直接调拨单 调出仓库 没有审核，未作废,不是暂存\t\n\t\t\t\tselect  FDOCUMENTSTATUS,FCreatorId,FModifierId,FApproverID, FCustID FCustomerID, FBaseQty,FBillNo,'直接调拨单' ftype,fseq,t_1.FMATERIALID,FDATE,t_1.FENTRYID  from T_STK_STKTRANSFERIN t_2\n\t\t\t\tleft join  T_STK_STKTRANSFERINENTRY t_1 on t_1.FID=t_2.FID\n\t\t\t\t where  FDOCUMENTSTATUS<>'C' and FOBJECTTYPEID='STK_TransferDirect' and FCancelStatus='A' and FDOCUMENTSTATUS<>'Z'  and     FMATERIALID in (select FMATERIALID from T_BD_MATERIAL where FMASTERID=(select top 1 FMASTERID from T_BD_MATERIAL where FMATERIALID=@fmaid) )   and FStockOutOrgId=@FStockOrgId \n                {5}                \n                and FBaseUnitId=@dwid   {1} \n                union all\n                --分步式调出单 没有审核，未作废,不是暂存\t\n\t\t\t\tselect   FDOCUMENTSTATUS,FCreatorId,FModifierId,FApproverID, FCustID FCustomerID, FBaseQty,FBillNo,'分步式调出单' ftype,fseq,t_1.FMATERIALID,FDATE,t_1.FENTRYID   from T_STK_STKTRANSFEROUT t_2\n\t\t\t\tleft join  T_STK_STKTRANSFEROUTENTRY t_1 on t_1.FID=t_2.FID\n\t\t\t\t where  FDOCUMENTSTATUS<>'C' and FCancelStatus='A' and FDOCUMENTSTATUS<>'Z' and     FMATERIALID in (select FMATERIALID from T_BD_MATERIAL where FMASTERID=(select top 1 FMASTERID from T_BD_MATERIAL where FMATERIALID=@fmaid) )   and FStockOrgID=@FStockOrgId \n                {6} and FBaseUnitId=@dwid   {1} \n                union all\n                 --生产领料单 没有审核，未作废,不是暂存\t\n                select FDOCUMENTSTATUS,FCreatorId,FModifierId,FApproverID,'', FBaseActualQty FQty,FBillNo,'生产领料单' ftype,fseq,t_1.FMATERIALID,FDATE,t_1.FENTRYID  from T_PRD_PICKMTRL t_2\n\t\t\t\tleft join  T_PRD_PICKMTRLDATA t_1 on t_1.FID=t_2.FID\n                where  FDOCUMENTSTATUS<>'C' and FCancelStatus='A' and FDOCUMENTSTATUS<>'Z' and     FMATERIALID in (select FMATERIALID from T_BD_MATERIAL where FMASTERID=(select top 1 FMASTERID from T_BD_MATERIAL where FMATERIALID=@fmaid) )   and FStockOrgID=@FStockOrgId \n                {7} and FBaseUnitId=@dwid   {0} \n                union all\n                 --简单生产领料单 没有审核，未作废,不是暂存\t\n                select FDOCUMENTSTATUS,FCreatorId,FModifierId,FApproverID,'', FBaseActualQty FQty,FBillNo,'简单生产领料单' ftype,fseq,t_1.FMATERIALID,FDATE,t_1.FENTRYID  from T_SP_PICKMTRL t_2\n\t\t\t\tleft join  T_SP_PICKMTRLDATA t_1 on t_1.FID=t_2.FID\n                where  FDOCUMENTSTATUS<>'C' and FCancelStatus='A' and FDOCUMENTSTATUS<>'Z' and     FMATERIALID in (select FMATERIALID from T_BD_MATERIAL where FMASTERID=(select top 1 FMASTERID from T_BD_MATERIAL where FMATERIALID=@fmaid) )   and FStockOrgID=@FStockOrgId \n                {8} and FBaseUnitId=@dwid   {0} \n                union all\n                --生产补料单 没有审核，未作废,不是暂存\t\n                select FDOCUMENTSTATUS,FCreatorId,FModifierId,FApproverID, '',FBaseActualQty FQty,FBillNo,'生产补料单' ftype,fseq,t_1.FMATERIALID,FDATE,t_1.FENTRYID  from T_PRD_FEEDMTRL t_2\n\t\t\t\tleft join  T_PRD_FEEDMTRLDATA t_1 on t_1.FID=t_2.FID\n\t\t\t\tleft join  T_PRD_FEEDMTRLDATA_Q t_3 on t_3.FENTRYID=t_1.FENTRYID\n                where  FDOCUMENTSTATUS<>'C' and FCancelStatus='A' and FDOCUMENTSTATUS<>'Z' and     FMATERIALID in (select FMATERIALID from T_BD_MATERIAL where FMASTERID=(select top 1 FMASTERID from T_BD_MATERIAL where FMATERIALID=@fmaid) )   and FStockOrgID=@FStockOrgId \n                {9} and FBaseUnitId=@dwid   {0} \n                union all\n                --委外领料单 没有审核，未作废,不是暂存\t\n                select FDOCUMENTSTATUS,FCreatorId,FModifierId,FApproverID,'',  FBaseActualQty FQty,FBillNo,'委外领料单' ftype,fseq,t_1.FMATERIALID,FDATE,t_1.FENTRYID  from T_SUB_PICKMTRL t_2\n\t\t\t\tleft join  T_SUB_PICKMTRLDATA t_1 on t_1.FID=t_2.FID\n                where  FDOCUMENTSTATUS<>'C' and FCancelStatus='A'  and FDOCUMENTSTATUS<>'Z' and     FMATERIALID in (select FMATERIALID from T_BD_MATERIAL where FMASTERID=(select top 1 FMASTERID from T_BD_MATERIAL where FMATERIALID=@fmaid) )   and FStockOrgID=@FStockOrgId \n                {10} and FBaseUnitId=@dwid   {0} \n                  union all\n                --委外补料单 没有审核，未作废,不是暂存\t\n                select FDOCUMENTSTATUS,FCreatorId,FModifierId,FApproverID,'', FBaseActualQty FQty,FBillNo,'委外补料单' ftype,fseq,t_1.FMATERIALID,FDATE,t_1.FENTRYID  from T_SUB_FEEDMTRL t_2\n\t\t\t\tleft join  T_SUB_FEEDMTRLENTRY t_1 on t_1.FID=t_2.FID\n\t\t\t\tleft join  T_SUB_FEEDMTRLENTRY_Q t_3 on t_3.FENTRYID=t_1.FENTRYID\n                where  FDOCUMENTSTATUS<>'C' and FCancelStatus='A' and FDOCUMENTSTATUS<>'Z' and     FMATERIALID in (select FMATERIALID from T_BD_MATERIAL where FMASTERID=(select top 1 FMASTERID from T_BD_MATERIAL where FMATERIALID=@fmaid) )   and FStockOrgID=@FStockOrgId \n                {11} and FBaseUnitId=@dwid   {0} \n                 union all\n                --组装拆卸单 事物为组装 没有审核，未作废,不是暂存\n                select FDOCUMENTSTATUS,FCreatorId,FModifierId,FApproverID,'', t_1.FBASEQTY FQty,FBillNo,'组装拆卸单' ftype,t_1.fseq,t_1.FMATERIALID,FDATE,t_1.FDETAILID  from T_STK_ASSEMBLY t_2\n\t\t\t    left join T_STK_ASSEMBLYPRODUCT t_3 on t_3.FID=t_2.fid\n\t\t\t\tleft join  T_STK_ASSEMBLYSUBITEM t_1 on t_1.FENTRYID=t_3.FENTRYID\n                where  FDOCUMENTSTATUS<>'C' and FCancelStatus='A' and FDOCUMENTSTATUS<>'Z' and FAFFAIRTYPE='Assembly' and     t_1.FMATERIALID in (select FMATERIALID from T_BD_MATERIAL where FMASTERID=(select top 1 FMASTERID from T_BD_MATERIAL where FMATERIALID=@fmaid) )   and FStockOrgID=@FStockOrgId \n                 and t_1.FBaseUnitId=@dwid   {0} \n                union all\n                --组装拆卸单 事物为拆卸 没有审核，未作废,不是暂存\n                select FDOCUMENTSTATUS,FCreatorId,FModifierId,FApproverID,'', t_1.FBASEQTY FQty,FBillNo,'组装拆卸单' ftype,t_1.fseq,t_1.FMATERIALID,FDATE,t_1.FENTRYID   from T_STK_ASSEMBLY t_2\n\t\t\t    left join T_STK_ASSEMBLYPRODUCT t_1 on t_1.FID=t_2.fid\n                where  FDOCUMENTSTATUS<>'C' and FCancelStatus='A' and FDOCUMENTSTATUS<>'Z' and FAFFAIRTYPE='Dassembly' and     t_1.FMATERIALID in (select FMATERIALID from T_BD_MATERIAL where FMASTERID=(select top 1 FMASTERID from T_BD_MATERIAL where FMATERIALID=@fmaid) )   and FStockOrgID=@FStockOrgId \n                 and t_1.FBaseUnitId=@dwid   {0} \n                )tb_1\n                ", new object[]
            {
                text,
                text2,
                text3,
                this.t1,
                this.t2,
                this.t3,
                this.t4,
                this.t5,
                this.t6,
                this.t_scbl,
                this.t_wwll,
                this.t_wwbl,
                text4
            });
            SqlParam[] array = new SqlParam[]
            {
                new SqlParam("@FStockOrgId",KDDbType.String, FStockOrgId),
                new SqlParam("@FStockId", KDDbType.String, FStockId),
                new SqlParam("@fmaid", KDDbType.String, fmaid),
                new SqlParam("@dwid", KDDbType.String, dwid),
                new SqlParam("@fid", KDDbType.String, fid)
            };
            result = DBUtils.ExecuteDynamicObject(ctx, stringBuilder.ToString(), null, null, CommandType.Text, array);
            return result;
        }
        private DynamicObjectCollection GetInsQty(Context ctx, object fmaid, object FStockOrgId, string FStockId, object dwid)
        {
            StringBuilder stringBuilder = new StringBuilder("/*dialect*/");
            stringBuilder.Append("  select FStockId,sum(FBASEQTY) FBASEQTY from  T_STK_INVENTORY t_1\n            left join t_BD_StockStatus_L t_2 on t_1.FStockStatusId=t_2.FStockStatusId and FLOCALEID='2052'\n            where isnull(t_2.FNAME,'')<>'在途'\n            and  FMATERIALID\n                in (  select FMASTERID  from T_BD_MATERIAL where  FMATERIALID=@fmaid ) and FStockOrgId=@FStockOrgId \n                and FBaseUnitId=@dwid\n                ");
            if (!string.IsNullOrEmpty(FStockId))
            {
                stringBuilder.Append(" and FStockId=@FStockId");
            }
            stringBuilder.Append(" group by FStockOrgId,FMaterialId,FStockId,FBaseUnitId");
            SqlParam[] array = new SqlParam[]
            {
                new SqlParam("@FStockOrgId", KDDbType.String, FStockOrgId),
                new SqlParam("@FStockId", KDDbType.String, FStockId),
                new SqlParam("@fmaid", KDDbType.String, fmaid),
                new SqlParam("@dwid", KDDbType.String, dwid)
            };
            return DBUtils.ExecuteDynamicObject(ctx, stringBuilder.ToString(), null, null, CommandType.Text, array);
        }
        private DynamicObject BuildErrEntryObject(DynamicObjectType dyObjType, DynamicObject srcDyObj, int seq, decimal kcsl, decimal dfsl, object fid, object khid, object kcid)
        {
            DynamicObject dynamicObject = new DynamicObject(dyObjType);
            DateTime dateTime = Convert.ToDateTime("1970-01-01");
            DateTime now = DateTime.Now;
            dynamicObject["Seq"]= seq;
            dynamicObject["BillNo"]=View.Model.GetValue("FBILLNO");
            dynamicObject["BillSeq"]= srcDyObj["SEQ"];
            dynamicObject["MaterialId_id"]= srcDyObj["MaterialId_id"];
            dynamicObject["StockId_id"]= kcid;
            dynamicObject["BaseUnitId_id"]= srcDyObj["BaseUnitId_id"];
            dynamicObject["CustomerID_id"]= khid;
            dynamicObject["fid"]= fid;
            dynamicObject["BaseQty"]= kcsl;
            dynamicObject["StockQty"]= kcsl - dfsl;
            dynamicObject["AwaitQty"]= dfsl;
            dynamicObject["BillQty"]= 0;
            dynamicObject["DiffQty"]= kcsl - dfsl;
            return dynamicObject;
        }
        private string ConvertEntryName(string formName)
        {
            string result = "";
            string text = formName.ToUpper();
            if (text != null)
            {
                if (text == "SAL_DELIVERYNOTICE")
                {
                    result = "FEntity";
                    this.kczz = "FDeliveryOrgID";
                    this.ckid = "FStockID";
                    this.cksx = "StockID";
                    this.t7 = " and  t_1.fid <>@fid  and t_1.FENTRYID<>0 ";
                    this.slid = "BaseUnitQty";
                }
                else if (text == "SAL_SALEORDER")
                {
                    result = "FSaleOrderEntry";
                    this.kczz = "FSaleOrgId";
                    this.ckid = "F_WDS_StockidEntry";
                    this.cksx = "WDS_StockidEntry";
                    this.t1 = " and  t_1.fid <>@fid  and t_1.FENTRYID<>0 ";
                }
                else if (text == "CP_SaleOrder")//网上订单
                {
                    result = "FCPOrderEntity";//分录实体id
                    this.kczz = "FSaleOrgId";//表头销售组织
                    this.ckid = "F_WDS_StockidEntry";//仓库
                    this.cksx = "WDS_StockidEntry";//仓库orm
                    this.t1 = " and  t_1.fid <>@fid  and t_1.FENTRYID<>0 ";
                }
            }
            return result;
        }
        private DynamicObject GetInvCheckResultObject(Context ctx, out DynamicObjectType entryType)
        {
            FormMetadata formMetadata = ServiceHelper.GetService<IMetaDataService>().Load(ctx, "WDS_SAL_CHECKQTY", true) as FormMetadata;
            entryType = formMetadata.BusinessInfo.GetEntryEntity("FEntity").DynamicObjectType;
            return new DynamicObject(formMetadata.BusinessInfo.GetDynamicObjectType());
        }
        private string ObjToVar(object obj)
        {
            string result;
            if (obj == null)
            {
                result = "";
            }
            else
            {
                result = obj.ToString();
            }
            return result;
        }
        private decimal ObjToDec(object obj)
        {
            decimal result;
            if (obj == null)
            {
                result = 0m;
            }
            else
            {
                result = Convert.ToDecimal(obj);
            }
            return result;
        }
    }
}
