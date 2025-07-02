using System.ComponentModel.DataAnnotations.Schema;
using DT_HR.Domain.Core.Primitives;

namespace DT_HR.Domain.Entities;

public class TelegramGroup : AggregateRoot
{
  private TelegramGroup(){}

  public TelegramGroup(long chatId, string title)
  {
      ChatId = chatId;
      Title = title;
      IsActive = true;
  }
    
   [Column("chat_id")] public long ChatId { get; set; }
   [Column("title")] public string Title { get; set; } = string.Empty;
   [Column("is_active")] public bool IsActive { get; set; }

   public void Activate() => IsActive = true;
   public void DeActivate() => IsActive = false;

}