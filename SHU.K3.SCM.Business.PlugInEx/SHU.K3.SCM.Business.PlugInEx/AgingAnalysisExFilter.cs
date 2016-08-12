using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.FIN.AR.Report.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SHU.K3.SCM.Business.PlugInEx.Report
{
    [Description("客户利息计算插件")]
    public  class AgingAnalysisExFilter : AgingAnalysisFilter
    {
        /// <summary>
        /// 值变更事件
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key.EqualsIgnoreCase("FRateScheme"))// 计息方案
            {
                bool flag = Convert.ToBoolean(e.NewValue);
                if (flag)
                {
                    SetEntAgingGrpSetting();
                }
            }
        }
        /// <summary>
        /// 处理分录信息
        /// </summary>
        public void SetEntAgingGrpSetting() {
            DynamicObject rateScheme = this.View.Model.GetValue("FRateScheme") as DynamicObject;//方案内码
            
            if (!rateScheme["id"].IsNullOrEmpty())
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(string.Format("select fday,FSection,frate from SHU_T_RateSchemeEntry where fid={0}", rateScheme["id"]));
                DataSet dataSet = DBServiceHelper.ExecuteDataSet(Context, stringBuilder.ToString());
                this.View.Model.BeginIniti();
                List<KeyValuePair<int, string>> list = new List<KeyValuePair<int, string>>();
                if (dataSet != null && dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
                {
                    for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
                    {
                        int key = Convert.ToInt32(dataSet.Tables[0].Rows[i]["fday"]);
                        String val = dataSet.Tables[0].Rows[i]["FSection"] + "|" + dataSet.Tables[0].Rows[i]["FRate"];
                        list.Add(new KeyValuePair<int, string>(key, val));
                    }
                }
                
                //==================================
                this.View.Model.DeleteEntryData("FEntAgingGrpSetting");
                foreach (KeyValuePair<int, string> current in list)
                {
                    this.View.Model.CreateNewEntryRow("FEntAgingGrpSetting");
                    int row = this.View.Model.GetEntryRowCount("FEntAgingGrpSetting") - 1;
                    string[] sectionArr = current.Value.Split('|');
                    this.View.Model.SetValue("FSection", sectionArr[0], row);//
                    this.View.Model.SetValue("FDays", current.Key, row);
                    this.View.Model.SetValue("FRate", Convert.ToDecimal(sectionArr[1]), row);//设置利率
                }
                //===================================
                this.View.UpdateView("FEntAgingGrpSetting");
                this.View.Model.EndIniti();
                
            }
        }

        /// <summary>
        /// 分录默认构建
        /// </summary>
        /// <param name="e"></param>
        public override void AfterCreateNewData(EventArgs e)
        {
            //base.AfterCreateNewData(e);
            this.InvokePluginMethod("AfterCreateNewData", e);
            this.View.Model.BeginIniti();
            List<KeyValuePair<int, string>> list = new List<KeyValuePair<int, string>>();
            list.Add(new KeyValuePair<int, string>(30, ResManager.LoadKDString("0-30天", "003227000003148", SubSystemType.FIN, new object[0])));
            list.Add(new KeyValuePair<int, string>(60, ResManager.LoadKDString("31-60天", "003227000003151", SubSystemType.FIN, new object[0])));
            list.Add(new KeyValuePair<int, string>(90, ResManager.LoadKDString("61-90天", "003227000003154", SubSystemType.FIN, new object[0])));
            list.Add(new KeyValuePair<int, string>(0, ResManager.LoadKDString("90天以上", "003227000003157", SubSystemType.FIN, new object[0])));
            this.View.Model.DeleteEntryData("FEntAgingGrpSetting");
            foreach (KeyValuePair<int, string> current in list)
            {
                this.View.Model.CreateNewEntryRow("FEntAgingGrpSetting");
                int row = this.View.Model.GetEntryRowCount("FEntAgingGrpSetting") - 1;
                this.View.Model.SetValue("FSection", current.Value, row);
                this.View.Model.SetValue("FDays", current.Key, row);
                this.View.Model.SetValue("FRate", 1, row);
            }
            this.View.Model.EndIniti();
        }

    }
}
