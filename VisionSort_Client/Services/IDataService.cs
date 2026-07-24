using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisionSort_Client.Models;

namespace VisionSort_Client.Services
{
    public interface IDataService
    {
        // 保存一条检测记录
        void SaveRecord(InspectionRecord record);

        // 新增：异步获取所有记录（按时间倒序，最新的在最上面）
        Task<List<InspectionRecord>> GetAllRecordsAsync();
    }
}
