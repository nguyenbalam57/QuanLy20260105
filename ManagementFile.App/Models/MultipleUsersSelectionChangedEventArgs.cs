using ManagementFile.App.Models.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.App.Models
{
    /// <summary>
    /// Dữ liệu sự kiện khi chọn nhiều users
    /// </summary>
    public class MultipleUsersSelectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Danh sách IDs của users được chọn
        /// </summary>
        public List<int> SelectedUserIds { get; set; } = new List<int>();

        /// <summary>
        /// Danh sách đối tượng users được chọn
        /// </summary>
        public List<UserModel> SelectedUsers { get; set; } = new List<UserModel>();

        /// <summary>
        /// Số lượng users đã chọn
        /// </summary>
        public int Count { get; set; }
    }
}