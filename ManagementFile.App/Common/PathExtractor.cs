using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ManagementFile.App.Common
{
    /// <summary>
    /// Class để phân tích text và trích xuất đường dẫn thư mục
    /// </summary>
    public class PathExtractor
    {
        /// <summary>
        /// Phân tích text và lấy ra tất cả các đường dẫn thư mục
        /// </summary>
        /// <param name="text">Đoạn text cần phân tích</param>
        /// <returns>Danh sách các đường dẫn được tìm thấy</returns>
        public static List<string> ExtractPaths(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            var paths = new List<string>();

            // Pattern 1: Đường dẫn Windows (C:\... hoặc \\...)
            string windowsPathPattern = @"(?:[A-Za-z]:\\|\\\\)(?:[^\s\\/:*?""<>|\r\n]+\\)*[^\s\\/:*?""<>|\r\n]*";

            // Pattern 2: Đường dẫn Unix/Linux (/home/... hoặc ~/...)
            string unixPathPattern = @"(?:~/|/)[^\s:*?""<>|\r\n]+";

            // Tìm tất cả đường dẫn Windows
            MatchCollection windowsMatches = Regex.Matches(text, windowsPathPattern);
            foreach (Match match in windowsMatches)
            {
                string path = match.Value.TrimEnd('\\', '.', ',', ';');
                if (!string.IsNullOrWhiteSpace(path))
                {
                    paths.Add(path);
                }
            }

            // Tìm tất cả đường dẫn Unix/Linux
            MatchCollection unixMatches = Regex.Matches(text, unixPathPattern);
            foreach (Match match in unixMatches)
            {
                string path = match.Value.TrimEnd('/', '.', ',', ';');
                if (!string.IsNullOrWhiteSpace(path))
                {
                    paths.Add(path);
                }
            }

            // Loại bỏ trùng lặp và sắp xếp
            return paths.Distinct().OrderBy(p => p).ToList();
        }

        /// <summary>
        /// Phân tích text và lấy các đường dẫn hợp lệ (tồn tại trên hệ thống)
        /// </summary>
        /// <param name="text">Đoạn text cần phân tích</param>
        /// <returns>Danh sách các đường dẫn hợp lệ</returns>
        public static List<string> ExtractValidPaths(string text)
        {
            var allPaths = ExtractPaths(text);
            var validPaths = new List<string>();

            foreach (var path in allPaths)
            {
                try
                {
                    if (Directory.Exists(path) || File.Exists(path))
                    {
                        validPaths.Add(path);
                    }
                }
                catch
                {
                    // Bỏ qua các đường dẫn không hợp lệ
                }
            }

            return validPaths;
        }

        /// <summary>
        /// Phân tích text và trả về thông tin chi tiết về các đường dẫn
        /// </summary>
        public static List<PathInfo> ExtractPathsWithDetails(string text)
        {
            var paths = ExtractPaths(text);
            var pathInfos = new List<PathInfo>();

            foreach (var path in paths)
            {
                var info = new PathInfo
                {
                    OriginalPath = path,
                    IsValid = Directory.Exists(path) || File.Exists(path),
                    IsDirectory = Directory.Exists(path),
                    IsFile = File.Exists(path),
                    NormalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar)
                };

                if (info.IsValid)
                {
                    try
                    {
                        if (info.IsDirectory)
                        {
                            var dirInfo = new DirectoryInfo(path);
                            info.Name = dirInfo.Name;
                            info.ParentPath = dirInfo.Parent?.FullName;
                        }
                        else if (info.IsFile)
                        {
                            var fileInfo = new FileInfo(path);
                            info.Name = fileInfo.Name;
                            info.ParentPath = fileInfo.Directory?.FullName;
                        }
                    }
                    catch (Exception ex)
                    {
                        info.ErrorMessage = ex.Message;
                    }
                }

                pathInfos.Add(info);
            }

            return pathInfos;
        }
    }

    /// <summary>
    /// Class chứa thông tin chi tiết về đường dẫn
    /// </summary>
    public class PathInfo
    {
        public string OriginalPath { get; set; }
        public string NormalizedPath { get; set; }
        public bool IsValid { get; set; }
        public bool IsDirectory { get; set; }
        public bool IsFile { get; set; }
        public string Name { get; set; }
        public string ParentPath { get; set; }
        public string ErrorMessage { get; set; }

        public override string ToString()
        {
            return $"Path: {OriginalPath} | Valid: {IsValid} | Type: {(IsDirectory ? "Directory" : IsFile ? "File" : "Unknown")}";
        }
    }
}
