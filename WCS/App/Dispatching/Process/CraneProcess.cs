﻿using System;
using System.Collections.Generic;
using System.Text;
using MCP;
using System.Data;
using Util;
using System.Timers;

namespace App.Dispatching.Process
{
    public class CraneProcess : AbstractProcess
    {
        private class rCrnStatus
        {
            public string TaskNo { get; set; }
            public int Status { get; set; }
            public int Action { get; set; }
            public int ErrCode { get; set; }
            public int TaskStatus { get; set; }
            public int io_flag { get; set; }

            public rCrnStatus()
            {
                TaskNo = "";
                Status = 0;
                Action = 0;
                ErrCode = 0;
                TaskStatus = 0;
                io_flag = 0;
            }
        }

        // 记录堆垛机当前状态及任务相关信息
        BLL.BLLBase bll = new BLL.BLLBase();
        private Dictionary<int, rCrnStatus> dCrnStatus = new Dictionary<int, rCrnStatus>();
        private Timer tmWorkTimer = new Timer();
        private bool blRun = false;
        private string AreaCode;


        public override void Initialize(Context context)
        {
            try
            {
                AreaCode = BLL.Server.GetAreaCode();
                //获取堆垛机信息
                DataTable dt = bll.FillDataTable("CMD.SelectCrane", new DataParameter[] { new DataParameter("{0}", "1=1") });
                for (int i = 1; i <= dt.Rows.Count; i++)
                {
                    if (!dCrnStatus.ContainsKey(i))
                    {
                        rCrnStatus crnsta = new rCrnStatus();
                        dCrnStatus.Add(i, crnsta);

                        dCrnStatus[i].TaskNo = "";
                        dCrnStatus[i].Status = int.Parse(dt.Rows[i-1]["State"].ToString());
                        dCrnStatus[i].TaskStatus = 0;
                        dCrnStatus[i].ErrCode = 0;
                        dCrnStatus[i].Action = 0;
                    }
                }

                tmWorkTimer.Interval = 3000;
                tmWorkTimer.Elapsed += new ElapsedEventHandler(tmWorker);
                

                base.Initialize(context);
            }
            catch (Exception ex)
            {
                Logger.Error("CraneProcess堆垛机初始化出错，原因：" + ex.Message);
            }
        }
        protected override void StateChanged(StateItem stateItem, IProcessDispatcher dispatcher)
        {
            //object obj = ObjectUtil.GetObject(stateItem.State);            
            //if (obj == null)
            //    return;

            switch (stateItem.ItemName)
            {
                case "CraneTaskFinished":
                    try
                    {
                        object obj = ObjectUtil.GetObject(stateItem.State);
                        string TaskFinish = obj.ToString();
                        if (TaskFinish.Equals("True") || TaskFinish.Equals("1"))
                        {
                            string TaskNo =ObjectUtil.GetObject(Context.ProcessDispatcher.WriteToService(stateItem.Name, "CraneTaskNo")).ToString();
                            if (TaskNo == "0")
                                TaskNo = "";
                            if (TaskNo.Length <= 0)
                                return;
                            //sbyte[] taskNo = new sbyte[10];
                            //更新任务状态
                            DataParameter[] param = new DataParameter[] { new DataParameter("@TaskNo", TaskNo) };
                            bll.ExecNonQueryTran("WCS.Sp_TaskProcess", param);
                           

                            //判断任务号是什么类型，如果是盘点另外处理
                            //param = new DataParameter[] { new DataParameter("{0}", string.Format("WCS_Task.TaskNo='{0}'", TaskNo)) };
                            //DataTable dt = bll.FillDataTable("WCS.SelectTask", param);

                            //string TaskType = "";
                            //string CellCode = "";
                            //string strState = "";
                            //if (dt.Rows.Count > 0)
                            //{
                            //    TaskType = dt.Rows[0]["TaskType"].ToString();
                            //    CellCode = dt.Rows[0]["CellCode"].ToString();
                            //    strState = dt.Rows[0]["State"].ToString();
                            //}
                            //string[] str = new string[3];
                            //str[0] = "6";
                            //string strValue = "";
                            //if (TaskType == "14" && strState == "4")
                            //{
                            //    while ((strValue = FormDialog.ShowDialog(str, dt)) != "")
                            //    {
                            //        if (strValue != "1")
                            //        {
                            //            //更新货位信息
                            //            bll.ExecNonQuery("WCS.UpdateErrCell", new DataParameter[] { new DataParameter("@CellCode", CellCode) });
                            //        }
                            //        bll.ExecNonQuery("WCS.UpdateTaskStateByTaskNo", new DataParameter[] { new DataParameter("@State", 5), new DataParameter("@TaskNo", TaskNo) });

                            //        //线程继续。
                            //        break;
                            //    }
                            //}
                            //清除堆垛机任务号


                            //Util.ConvertStringChar.stringToBytes("", 10).CopyTo(taskNo, 0);
                            WriteToService(stateItem.Name, "TaskNo", 0);
                            Logger.Info(stateItem.ItemName + "完成标志,任务号:" + TaskNo);



                        }
                    }
                    catch (Exception ex1)
                    {
                        Logger.Info("CraneProcess中CraneTaskFinished出错：" + ex1.Message);
                    }
                    break;
                case "Run":
                    blRun = (int)stateItem.State == 1;
                    if (blRun)
                    {
                        tmWorkTimer.Start();
                        Logger.Info("堆垛机联机");
                    }
                    else
                    {
                        tmWorkTimer.Stop();
                        Logger.Info("堆垛机脱机");
                    }
                    break;
                default:
                    break;
            }
            
            
            return;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tmWorker(object sender, ElapsedEventArgs e)
        {
            try
            {
                tmWorkTimer.Stop();

                DataTable dt = bll.FillDataTable("CMD.SelectCrane", new DataParameter[] { new DataParameter("{0}", "1=1") });
                for (int i = 1; i <= dt.Rows.Count; i++)
                {
                    if (!dCrnStatus.ContainsKey(i))
                    {
                        dCrnStatus[i].Status = int.Parse(dt.Rows[i - 1]["State"].ToString());
                    }
                }

                for (int i = 1; i <= 1; i++)
                {
                    if (dCrnStatus[i].Status != 1)
                        continue;

                    CraneTask(i);

                }
                
            }
            finally
            {
                tmWorkTimer.Start();
            }
        }
        /// <summary>
        /// 检查堆垛机入库状态
        /// </summary>
        /// <param name="piCrnNo"></param>
        /// <returns></returns>
        private bool Check_Crane_Status_IsOk(int craneNo)
        {
            try
            {
                string serviceName = "CranePLC" + craneNo;

                string plcTaskNo =  ObjectUtil.GetObject(Context.ProcessDispatcher.WriteToService(serviceName, "CraneTaskNo")).ToString();
                if (plcTaskNo == "0")
                    plcTaskNo = "";

                string craneMode = ObjectUtil.GetObject(Context.ProcessDispatcher.WriteToService(serviceName, "CraneMode")).ToString();
                object[] obj = ObjectUtil.GetObjects(Context.ProcessDispatcher.WriteToService(serviceName, "CraneAlarmCode"));
                int CraneState = int.Parse(obj[1].ToString());
                int CraneAlarmCode = int.Parse(obj[0].ToString());
                if (plcTaskNo == "" && craneMode == "1" && CraneAlarmCode == 0 && CraneState == 0 )
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return false;
            }            
        }        
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="craneNo"></param>
        private void CraneTask(int craneNo)
        {
            // 判断堆垛机的状态 自动  空闲
            //Logger.Debug("判断堆垛机" + piCrnNo.ToString() + "能否出库");
            try
            {
                
                //判断堆垛机
                if (!Check_Crane_Status_IsOk(craneNo))
                {
                    //Logger.Info("堆垛机状态不符合出库");
                    return;
                }
                //切换入库优先
                dCrnStatus[craneNo].io_flag = 1;
            }
            catch (Exception e)
            {
                Logger.Debug("Crane out 状态检查错误:" + e.Message.ToString());
                return;
            }

            string serviceName = "CranePLC" + craneNo;


            string CraneNo = "0" + craneNo.ToString();
            //获取任务，排序优先等级、任务时间
            DataParameter[] parameter = new DataParameter[] { new DataParameter("{0}", string.Format(" (WCS_Task.State='0' or WCS_Task.TaskType='14' and WCS_Task.State=5) and WCS_Task.CraneNo='{0}' ", CraneNo)) };
            DataTable dt = bll.FillDataTable("WCS.SelectTask", parameter);

            //出库
            if (dt.Rows.Count>0)
            {
                DataRow dr = dt.Rows[0];
                string TaskNo = dr["TaskNo"].ToString();
                string TaskID = dr["TaskID"].ToString();
               
                byte taskType = byte.Parse(dt.Rows[0]["TaskType"].ToString().Substring(1, 1));
                string state = dt.Rows[0]["State"].ToString();

                string fromStation = dt.Rows[0]["FromStation"].ToString();
                string toStation = dt.Rows[0]["ToStation"].ToString();
                string stationNo = dt.Rows[0]["StationNo"].ToString();

                if (taskType == 2 || (taskType == 4 && state == "0"))
                {
                    string StationLoad = ObjectUtil.GetObject(Context.ProcessDispatcher.WriteToService("CarPLC1", "HasProduct")).ToString();
                    //判断出库站台无货
                    if (StationLoad.Equals("True") || StationLoad.Equals("1"))
                    {
                        Logger.Info("站台状态不符合堆垛机出库");
                        return;
                    }
                }
                
                int[] cellAddr = new int[9];
                cellAddr[0] = 0;
                cellAddr[1] = 0;
                cellAddr[2] = 0;
                
                cellAddr[3] = byte.Parse(fromStation.Substring(3, 3));
                cellAddr[4] = byte.Parse(fromStation.Substring(6, 3));
                cellAddr[5] = byte.Parse(fromStation.Substring(0, 3));                
                cellAddr[6] = byte.Parse(toStation.Substring(3, 3));
                cellAddr[7] = byte.Parse(toStation.Substring(6, 3));
                cellAddr[8] = byte.Parse(toStation.Substring(0, 3));

                //sbyte[] taskNo = new sbyte[10];
                //Util.ConvertStringChar.stringToBytes(dr["TaskNo"].ToString(), 10).CopyTo(taskNo, 0);
                
                WriteToService(serviceName, "TaskAddress", cellAddr);
                WriteToService(serviceName, "TaskNo", dr["TaskNo"].ToString());
                if (WriteToService(serviceName, "WriteFinished", 1))
                {
                    int UpdateState =3;
                    if (taskType == 4 && state == "5")
                    {
                        UpdateState = 6;
                    }
                    //更新任务状态为执行中
                    bll.ExecNonQuery("WCS.UpdateTaskTimeByTaskNo", new DataParameter[] { new DataParameter("@State", UpdateState), new DataParameter("@TaskNo", TaskNo) });
                    bll.ExecNonQuery("WCS.UpdateBillStateByBillID", new DataParameter[] { new DataParameter("@State", 3), new DataParameter("@TaskID", TaskID) });
                }
                Logger.Info("任务:" + dr["TaskNo"].ToString() + "已下发给" + craneNo + "堆垛机;起始地址:" + fromStation + ",目标地址:" + toStation);
            }
        }
        
    }
}