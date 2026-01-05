using ManagementFile.Contracts.DTOs.ProjectManagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManagementFile.App.Models.Projects
{
    /// <summary>
    /// Extension methods for ProjectDto and ProjectModel conversions
    /// Provides fluent API for easy conversion
    /// </summary>
    public static class ProjectModelExtensions
    {
        /// <summary>
        /// Extension method: Convert ProjectDto to ProjectModel
        /// Usage: dto.ToProjectModel()
        /// </summary>
        public static ProjectModel ToProjectModel(this ProjectDto dto)
        {
            return ProjectModel.FromDto(dto);
        }

        /// <summary>
        /// Extension method: Convert ProjectModel to ProjectDto
        /// Usage: model.ToProjectDto()
        /// </summary>
        public static ProjectDto ToProjectDto(this ProjectModel model)
        {
            return ProjectModel.ToDto(model);
        }

        /// <summary>
        /// Extension method: Convert list of ProjectDto to list of ProjectModel
        /// Usage: dtos.ToProjectModels()
        /// </summary>
        public static List<ProjectModel> ToProjectModels(this IEnumerable<ProjectDto> dtos)
        {
            return ProjectModel.FromDtoList(dtos);
        }

        /// <summary>
        /// Extension method: Convert list of ProjectModel to list of ProjectDto
        /// Usage: models.ToProjectDtos()
        /// </summary>
        public static List<ProjectDto> ToProjectDtos(this IEnumerable<ProjectModel> models)
        {
            return ProjectModel.ToDtoList(models);
        }

        /// <summary>
        /// Extension method: Convert and build hierarchy from ProjectDto list
        /// Usage: dtos.ToProjectModelsWithHierarchy()
        /// </summary>
        public static List<ProjectModel> ToProjectModelsWithHierarchy(this IEnumerable<ProjectDto> dtos)
        {
            return ProjectModel.FromDtoListWithHierarchy(dtos);
        }

        /// <summary>
        /// Extension method: Update existing model from DTO
        /// Usage: model.UpdateFromDto(dto)
        /// </summary>
        public static void UpdateFromDto(this ProjectModel model, ProjectDto dto)
        {
            ProjectModel.UpdateFromDto(model, dto);
        }

        /// <summary>
        /// Check if ProjectDto has children
        /// </summary>
        public static bool HasChildren(this ProjectDto dto)
        {
            return dto?.Children != null && dto.Children.Count > 0;
        }

        /// <summary>
        /// Get all descendant DTOs recursively
        /// </summary>
        public static List<ProjectDto> GetAllDescendants(this ProjectDto dto)
        {
            var descendants = new List<ProjectDto>();

            if (dto?.Children == null || dto.Children.Count == 0)
                return descendants;

            foreach (var child in dto.Children)
            {
                descendants.Add(child);
                descendants.AddRange(child.GetAllDescendants());
            }

            return descendants;
        }

        /// <summary>
        /// Flatten hierarchy to single list
        /// </summary>
        public static List<ProjectDto> FlattenHierarchy(this IEnumerable<ProjectDto> dtos)
        {
            var result = new List<ProjectDto>();

            if (dtos == null) return result;

            foreach (var dto in dtos)
            {
                result.Add(dto);
                if (dto.HasChildren())
                {
                    result.AddRange(dto.Children.FlattenHierarchy());
                }
            }

            return result;
        }

        /// <summary>
        /// Count total projects including nested children
        /// </summary>
        public static int CountTotal(this IEnumerable<ProjectDto> dtos)
        {
            if (dtos == null) return 0;

            var count = 0;
            foreach (var dto in dtos)
            {
                count++; // Count self
                if (dto.HasChildren())
                {
                    count += dto.Children.CountTotal(); // Count descendants
                }
            }

            return count;
        }

        /// <summary>
        /// Get maximum depth of hierarchy tree
        /// </summary>
        public static int GetMaxDepth(this ProjectDto dto)
        {
            if (dto == null || !dto.HasChildren())
                return 0;

            var maxChildDepth = 0;
            foreach (var child in dto.Children)
            {
                var childDepth = child.GetMaxDepth();
                if (childDepth > maxChildDepth)
                    maxChildDepth = childDepth;
            }

            return maxChildDepth + 1;
        }

        /// <summary>
        /// Find ProjectDto by ID recursively
        /// </summary>
        public static ProjectDto FindById(this IEnumerable<ProjectDto> dtos, int id)
        {
            if (dtos == null) return null;

            foreach (var dto in dtos)
            {
                if (dto.Id == id)
                    return dto;

                if (dto.HasChildren())
                {
                    var found = dto.Children.FindById(id);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Filter projects by status
        /// </summary>
        public static List<ProjectDto> FilterByStatus(
            this IEnumerable<ProjectDto> dtos, 
            ManagementFile.Contracts.Enums.ProjectStatus status)
        {
            if (dtos == null) return new List<ProjectDto>();

            return dtos.Where(d => d.Status == status).ToList();
        }

        /// <summary>
        /// Filter active projects only
        /// </summary>
        public static List<ProjectDto> FilterActive(this IEnumerable<ProjectDto> dtos)
        {
            if (dtos == null) return new List<ProjectDto>();

            return dtos.Where(d => d.IsActive).ToList();
        }

        /// <summary>
        /// Filter root projects only (no parent)
        /// </summary>
        public static List<ProjectDto> FilterRoots(this IEnumerable<ProjectDto> dtos)
        {
            if (dtos == null) return new List<ProjectDto>();

            return dtos.Where(d => !d.ProjectParentId.HasValue).ToList();
        }

        /// <summary>
        /// Filter child projects only (has parent)
        /// </summary>
        public static List<ProjectDto> FilterChildren(this IEnumerable<ProjectDto> dtos)
        {
            if (dtos == null) return new List<ProjectDto>();

            return dtos.Where(d => d.ProjectParentId.HasValue).ToList();
        }

        /// <summary>
        /// Get projects by parent ID
        /// </summary>
        public static List<ProjectDto> GetByParentId(this IEnumerable<ProjectDto> dtos, int parentId)
        {
            if (dtos == null) return new List<ProjectDto>();

            return dtos.Where(d => d.ProjectParentId == parentId).ToList();
        }

        /// <summary>
        /// Check if DTO would create circular reference if set as child of target
        /// </summary>
        public static bool WouldCreateCircularReference(this ProjectDto dto, int targetParentId, IEnumerable<ProjectDto> allProjects)
        {
            if (dto == null || allProjects == null)
                return false;

            // Can't set self as parent
            if (dto.Id == targetParentId)
                return true;

            // Check if target is a descendant of current project
            var descendants = dto.GetAllDescendants();
            return descendants.Any(d => d.Id == targetParentId);
        }
    }
}
