using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class CampaignBook
{
    public int CampaignID { get; set; }
    public int BookID { get; set; }

    [ForeignKey("CampaignID")]
    public virtual PromotionCampaign Campaign { get; set; } = null!;

    [ForeignKey("BookID")]
    public virtual Book Book { get; set; } = null!;
}
