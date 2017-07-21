﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Util;
using DataGridViewAutoFilter;

namespace App.View.Task
{
    public partial class frmInventor : BaseForm
    {
        BLL.BLLBase bll = new BLL.BLLBase();

        public frmInventor()
        {
            InitializeComponent();
            this.dgvMain.DataError += delegate(object sender, DataGridViewDataErrorEventArgs e) { }; 
        }

        private void toolStripButton_Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void toolStripButton_Refresh_Click(object sender, EventArgs e)
        {
            BindData();
            
        }

        private void toolStripButton_Request_Click(object sender, EventArgs e)
        {
            frmInStockTask f = new frmInStockTask();
            if (f.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //string TaskNo = this.dgvMain.SelectedRows[0].Cells["colTaskNo"].Value.ToString();
                //bll.ExecNonQuery("WCS.UpdateTaskStateByTaskNo", new DataParameter[] { new DataParameter("@State", 1), new DataParameter("@TaskNo", TaskNo) });
                this.BindData();
            }
        }

        private void toolStripButton_Cancel_Click(object sender, EventArgs e)
        {
            if (this.dgvMain.CurrentRow == null)
                return;
            if (this.dgvMain.CurrentRow.Index >= 0)
            {
                if (this.dgvMain.SelectedRows[0].Cells["colState"].Value.ToString() == "等待")
                {
                    if (DialogResult.Yes == MessageBox.Show("您确定要取消此任务吗？", "询问", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                    {
                        string TaskNo = this.dgvMain.SelectedRows[0].Cells["colTaskNo"].Value.ToString();
                        //bll.ExecNonQuery("WCS.UpdateTaskStateByTaskNo", new DataParameter[] { new DataParameter("@State", 9), new DataParameter("@TaskNo", TaskNo) });
                        DataParameter[] param = new DataParameter[] { new DataParameter("@TaskNo", TaskNo) };
                        bll.ExecNonQueryTran("WCS.Sp_TaskCancelProcess", param);
                        
                        this.BindData();
                    }
                }
                else
                {
                    MessageBox.Show("选中的状态非[等待],请确认！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
        }

        private void toolStripButton_EmptyIn_Click(object sender, EventArgs e)
        {
            frmPalletInTask f = new frmPalletInTask();
            if (f.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.BindData();
            }
        }
        private void BindData()
        {
            DataTable dt = bll.FillDataTable("WCS.SelectTask", new DataParameter[] { new DataParameter("{0}", "WCS_TASK.State in('0','1','2','3','4','5','6') and WCS_TASK.TaskType='14'") });
            bsMain.DataSource = dt;
        }

        private void frmInStock_Load(object sender, EventArgs e)
        {
            //this.BindData();
            for (int i = 0; i < this.dgvMain.Columns.Count - 1; i++)
                ((DataGridViewAutoFilterTextBoxColumn)this.dgvMain.Columns[i]).FilteringEnabled = true;
        }

        private void dgvMain_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            //if (e.Button == MouseButtons.Right)
            //{
            //    if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            //    {
            //        //若行已是选中状态就不再进行设置
            //        if (dgvMain.Rows[e.RowIndex].Selected == false)
            //        {
            //            dgvMain.ClearSelection();
            //            dgvMain.Rows[e.RowIndex].Selected = true;
            //        }
            //        //只选中一行时设置活动单元格
            //        if (dgvMain.SelectedRows.Count == 1)
            //        {
            //            dgvMain.CurrentCell = dgvMain.Rows[e.RowIndex].Cells[e.ColumnIndex];
            //        }                    
            //        //弹出操作菜单
            //        contextMenuStrip1.Show(MousePosition.X, MousePosition.Y);
            //    }
            //}
        }
        
        private void UpdatedgvMainState(string State)
        {
            if (this.dgvMain.CurrentCell != null)
            {
                BLL.BLLBase bll = new BLL.BLLBase();
                string TaskNo = this.dgvMain.Rows[this.dgvMain.CurrentCell.RowIndex].Cells["colTaskNo"].Value.ToString();
                DataParameter[] param = new DataParameter[] { new DataParameter("@TaskNo", TaskNo), new DataParameter("@State", State) };
                bll.ExecNonQueryTran("WCS.Sp_UpdateTaskState", param);
                MCP.Logger.Info("frmInventor中任何号：" + TaskNo + "手动更新为" + State);
                 
                BindData();
            }
        }

      

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            UpdatedgvMainState("6");
        }
        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            UpdatedgvMainState("7");
        }

        private void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            UpdatedgvMainState("9");
        }
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            UpdatedgvMainState("0");
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            UpdatedgvMainState("3");
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            UpdatedgvMainState("4");
        }

       



        private void frmInStock_Activated(object sender, EventArgs e)
        {
            this.BindData();
        }

        private void dgvMain_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            string TaskNo = this.dgvMain.Rows[e.RowIndex].Cells[1].Value.ToString();
            DataTable dt = bll.FillDataTable("WCS.SelectTaskDetail", new DataParameter[] { new DataParameter("{0}", string.Format("T.TaskNo='{0}'",TaskNo)) });
            bsDetail.DataSource = dt;
        }

        

       
    }
}
