using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ESI.NET.Models.SSO;

namespace Leviathan.Core.Models.Database
{
    [Table("characters")]
    public class Character
    {
        [Key, Column("id")]
        public int Id { get; set; }
        
        [Column("auth_state")]
        public string State { get; set; } = string.Empty;

        [Column("esi_sso_status")]
        public bool EsiSsoStatus { get; set; } = false;

        [Column("esi_sso_access_token")]
        public string EsiTokenAccessToken { get; set; } = string.Empty;
        
        [Column("esi_sso_expires_in")]
        public int EsiTokenExpiresIn { get; set; }

        [Column("esi_sso_refresh_token")]
        public string EsiTokenRefreshToken { get; set; }  = string.Empty;
        
        [Column("esi_character_id")]
        public int EsiCharacterID { get; set; }
        
        [Column("esi_character_name")]
        public string EsiCharacterName { get; set; } = string.Empty;
        
        [Column("esi_alliance_id")]
        public int EsiAllianceID { get; set; }
        
        [Column("esi_corporation_id")]
        public int EsiCorporationID { get; set; }
        
        [Column("discord_user_id")]
        public ulong DiscordUserId { get; set; }

        public Character() { }

        public Character(string state)
        {
            State = state;
        }
    }
}