using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ManagementFile.Models.BaseModels
{
    /// <summary>
    /// MetadataEntity - Thực thể có metadata và tags
    /// Kế thừa từ AuditableEntity và implement IHasMetadata, IHasTags
    /// </summary>
    public abstract class MetadataEntity : AuditableEntity, IHasMetadata, IHasTags
    {
        /// <summary>
        /// Metadata - Dữ liệu metadata dạng JSON
        /// Lưu trữ thông tin bổ sung không có trong schema cố định
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Metadata { get; set; } = "{}";

        /// <summary>
        /// Tags - Danh sách tags dạng JSON array
        /// Dùng cho categorization và filtering
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Tags { get; set; } = "[]";

        /// <summary>
        /// GetMetadata - Deserialize metadata thành object
        /// </summary>
        public virtual T GetMetadata<T>() where T : class, new()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Metadata) || Metadata == "{}")
                    return new T();

                return JsonSerializer.Deserialize<T>(Metadata, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new T();
            }
            catch
            {
                return new T();
            }
        }

        /// <summary>
        /// SetMetadata - Serialize object thành metadata JSON
        /// </summary>
        public virtual void SetMetadata<T>(T data) where T : class
        {
            try
            {
                Metadata = data == null ? "{}" : JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });
            }
            catch
            {
                Metadata = "{}";
            }
        }

        /// <summary>
        /// GetTags - Lấy danh sách tags
        /// </summary>
        public virtual List<string> GetTags()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Tags) || Tags == "[]")
                    return new List<string>();

                return JsonSerializer.Deserialize<List<string>>(Tags) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// SetTags - Set danh sách tags
        /// </summary>
        public virtual void SetTags(List<string> tags)
        {
            try
            {
                var cleanTags = tags?.Where(t => !string.IsNullOrWhiteSpace(t))
                                   .Select(t => t.Trim().ToLowerInvariant())
                                   .Distinct()
                                   .ToList() ?? new List<string>();

                Tags = JsonSerializer.Serialize(cleanTags);
            }
            catch
            {
                Tags = "[]";
            }
        }

        /// <summary>
        /// AddTag - Thêm tag mới
        /// </summary>
        public virtual void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;

            var currentTags = GetTags();
            var cleanTag = tag.Trim().ToLowerInvariant();

            if (!currentTags.Contains(cleanTag, StringComparer.OrdinalIgnoreCase))
            {
                currentTags.Add(cleanTag);
                SetTags(currentTags);
            }
        }

        /// <summary>
        /// RemoveTag - Xóa tag
        /// </summary>
        public virtual void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;

            var currentTags = GetTags();
            var cleanTag = tag.Trim().ToLowerInvariant();

            currentTags.RemoveAll(t => string.Equals(t, cleanTag, StringComparison.OrdinalIgnoreCase));
            SetTags(currentTags);
        }

        /// <summary>
        /// HasTag - Kiểm tra có tag không
        /// </summary>
        public virtual bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;

            var currentTags = GetTags();
            var cleanTag = tag.Trim().ToLowerInvariant();

            return currentTags.Any(t => string.Equals(t, cleanTag, StringComparison.OrdinalIgnoreCase));
        }
    }
}
