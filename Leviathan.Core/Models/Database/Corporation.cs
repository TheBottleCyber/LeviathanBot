using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Leviathan.Core.Models.Database
{
    [Table("corporations")]
    public class Corporation
    {
        [Key, Column("id")]
        public int Id { get; set; }
        
        [Column("alliance_id")]
        public int AllianceId { get; set; }
        
        [Column("name")]
        public string Name { get; set; }
        
        [Column("corporation_id")]
        public int CorporationId { get; set; }
        
        [Column("ticker")]
        public string Ticker { get; set; }

        public Corporation()
        {
            
        }

        public Corporation(int allianceId, string name, int corporationId, string ticker)
        {
            AllianceId = allianceId;
            Name = name;
            CorporationId = corporationId;
            Ticker = ticker;
        }
    }
}