using Kingdee.BOS;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Enums;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Core.Warn.Enums;
using Kingdee.BOS.Core.Warn.Message;
using Kingdee.BOS.Core.Warn.PlugIn;
using Kingdee.BOS.Core.Warn.PlugIn.Args;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;


namespace SHU.K3.SCM.Business.PlugInEx.Warn
{
    [Description("审核预警客户端插件")]
    public class BillAuditMessagePlugln : AbstractWarnMessagePlugIn
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public override void ProcessWarnMessage(ProcessWarnMessageEventArgs e)
        {
            
            e.IsProcessByPlugin = true;//是否可重复处理
            if (e.MsgDataKeyValueList != null && e.MsgDataKeyValueList.Count<WarnMessageDataKeyValue>() > 0)
            {
                ShowPurOrderList(Context, this.ParentView, e.MsgDataKeyValueList);
            }
            base.ProcessWarnMessage(e);
        }
        /// <summary>
        /// 事前查看权限验证
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeProcessWarnMessage(BeforeProcessWarnMessageEventArgs e)
        {
            base.BeforeProcessWarnMessage(e);
            e.PermissionCheckFailedMessage =  ResManager.LoadKDString("没有"+ WarnDataSourceBusinessInfo .GetForm().Name+ "的查看权限！", "004015000011307", SubSystemType.SCM, new object[0]);
            List<FormOperation> list = (
                from x in WarnDataSourceBusinessInfo.GetForm().FormOperations
                where x.Operation == "View"
                select x).ToList<FormOperation>();
            if (list != null && list.Count > 0)
            {
                e.PermissionItemId = list.First<FormOperation>().PermissionItemId;
            }
        }

        /// <summary>
        /// 根据单据号，打开处理页面转向业务单据
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="view"></param>
        /// <param name="valueList">主键id列表</param>
        public void ShowPurOrderList(Context ctx, IDynamicFormView view, List<WarnMessageDataKeyValue> valueList)
        {
            List<string> values = (
                from p in valueList
                select p.Items[0].Value).ToList<string>();
            string filter = string.Format(" Fid in ({0})", string.Join(",", values));
            OpenForm(WarnDataSourceBusinessInfo.GetForm().Id, view, filter);
        }
        /// <summary>
        /// 打开页面
        /// </summary>
        /// <param name="formId"></param>
        /// <param name="view"></param>
        /// <param name="filter"></param>
        public  void OpenForm(string formId, IDynamicFormView view, string filter)
        {
            ListShowParameter listShowParameter = new ListShowParameter();
            ListRegularFilterParameter listRegularFilterParameter = new ListRegularFilterParameter();
            listRegularFilterParameter.Filter = filter;
            listShowParameter.Height = 600;
            listShowParameter.Width = 800;
            listShowParameter.FormId = formId;
            listShowParameter.ParentPageId = view.PageId;
            listShowParameter.PageId = SequentialGuid.NewGuid().ToString();
            listShowParameter.ListType = Convert.ToInt32(BOSEnums.Enu_ListType.List);
            listShowParameter.ListFilterParameter = listRegularFilterParameter;
            view.ShowForm(listShowParameter);
        }


    }
}
