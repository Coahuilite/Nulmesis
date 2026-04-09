using Nulmesis.Core.Domain.Models;

namespace Nulmesis.App.Services;

/// <summary>
/// 对话框服务接口，用于显示确认对话框
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// 显示删除确认对话框
    /// </summary>
    /// <param name="data">确认数据</param>
    /// <returns>true 表示用户确认删除，false 表示取消</returns>
    bool ShowDeleteConfirmation(DeleteConfirmationDto data);
}