using System;
using System.Web;
using ESI.NET;
using ESI.NET.Enumerations;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Models.Database;
using Leviathan.Core.Models.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Leviathan.Web.Pages
{
    public class IndexModel : PageModel
    {
        public readonly string _eveButtonLink;
        public readonly string _discordButtonLink;
        public readonly string _state;

        public IndexModel(MemoryContext context, Settings settings)
        {
            var esiClient = new EsiClient(Options.Create(settings.ESIConfig));
            
            _state = Guid.NewGuid().ToString().Replace("-", "")[..16];
            
            context.Characters.Add(new Character(_state));
            context.SaveChanges();
            
            _eveButtonLink = esiClient.SSO.CreateAuthenticationUrl(state: _state);
            _discordButtonLink = $"https://discord.com/api/oauth2/authorize?client_id={settings.DiscordConfig.ClientId}&" +
                                 $"redirect_uri={settings.DiscordConfig.CallbackUrl}&" +
                                 "response_type=code&" +
                                 "scope=identify&" +
                                 "state=" + _state;
        }

        public void OnGet() { }
    }
}