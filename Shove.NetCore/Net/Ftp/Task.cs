using System;
using System.Collections.Generic;
using System.Collections;

namespace Shove.Net.Ftp
{
    /// <summary>
    /// FTP 任务集合
    /// </summary>
    public class Task : IEnumerable
    {
        private IList<TaskItem> Items = new List<TaskItem>();
        private System.Threading.Thread threadTaskControler = null;

        /// <summary>
        /// 同时传输的最大任务数量
        /// </summary>
        public int MaxRuningTaskNumber = 5;

        /// <summary>
        /// 当面任务总数
        /// </summary>
        public int Count
        {
            get
            {
                return Items.Count;
            }
        }

        /// <summary>
        /// 增加一条任务
        /// </summary>
        /// <param name="item"></param>
        public void Add(TaskItem item)
        {
            lock (Items)
            {
                item.Parent = this;
                Items.Add(item);
            }
        }

        /// <summary>
        /// 删除一条任务
        /// </summary>
        /// <param name="i"></param>
        public void Remove(int i)
        {
            if (i >= Count)
            {
                return;
            }

            TaskItem item = Items[i];
            Remove(item);
        }

        /// <summary>
        /// 删除一条任务
        /// </summary>
        /// <param name="item"></param>
        public void Remove(TaskItem item)
        {
            if (item.TransferStatus == Status.Connecting || item.TransferStatus == Status.Transfering)
            {
                item.Cancel();
            } 

            lock (Items)
            {
                item.Parent = null;
                Items.Remove(item);
            }
        }

        /// <summary>
        /// 任务索引器
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TaskItem this[int index]
        {
            get
            {
                if (index >= Count)
                {
                    return null;
                }

                return Items[index];
            }
        }

        /// <summary>
        /// 指示列表中的所有任务是否均已经传输结束，注意：错误、失败、暂停、发现有重名文件等等，只要是没在传输，均是传输结束。
        /// 如果需要判断传输完成并且成功，请使用 TaskItem.Finished 逐一进行判断、或使用 Task.Finished 属性判断。
        /// </summary>
        public bool Completed
        {
            get
            {
                return _Completed;
            }
        }
        private bool _Completed = false;

        /// <summary>
        /// 指示列表中的所有人物是否均已经传输成功(完成，并且成功了)，主动取消的任务，也不算成功，所以只能用于比较单纯的全部上传、全部下载，并且均要求成功的场合。
        /// </summary>
        public bool Finished
        {
            get
            {
                for (int i = 0; i < Count; i++)
                {
                    if (Items[i].TransferStatus != Status.Finished)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 取消所有任务
        /// </summary>
        public void CancelAll()
        {
            for (int i = 0; i < Count; i++)
            {
                if ((Items[i].TransferStatus == Status.Waiting) || (Items[i].TransferStatus == Status.Connecting) || (Items[i].TransferStatus == Status.Transfering))
                {
                    Items[i].Cancel();
                }
            }
        }

        /// <summary>
        /// 清除任务列表中的已经成功传输完成的任务
        /// </summary>
        public void ClearFinished()
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                if (Items[i].TransferStatus == Status.Finished)
                {
                    Remove(i);
                }
            }
        }

        /// <summary>
        /// 清除任务列表中的所有的任务
        /// </summary>
        public void ClearAll()
        {
            CancelAll();

            lock (Items)
            {
                Items.Clear();
            }
        }

        /// <summary>
        /// 迭代器
        /// </summary>
        /// <returns></returns>
        public System.Collections.IEnumerator GetEnumerator()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                yield return Items[i];
            }
        }

        /// <summary>
        /// 构造，启动任务控制线程
        /// </summary>
        /// <param name="maxRuningTaskNumber">同时工作的最大线程数[1-100, 根据网络情况选择，推荐3、4、5]</param>
        public Task(int maxRuningTaskNumber)
        {
            if ((maxRuningTaskNumber < 1) || (maxRuningTaskNumber > 100))
            {
                throw new Exception("Shove.Net.Ftp.Task 的 MaxRuningTaskNumber 只能在 1-100 之间。");
            }

            this.MaxRuningTaskNumber = maxRuningTaskNumber;

            threadTaskControler = new System.Threading.Thread(new System.Threading.ThreadStart(this.TaskControler));
            threadTaskControler.IsBackground = true;
            threadTaskControler.Start();
        }

        /// <summary>
        /// 任务控制器，当正在运行的任务没有达到 MaxRuningTaskNumber，则启动新的等待中的任务
        /// </summary>
        private void TaskControler()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(500);

                lock (Items)
                {
                    try
                    {
                        int Runing = 0;

                        for (int i = 0; i < Count; i++)
                        {
                            TaskItem item = Items[i];
                            if (item.TransferStatus == Status.Connecting || item.TransferStatus == Status.Transfering)
                            {
                                Runing++;
                            }
                        }

                        if (Runing < MaxRuningTaskNumber)
                        {
                            for (int i = 0; i < Count; i++)
                            {
                                TaskItem item = Items[i];
                                if (!item.InstructExcuted && item.TransferInstruct != Instruct.Cancel && item.TransferStatus != Status.Connecting && item.TransferStatus != Status.Transfering)
                                {
                                    item.Start();

                                    Runing++;

                                    if (Runing >= MaxRuningTaskNumber)
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        _Completed = (Runing == 0);
                    }
                    catch { }
                }
            }
        }
    }
}