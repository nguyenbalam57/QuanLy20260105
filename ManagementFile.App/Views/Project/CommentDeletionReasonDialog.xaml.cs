using ManagementFile.App.Models;
using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace ManagementFile.App.Views.Project
{
    /// <summary>
    /// Interaction logic for CommentDeletionReasonDialog.xaml
    /// </summary>
    public partial class CommentDeletionReasonDialog : Window, INotifyPropertyChanged
    {
        private string _deletionReason = "";
        private string _commentInfo = "";
        private TaskCommentModel _comment;

        public string DeletionReason
        {
            get => _deletionReason;
            set
            {
                _deletionReason = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanConfirm));

                // Enable/disable confirm button based on input
                if (ConfirmButton != null)
                {
                    ConfirmButton.IsEnabled = CanConfirm;
                }
            }
        }

        public string CommentInfo
        {
            get => _commentInfo;
            set
            {
                _commentInfo = value;
                OnPropertyChanged();
            }
        }

        public bool CanConfirm => !string.IsNullOrWhiteSpace(DeletionReason) && DeletionReason.Trim().Length >= 3;


        public CommentDeletionReasonDialog(TaskCommentModel comment)
        {
            InitializeComponent();

            DataContext = this;

            _comment = comment;

            if (comment != null)
            {
                CommentInfo = BuildCommentInfo(comment);
                Title = $"Xóa bình luận #{comment.Id} - Nhập lý do";
            }

            // Set initial focus and enable/disable confirm button
            Loaded += (s, e) =>
            {
                ReasonTextBox.Focus();
                ConfirmButton.IsEnabled = CanConfirm;
            };

            // Log dialog opening
            System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] User 'nguyenbalam57' opened deletion reason dialog for comment ID: {comment?.Id}");
        }

        private string BuildCommentInfo(TaskCommentModel comment)
        {
            var info = $"📋 ID: #{comment.Id}\n" +
                      $"👤 Tác giả: {comment.CreatedByName}\n" +
                      $"📅 Ngày tạo: {comment.CreatedAt:dd/MM/yyyy HH:mm:ss}\n" +
                      $"🏷️ Loại: {CommentTypeExtensions.GetDisplayName(comment.CommentType)}\n" +
                      $"⚡ Ưu tiên: {comment.Priority.GetDisplayName()}\n";

            if (!string.IsNullOrEmpty(comment.IssueTitle))
            {
                info += $"🔍 Vấn đề: {comment.IssueTitle}\n";
            }

            info += $"📝 Nội dung: ";

            if (comment.Content?.Length > 150)
            {
                info += $"{comment.Content.Substring(0, 150)}...";
            }
            else
            {
                info += comment.Content ?? "";
            }

            return info;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (CanConfirm)
            {
                var trimmedReason = DeletionReason.Trim();

                if (trimmedReason.Length < 3)
                {
                    MessageBox.Show("Lý do xóa phải có ít nhất 3 ký tự.", "Thông báo",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    ReasonTextBox.Focus();
                    return;
                }

                // Log confirmation
                System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] User 'nguyenbalam57' confirmed deletion of comment ID: {_comment?.Id} with reason: '{trimmedReason}'");

                DeletionReason = trimmedReason;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Vui lòng nhập lý do xóa bình luận (ít nhất 3 ký tự).", "Thông báo",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                ReasonTextBox.Focus();
            }
        }

        private void PredefinedReason_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && !string.IsNullOrEmpty(button.Content?.ToString()))
            {
                var selectedReason = button.Content.ToString();

                // If there's already text, ask user whether to replace or append
                if (!string.IsNullOrWhiteSpace(DeletionReason))
                {
                    var result = MessageBox.Show(
                        $"Bạn muốn thay thế lý do hiện tại bằng \"{selectedReason}\" hay thêm vào cuối?",
                        "Chọn hành động",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    switch (result)
                    {
                        case MessageBoxResult.Yes: // Replace
                            DeletionReason = selectedReason;
                            break;
                        case MessageBoxResult.No: // Append
                            DeletionReason = $"{DeletionReason.TrimEnd()}; {selectedReason}";
                            break;
                        case MessageBoxResult.Cancel: // Do nothing
                            return;
                    }
                }
                else
                {
                    DeletionReason = selectedReason;
                }

                // Set cursor to end of text
                ReasonTextBox.Focus();
                ReasonTextBox.CaretIndex = DeletionReason.Length;

                // Log predefined reason selection
                System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] User 'nguyenbalam57' selected predefined reason: '{selectedReason}'");
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Log dialog closing
            System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] User 'nguyenbalam57' closed deletion reason dialog. Result: {DialogResult}, Reason: '{DeletionReason}'");

            base.OnClosing(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
