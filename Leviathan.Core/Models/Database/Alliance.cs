using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Leviathan.Core.Models.Database
{
    [Table("alliances")]
    public class Alliance
    {
        [Key, Column("id")]
        public int Id { get; set; }
        
        [Column("name")]
        public string Name { get; set; }
        
        [Column("alliance_id")]
        public int AllianceId { get; set; }

        [Column("ticker")]
        public string Ticker { get; set; }

        public Alliance()
        {
            
        }

        public Alliance(string name, int allianceId, string ticker)
        {
            Name = name;
            AllianceId = allianceId;
            Ticker = ticker;
        }
    }
}