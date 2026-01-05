using System;

namespace ManagementFile.App.ViewModels.Project
{
    /// <summary>
    /// Arguments cho event close dialog - Unified version
    /// </summary>
    public class DialogCloseEventArgs : EventArgs
    {
        /// <summary>
        /// Kết quả dialog (true = OK/Save, false = Cancel)
        /// </summary>
        public bool DialogResult { get; }
        
        /// <summary>
        /// Backward compatibility với AddEditProjectDialogViewModel
        /// </summary>
        public bool Result => DialogResult;

        /// <summary>
        /// Tiêu đề dialog
        /// </summary>
        public string Title => DialogResult ? "Completed" : "Cancelled";

        /// <summary>
        /// Hiển thị trạng thái
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Dữ liệu bổ sung (optional)
        /// </summary>
        public object Data { get; }

        public DialogCloseEventArgs(bool dialogResult, string reason = null, object data = null)
        {
            DialogResult = dialogResult;
            Reason = reason;
            Data = data;
        }
    }
}